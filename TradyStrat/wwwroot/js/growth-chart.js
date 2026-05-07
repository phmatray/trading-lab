// Growth-chart client-side renderer.
// One module per chart instance, keyed by the SVG element. Owns:
//   - actual line + area path generation (was C# PathBuilder)
//   - required-CAGR plan curve sampling
//   - today dot + value/chip callout with edge-flip
//   - HTML text overlays (y-axis values, goal label, date axis, today)
//   - tooltip + crosshair (uses the live visible window for date math)
//   - navigator strip pan/zoom (Stage C — added later)

const VB_W = 1200;
const VB_H = 240;
const PLOT_H = 220;          // grid baseline (y=220)
const PLAN_SAMPLES = 30;

const fmtSigned = n => {
    if (n == null || isNaN(n)) return '—';
    const v = Math.round(n);
    return (v >= 0 ? '+€' : '−€') + Math.abs(v).toLocaleString('fr-FR');
};
const fmtEur = n => '€' + Math.round(Math.abs(n)).toLocaleString('fr-FR');

const monthLabel = ms =>
    new Date(ms).toLocaleDateString('en-US', { month: 'short', year: 'numeric' });

// Find the latest data index whose date is <= hoveredDate (binary search).
function findIndexByDate(datesMs, hoveredMs) {
    if (hoveredMs <= datesMs[0]) return 0;
    if (hoveredMs >= datesMs[datesMs.length - 1]) return datesMs.length - 1;
    let lo = 0, hi = datesMs.length - 1;
    while (lo < hi) {
        const mid = (lo + hi + 1) >> 1;
        if (datesMs[mid] <= hoveredMs) lo = mid;
        else hi = mid - 1;
    }
    return lo;
}

const instances = new WeakMap();

export function init(svg, data /*, locale */) {
    if (!svg || !data || !Array.isArray(data.dates) || data.dates.length === 0) return;

    const wrap = svg.parentElement;
    if (getComputedStyle(wrap).position === 'static') wrap.style.position = 'relative';

    const id = data.id;
    const $ = suffix => document.getElementById(`${id}-${suffix}`);

    // ---- Static text (set once) ----
    const goalLabel = $('goalLabel');
    if (goalLabel) goalLabel.textContent = data.goalLabel ?? '';
    const yLabels = [
        { el: $('yLabel75'), text: data.yLabel75 ?? '' },
        { el: $('yLabel50'), text: data.yLabel50 ?? '' },
        { el: $('yLabel25'), text: data.yLabel25 ?? '' },
    ];
    for (const y of yLabels) if (y.el) y.el.textContent = y.text;

    // ---- Pre-compute axis bounds in ms ----
    const datesMs = data.dates.map(s => new Date(s).getTime());
    const axisStartMs = new Date(data.axisStartDate ?? data.dates[0]).getTime();
    const axisEndMs   = new Date(data.axisEndDate   ?? data.dates[data.dates.length - 1]).getTime();
    const axisSpanMs  = Math.max(1, axisEndMs - axisStartMs);
    const lastDataMs  = datesMs[datesMs.length - 1];

    const startCapital = Number(data.startCapitalEur) > 0 ? Number(data.startCapitalEur) : 1;
    const goalEur = Number(data.targetEur) || startCapital;
    const startMs = data.startDate ? new Date(data.startDate).getTime() : axisStartMs;
    const planSpanMs = Math.max(1, axisEndMs - startMs);

    // ---- Element refs ----
    const els = {
        line:        $('linePath'),
        area:        $('areaPath'),
        plan:        $('planPath'),
        nowLine:     $('nowLine'),
        planGap:     $('planGap'),
        planGapCap:  $('planGapCap'),
        todayDot:    $('todayDot'),
        todayValue:  $('todayValue'),
        todayChip:   $('todayChip'),
        xStart:      $('xStart'),
        xMid:        $('xMid'),
        xEnd:        $('xEnd'),
    };

    // ---- Helpers ----
    // t in [0,1] across the axis
    const valueToY = v => PLOT_H - (v / goalEur) * PLOT_H;
    const tToX     = (t, w0, w1) => ((t - w0) / (w1 - w0)) * VB_W;
    const msToT    = ms => (ms - axisStartMs) / axisSpanMs;
    // Required-CAGR plan: V(t) = V0 * (V_T / V0)^τ where τ is fraction of plan span
    const planValueAtMs = ms => {
        if (ms <= startMs) return startCapital;
        const tau = (ms - startMs) / planSpanMs;
        return startCapital * Math.pow(goalEur / startCapital, tau);
    };

    // ---- Today (last data point) ----
    const todayMs    = lastDataMs;
    const todayValue = data.capital[data.capital.length - 1];
    const todayT     = msToT(todayMs);
    const planAtToday = planValueAtMs(todayMs);

    // ---- Tooltip + crosshair ----
    const tooltip = document.createElement('div');
    tooltip.className = 'gc-tooltip';
    tooltip.style.position = 'absolute';
    tooltip.style.pointerEvents = 'none';
    tooltip.style.display = 'none';
    wrap.appendChild(tooltip);

    const guide = document.createElementNS('http://www.w3.org/2000/svg', 'line');
    guide.setAttribute('class', 'gc-guide');
    guide.setAttribute('y1', '0');
    guide.setAttribute('y2', String(PLOT_H));
    guide.style.stroke = 'rgba(196,154,86,0.5)';
    guide.style.strokeWidth = '1';
    guide.style.display = 'none';
    svg.appendChild(guide);

    // Window state: w0/w1 are fractions of the axis span [0,1].
    let w0 = 0;
    let w1 = 1;

    function showTooltip(evt) {
        const rect    = svg.getBoundingClientRect();
        const ratio   = Math.max(0, Math.min(1, (evt.clientX - rect.left) / rect.width));
        // Hovered date computed from the LIVE visible window.
        const tHovered  = w0 + ratio * (w1 - w0);
        const hoveredMs = axisStartMs + tHovered * axisSpanMs;
        const planAt    = planValueAtMs(hoveredMs);
        const dateLabel = new Date(hoveredMs)
            .toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });

        if (hoveredMs > lastDataMs) {
            tooltip.innerHTML =
                `<div class="date">${dateLabel}</div>` +
                `<div class="delta">future runway</div>` +
                `<div class="row"><span>ON PLAN</span><span>${fmtEur(planAt)}</span></div>`;
        } else {
            const i        = findIndexByDate(datesMs, hoveredMs);
            const capital  = data.capital[i] ?? 0;
            const prior    = i > 0 ? (data.capital[i - 1] ?? capital) : capital;
            const dPrior   = capital - prior;
            const focusEur = data.focusTickerEur?.[i];
            const vsPlan   = capital - planAt;
            const planRow  = vsPlan >= 0 ? 'ahead' : 'behind';

            tooltip.innerHTML =
                `<div class="date">${dateLabel}</div>` +
                `<div class="big">${fmtEur(capital)}</div>` +
                `<div class="delta">${fmtSigned(dPrior)} vs. prior day</div>` +
                (focusEur != null && focusEur > 0
                    ? `<div class="row"><span>CON3.L</span><span>€${focusEur.toFixed(2)}</span></div>` : '') +
                `<div class="row ${planRow}"><span>VS. PLAN</span><span>${fmtSigned(vsPlan)}</span></div>`;
        }

        tooltip.style.display = 'block';
        const x = ratio * rect.width;
        tooltip.style.left = Math.min(rect.width - tooltip.offsetWidth - 10, Math.max(0, x + 12)) + 'px';
        tooltip.style.top  = '8px';

        const xVB = ratio * VB_W;
        guide.setAttribute('x1', xVB);
        guide.setAttribute('x2', xVB);
        guide.style.display = '';
    }

    function hideTooltip() {
        tooltip.style.display = 'none';
        guide.style.display   = 'none';
    }

    svg.addEventListener('pointermove',  showTooltip);
    svg.addEventListener('pointerleave', hideTooltip);

    // ---- Plan delta chip text ----
    function buildPlanDeltaText(actual, plan) {
        const delta = actual - plan;
        if (Math.abs(delta) < 50) return 'on plan';
        return fmtEur(delta) + (delta < 0 ? ' behind plan' : ' ahead of plan');
    }

    // ---- Main chart render ----
    function render() {
        // Capital points whose t falls inside [w0, w1]
        const visible = [];
        for (let i = 0; i < datesMs.length; i++) {
            const t = msToT(datesMs[i]);
            if (t < w0 - 1e-6 || t > w1 + 1e-6) continue;
            visible.push({ t, v: data.capital[i] ?? 0 });
        }

        if (visible.length >= 2 && els.line && els.area) {
            const lineD = visible.map((p, i) =>
                (i ? 'L' : 'M') + tToX(p.t, w0, w1).toFixed(2) + ',' + valueToY(p.v).toFixed(2)
            ).join(' ');
            const last  = visible[visible.length - 1];
            const first = visible[0];
            const areaD = lineD +
                ' L' + tToX(last.t,  w0, w1).toFixed(2) + ',' + PLOT_H +
                ' L' + tToX(first.t, w0, w1).toFixed(2) + ',' + PLOT_H + ' Z';
            els.line.setAttribute('d', lineD);
            els.area.setAttribute('d', areaD);
            els.line.style.display = els.area.style.display = '';
        } else if (els.line && els.area) {
            els.line.style.display = els.area.style.display = 'none';
        }

        // Plan curve — sample evenly across visible window
        if (els.plan) {
            const segs = [];
            for (let i = 0; i <= PLAN_SAMPLES; i++) {
                const t  = w0 + (w1 - w0) * (i / PLAN_SAMPLES);
                const ms = axisStartMs + t * axisSpanMs;
                const x  = (i / PLAN_SAMPLES) * VB_W;
                segs.push((i ? 'L' : 'M') + x.toFixed(2) + ',' + valueToY(planValueAtMs(ms)).toFixed(2));
            }
            els.plan.setAttribute('d', segs.join(' '));
        }

        // Today marker (only if today is in the window)
        const todayVisible = todayT >= w0 && todayT <= w1;
        for (const el of [els.nowLine, els.planGap, els.planGapCap, els.todayDot, els.todayValue, els.todayChip]) {
            if (el) el.style.display = todayVisible ? '' : 'none';
        }

        if (todayVisible) {
            const x     = tToX(todayT, w0, w1);
            const y     = valueToY(todayValue);
            const planY = valueToY(planAtToday);

            if (els.nowLine) {
                els.nowLine.setAttribute('x1', x);
                els.nowLine.setAttribute('x2', x);
            }
            if (els.planGap) {
                els.planGap.setAttribute('x1', x);
                els.planGap.setAttribute('x2', x);
                els.planGap.setAttribute('y1', Math.min(planY, y));
                els.planGap.setAttribute('y2', Math.max(planY, y));
            }
            if (els.planGapCap) {
                els.planGapCap.setAttribute('cx', x);
                els.planGapCap.setAttribute('cy', planY);
            }
            if (els.todayDot) {
                els.todayDot.setAttribute('cx', x);
                els.todayDot.setAttribute('cy', y);
            }

            // HTML overlays — viewBox-percentage positioning
            if (els.todayValue) {
                els.todayValue.style.left = (x / VB_W * 100) + '%';
                els.todayValue.style.top  = ((y - 10) / VB_H * 100) + '%';
                els.todayValue.textContent = `${fmtEur(todayValue)} — today`;
            }
            if (els.todayChip) {
                els.todayChip.style.left  = (x / VB_W * 100) + '%';
                els.todayChip.style.top   = ((y + 16) / VB_H * 100) + '%';
                els.todayChip.textContent = buildPlanDeltaText(todayValue, planAtToday);
            }

            // Flip label anchor when today is near the left edge of the visible window
            const flipLeft = x < 200;
            if (els.todayValue) els.todayValue.classList.toggle('flip', flipLeft);
            if (els.todayChip)  els.todayChip.classList.toggle('flip', flipLeft);

            // Color chip + gap by sign of (actual - plan)
            const ahead = todayValue >= planAtToday;
            if (els.todayChip)   { els.todayChip.classList.toggle('ahead', ahead);   els.todayChip.classList.toggle('behind', !ahead); }
            if (els.planGap)     { els.planGap.classList.toggle('ahead', ahead);     els.planGap.classList.toggle('behind', !ahead); }
            if (els.planGapCap)  { els.planGapCap.classList.toggle('ahead', ahead);  els.planGapCap.classList.toggle('behind', !ahead); }
        }

        // Date axis
        if (els.xStart) els.xStart.textContent = monthLabel(axisStartMs + w0 * axisSpanMs);
        if (els.xMid)   els.xMid.textContent   = monthLabel(axisStartMs + ((w0 + w1) / 2) * axisSpanMs);
        if (els.xEnd)   els.xEnd.textContent   = monthLabel(axisStartMs + w1 * axisSpanMs);
    }

    render();

    instances.set(svg, {
        showTooltip, hideTooltip, tooltip, guide,
        setWindow: (a, b) => { w0 = a; w1 = b; render(); },
    });
}

export function dispose(svg) {
    const i = instances.get(svg);
    if (!i) return;
    svg.removeEventListener('pointermove',  i.showTooltip);
    svg.removeEventListener('pointerleave', i.hideTooltip);
    i.tooltip.remove();
    i.guide.remove();
    instances.delete(svg);
}

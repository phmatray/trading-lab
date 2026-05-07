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

    // ---- Helpers ----
    // t in [0,1] across the axis
    const valueToY = v => PLOT_H - (v / goalEur) * PLOT_H;
    const tToX     = (t, w0, w1) => ((t - w0) / (w1 - w0)) * VB_W;
    const msToT    = ms => (ms - axisStartMs) / axisSpanMs;
    // Required-CAGR plan: V(t) = V0 * (V_T / V0)^τ where τ is fraction of plan span
    function planValueAtMs(ms) {
        if (ms <= startMs) return startCapital;
        const tau = (ms - startMs) / planSpanMs;
        return startCapital * Math.pow(goalEur / startCapital, tau);
    }

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

    // ---- Event annotations ----
    // Each event resolves to:
    //   ms: the event date
    //   capital: actual capital at the closest data point on/before the event
    //            — falls back to plan value if the event predates all data.
    const events = (Array.isArray(data.events) ? data.events : []).map((e, i) => {
        const eMs = new Date(e.date).getTime();
        const idx = findIndexByDate(datesMs, eMs);
        const cap = data.capital[idx] ?? 0;
        return {
            ms:      eMs,
            capital: cap > 0 ? cap : planValueAtMs(eMs),
            svg:     $(`event-svg-${i}`),
            num:     $(`event-num-${i}`),
        };
    });

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

        // Events — italic Roman numeral above each event date on the line
        for (const ev of events) {
            const t = msToT(ev.ms);
            const inWin = t >= w0 && t <= w1;
            if (ev.svg) ev.svg.style.display = inWin ? '' : 'none';
            if (ev.num) ev.num.style.display = inWin ? '' : 'none';
            if (!inWin) continue;

            const x        = tToX(t, w0, w1);
            const y        = valueToY(ev.capital);
            const stalkTop = y - 14;

            if (ev.svg) {
                const stalk = ev.svg.querySelector('.event-stalk');
                const dot   = ev.svg.querySelector('.event-dot');
                if (stalk) {
                    stalk.setAttribute('x1', x);
                    stalk.setAttribute('x2', x);
                    stalk.setAttribute('y1', y);
                    stalk.setAttribute('y2', stalkTop);
                }
                if (dot) {
                    dot.setAttribute('cx', x);
                    dot.setAttribute('cy', y);
                }
            }
            if (ev.num) {
                ev.num.style.left = (x / VB_W * 100) + '%';
                ev.num.style.top  = (stalkTop / VB_H * 100) + '%';
            }
        }

        // Date axis
        if (els.xStart) els.xStart.textContent = monthLabel(axisStartMs + w0 * axisSpanMs);
        if (els.xMid)   els.xMid.textContent   = monthLabel(axisStartMs + ((w0 + w1) / 2) * axisSpanMs);
        if (els.xEnd)   els.xEnd.textContent   = monthLabel(axisStartMs + w1 * axisSpanMs);
    }

    // ---- Navigator strip ----
    const naviBg     = $('naviBg');
    const naviLine   = $('naviLine');
    const naviArea   = $('naviArea');
    const naviPlan   = $('naviPlan');
    const spotlight  = $('spotlight');
    const handleL    = $('handleL');
    const handleR    = $('handleR');
    const naviLabelL = $('naviLabelL');
    const naviLabelR = $('naviLabelR');
    const naviPresets = $('naviPresets');

    // Total time span in ms — used for preset windows.
    const totalDays = Math.max(1, Math.round(axisSpanMs / (24 * 3600 * 1000)));
    const MIN_SIZE  = Math.min(0.05, 30 / totalDays);  // 5% of axis or 30 days, whichever smaller

    function renderNavigator() {
        if (!naviBg || datesMs.length === 0) return;
        const VBH = 40;
        // Squeeze capital line into the bottom 70% of the strip; plan curve fills the top.
        const navY = v => VBH - 4 - (v / goalEur) * (VBH - 6);

        const linePts = data.capital.map((v, i) => {
            const t = msToT(datesMs[i]);
            const x = t * VB_W;
            return (i ? 'L' : 'M') + x.toFixed(1) + ',' + navY(v).toFixed(2);
        }).join(' ');
        if (naviLine) naviLine.setAttribute('d', linePts);

        const lastT = msToT(datesMs[datesMs.length - 1]);
        const lastX = lastT * VB_W;
        if (naviArea) naviArea.setAttribute('d',
            linePts + ' L' + lastX.toFixed(1) + ',' + (VBH - 0.5) +
            ' L0,' + (VBH - 0.5) + ' Z'
        );

        const planSegs = [];
        for (let i = 0; i <= PLAN_SAMPLES; i++) {
            const t  = i / PLAN_SAMPLES;
            const ms = axisStartMs + t * axisSpanMs;
            const x  = t * VB_W;
            planSegs.push((i ? 'L' : 'M') + x.toFixed(1) + ',' + navY(planValueAtMs(ms)).toFixed(2));
        }
        if (naviPlan) naviPlan.setAttribute('d', planSegs.join(' '));
    }

    function updateNavigatorUI() {
        if (!spotlight) return;
        spotlight.style.left  = (w0 * 100) + '%';
        spotlight.style.width = ((w1 - w0) * 100) + '%';
        if (naviLabelL) {
            naviLabelL.textContent = monthLabel(axisStartMs + w0 * axisSpanMs);
            naviLabelL.style.left  = (w0 * 100) + '%';
        }
        if (naviLabelR) {
            naviLabelR.textContent = monthLabel(axisStartMs + w1 * axisSpanMs);
            naviLabelR.style.left  = (w1 * 100) + '%';
        }
        // Mark active preset (or none if window doesn't match a preset)
        if (naviPresets) {
            const sizeDays = Math.round((w1 - w0) * totalDays);
            const isAll = (w1 - w0) >= 0.999;
            naviPresets.querySelectorAll('button').forEach(btn => {
                const r = btn.dataset.range;
                if (r === 'all') {
                    btn.classList.toggle('active', isAll);
                } else {
                    const months = parseInt(r, 10);
                    const expected = Math.round(months * 30.4375); // average days/month
                    btn.classList.toggle('active', !isAll && Math.abs(sizeDays - expected) <= 3);
                }
            });
        }
    }

    function setWindow(newW0, newW1) {
        if (newW0 < 0) newW0 = 0;
        if (newW1 > 1) newW1 = 1;
        if (newW1 - newW0 < MIN_SIZE) {
            // Resize handles hit the floor — keep edge anchored, push opposite edge
            if (newW0 === 0)        newW1 = MIN_SIZE;
            else if (newW1 === 1)   newW0 = 1 - MIN_SIZE;
            else                    newW1 = newW0 + MIN_SIZE;
        }
        w0 = newW0;
        w1 = newW1;
        updateNavigatorUI();
        render();
    }

    function getNaviFraction(clientX) {
        if (!naviBg) return 0;
        const r = naviBg.getBoundingClientRect();
        return Math.max(0, Math.min(1, (clientX - r.left) / r.width));
    }

    let dragMode    = null;       // 'pan' | 'l' | 'r' | null
    let dragMoved   = false;
    let dragStartX  = 0;
    let dragStartW0 = 0, dragStartW1 = 0;

    function startDrag(mode, clientX) {
        dragMode    = mode;
        dragStartX  = getNaviFraction(clientX);
        dragStartW0 = w0;
        dragStartW1 = w1;
        dragMoved   = false;
        hideTooltip();   // prevent tooltip lingering during drag
    }

    function endDrag() {
        dragMode = null;
        // Clear dragMoved on the next tick so the synthetic `click` event
        // (fires after pointerup) can still read it as true.
        setTimeout(() => { dragMoved = false; }, 0);
    }

    if (spotlight) {
        spotlight.addEventListener('pointerdown', e => {
            if (e.target === handleL)      startDrag('l',   e.clientX);
            else if (e.target === handleR) startDrag('r',   e.clientX);
            else                           startDrag('pan', e.clientX);
            spotlight.setPointerCapture(e.pointerId);
            e.preventDefault();
        });
        spotlight.addEventListener('pointermove', e => {
            if (!dragMode) return;
            dragMoved = true;
            const x  = getNaviFraction(e.clientX);
            const dx = x - dragStartX;
            if (dragMode === 'pan') {
                const size = dragStartW1 - dragStartW0;
                let nW0 = dragStartW0 + dx;
                let nW1 = dragStartW1 + dx;
                if (nW0 < 0) { nW0 = 0; nW1 = size; }
                if (nW1 > 1) { nW1 = 1; nW0 = 1 - size; }
                setWindow(nW0, nW1);
            } else if (dragMode === 'l') {
                setWindow(dragStartW0 + dx, dragStartW1);
            } else if (dragMode === 'r') {
                setWindow(dragStartW0, dragStartW1 + dx);
            }
        });
        spotlight.addEventListener('pointerup',     endDrag);
        spotlight.addEventListener('pointercancel', endDrag);
    }

    // Click on navigator background outside spotlight → recenter window there.
    // Skip if the click is the synthetic trailing-click of a real drag.
    if (naviBg) {
        naviBg.addEventListener('click', e => {
            if (dragMoved) return;
            const x = getNaviFraction(e.clientX);
            if (x >= w0 && x <= w1) return;
            const size = w1 - w0;
            setWindow(x - size / 2, x + size / 2);
        });
        naviBg.addEventListener('pointerdown', hideTooltip);
    }

    // Preset chips — anchor the window on today (or as much past as fits).
    if (naviPresets) {
        const todayFraction = msToT(todayMs);
        naviPresets.querySelectorAll('button').forEach(btn => {
            btn.addEventListener('click', () => {
                const r = btn.dataset.range;
                if (r === 'all') { setWindow(0, 1); return; }
                const months  = parseInt(r, 10);
                const sizeDays = months * 30.4375;
                const size    = sizeDays / totalDays;
                if (size >= 1) { setWindow(0, 1); return; }
                let nW1 = Math.min(1, todayFraction + size * 0.15);
                let nW0 = nW1 - size;
                if (nW0 < 0) { nW0 = 0; nW1 = size; }
                setWindow(nW0, nW1);
            });
        });
    }

    renderNavigator();
    updateNavigatorUI();
    render();

    instances.set(svg, {
        showTooltip, hideTooltip, tooltip, guide,
        setWindow,
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

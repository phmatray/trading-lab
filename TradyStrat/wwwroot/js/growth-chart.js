const formatters = {
    eur: new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 }),
};

function fmtSigned(n) {
    if (n == null || isNaN(n)) return '—';
    const v = Math.round(n);
    return (v >= 0 ? '+€' : '−€') + Math.abs(v).toLocaleString('fr-FR');
}

// Find the latest data index whose date is <= hoveredDate.
// Data dates are ascending; binary search.
function findIndexByDate(dates, hoveredMs) {
    if (hoveredMs <= new Date(dates[0]).getTime()) return 0;
    if (hoveredMs >= new Date(dates[dates.length - 1]).getTime()) return dates.length - 1;
    let lo = 0, hi = dates.length - 1;
    while (lo < hi) {
        const mid = (lo + hi + 1) >> 1;
        if (new Date(dates[mid]).getTime() <= hoveredMs) lo = mid;
        else hi = mid - 1;
    }
    return lo;
}

const tooltipsByElement = new WeakMap();

export function init(svg, data /*, locale */) {
    if (!svg || !data || !Array.isArray(data.dates) || data.dates.length === 0) return;

    const wrap = svg.parentElement;
    if (getComputedStyle(wrap).position === 'static') wrap.style.position = 'relative';

    const tooltip = document.createElement('div');
    tooltip.className = 'gc-tooltip';
    tooltip.style.position = 'absolute';
    tooltip.style.pointerEvents = 'none';
    tooltip.style.display = 'none';
    wrap.appendChild(tooltip);

    const guide = document.createElementNS('http://www.w3.org/2000/svg', 'line');
    guide.setAttribute('class', 'gc-guide');
    guide.setAttribute('y1', '0');
    guide.setAttribute('y2', '100%');
    guide.style.stroke = 'rgba(196,154,86,0.5)';
    guide.style.strokeWidth = '1';
    guide.style.display = 'none';
    svg.appendChild(guide);

    // Axis spans first-trade-date → axisEndDate (goal date or last data date).
    // Capital/position/focusTickerEur arrays only span first-trade-date → today
    // (last data date). Past today, the chart's "future runway" shows the
    // planned trajectory only — actual values are absent.
    const axisStartMs = new Date(data.axisStartDate ?? data.dates[0]).getTime();
    const axisEndMs   = new Date(data.axisEndDate   ?? data.dates[data.dates.length - 1]).getTime();
    const axisSpanMs  = Math.max(1, axisEndMs - axisStartMs);
    const lastDataMs  = new Date(data.dates[data.dates.length - 1]).getTime();

    function show(evt) {
        const rect = svg.getBoundingClientRect();
        const ratio = Math.max(0, Math.min(1, (evt.clientX - rect.left) / rect.width));
        const hoveredMs = axisStartMs + ratio * axisSpanMs;
        const dateLabel = new Date(hoveredMs)
            .toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });

        // Linear plan baseline at hovered date — works for any point on the axis.
        const planAt = data.targetEur != null
            ? data.targetEur * ((hoveredMs - axisStartMs) / axisSpanMs)
            : null;

        if (hoveredMs > lastDataMs) {
            // Future runway: no actual capital, just the planned trajectory.
            tooltip.innerHTML =
                `<div class="date">${dateLabel}</div>` +
                `<div class="delta">future runway</div>` +
                (planAt != null ? `<div class="row"><span>ON PLAN</span><span>${formatters.eur.format(planAt)}</span></div>` : '');
        } else {
            const i        = findIndexByDate(data.dates, hoveredMs);
            const capital  = data.capital[i] ?? 0;
            const prior    = i > 0 ? (data.capital[i - 1] ?? capital) : capital;
            const dPrior   = capital - prior;
            const position = data.position?.[i];
            const focusEur = data.focusTickerEur?.[i];
            const vsPlan   = planAt != null ? capital - planAt : null;

            tooltip.innerHTML =
                `<div class="date">${dateLabel}</div>` +
                `<div class="big">${formatters.eur.format(capital)}</div>` +
                `<div class="delta">${fmtSigned(dPrior)} vs. prior day</div>` +
                (position != null ? `<div class="row"><span>POSITION</span><span>${Math.round(position).toLocaleString('fr-FR')} sh</span></div>` : '') +
                (focusEur != null && focusEur > 0 ? `<div class="row"><span>CON3.L</span><span>€${focusEur.toFixed(2)}</span></div>` : '') +
                (vsPlan != null ? `<div class="row"><span>VS. PLAN</span><span>${fmtSigned(vsPlan)}</span></div>` : '');
        }

        tooltip.style.display = 'block';
        const x = ratio * rect.width;
        tooltip.style.left = Math.min(rect.width - tooltip.offsetWidth - 10, Math.max(0, x + 12)) + 'px';
        tooltip.style.top  = '8px';

        guide.setAttribute('x1', `${ratio * 100}%`);
        guide.setAttribute('x2', `${ratio * 100}%`);
        guide.style.display = '';
    }

    function hide() {
        tooltip.style.display = 'none';
        guide.style.display = 'none';
    }

    svg.addEventListener('pointermove', show);
    svg.addEventListener('pointerleave', hide);

    tooltipsByElement.set(svg, { tooltip, guide, show, hide });
}

export function dispose(svg) {
    const refs = tooltipsByElement.get(svg);
    if (!refs) return;
    svg.removeEventListener('pointermove', refs.show);
    svg.removeEventListener('pointerleave', refs.hide);
    refs.tooltip.remove();
    refs.guide.remove();
    tooltipsByElement.delete(svg);
}

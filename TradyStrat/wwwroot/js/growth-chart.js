const formatters = {
    eur: new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 }),
};

function fmtSigned(n) {
    if (n == null || isNaN(n)) return '—';
    const v = Math.round(n);
    return (v >= 0 ? '+€' : '−€') + Math.abs(v).toLocaleString('fr-FR');
}

function findIndex(dates, mouseRatio) {
    const idx = Math.round(mouseRatio * (dates.length - 1));
    return Math.max(0, Math.min(dates.length - 1, idx));
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

    function show(evt) {
        const rect = svg.getBoundingClientRect();
        const ratio = (evt.clientX - rect.left) / rect.width;
        const i = findIndex(data.dates, ratio);
        const d = new Date(data.dates[i]);
        const dateLabel = d.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });

        const capital     = data.capital[i] ?? 0;
        const prior       = i > 0 ? (data.capital[i - 1] ?? capital) : capital;
        const dPrior      = capital - prior;
        const position    = data.position?.[i];
        const focusPxEur  = data.focusTickerEur?.[i];
        const planAtIdx   = data.targetEur && data.targetDate
            ? data.targetEur * (i / (data.dates.length - 1))
            : null;
        const vsPlan      = planAtIdx != null ? capital - planAtIdx : null;

        tooltip.innerHTML =
            `<div class="date">${dateLabel}</div>` +
            `<div class="big">${formatters.eur.format(capital)}</div>` +
            `<div class="delta">${fmtSigned(dPrior)} vs. prior day</div>` +
            (position != null ? `<div class="row"><span>POSITION</span><span>${Math.round(position).toLocaleString('fr-FR')} sh</span></div>` : '') +
            (focusPxEur != null ? `<div class="row"><span>CON3.L</span><span>€${focusPxEur.toFixed(2)}</span></div>` : '') +
            (vsPlan != null ? `<div class="row"><span>VS. PLAN</span><span>${fmtSigned(vsPlan)}</span></div>` : '');

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

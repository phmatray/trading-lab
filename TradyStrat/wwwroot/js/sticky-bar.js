// Toggles `.visible` (and `aria-hidden`) on [data-sticky-cap] once the
// dashboard hero (.hero-row) has scrolled fully above the fold. Clicking the
// bar scrolls smoothly to top. Idempotent — calling observeHero() again after
// a Blazor re-render disconnects the prior observer and re-attaches.
let observer = null;
let clickHandler = null;
let observedBar = null;

export function observeHero() {
    disconnect();

    const bar = document.querySelector('[data-sticky-cap]');
    const hero = document.querySelector('.hero-row');
    if (!bar || !hero) return;

    observer = new IntersectionObserver(
        (entries) => {
            for (const e of entries) {
                const show = !e.isIntersecting && e.boundingClientRect.top < 0;
                bar.classList.toggle('visible', show);
                bar.setAttribute('aria-hidden', show ? 'false' : 'true');
            }
        },
        { rootMargin: '0px 0px -100% 0px', threshold: 0 }
    );
    observer.observe(hero);

    clickHandler = (ev) => {
        ev.preventDefault();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    };
    bar.addEventListener('click', clickHandler);
    observedBar = bar;
}

export function disconnect() {
    if (observer) {
        observer.disconnect();
        observer = null;
    }
    if (observedBar && clickHandler) {
        observedBar.removeEventListener('click', clickHandler);
    }
    clickHandler = null;
    observedBar = null;
}

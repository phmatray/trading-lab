// Toggles `.visible` (and `aria-hidden`) on [data-sticky-cap] once the
// dashboard hero (.hero-row) has scrolled fully above the fold. Clicking the
// bar scrolls smoothly to top. Idempotent — calling observeHero() again when
// already observing the same elements is a no-op (so Blazor re-renders that
// re-invoke it don't tear down a working observer mid-flight and lose track
// of pending transitions).
let observer = null;
let clickHandler = null;
let observedBar = null;
let observedHero = null;

export function observeHero() {
    const bar = document.querySelector('[data-sticky-cap]');
    const hero = document.querySelector('.hero-row');
    if (!bar || !hero) return;

    // If we're already observing this exact pair, leave the working observer alone.
    if (observer && observedBar === bar && observedHero === hero) return;

    disconnect();

    // Observe the hero against the full viewport (default root). Show the
    // bar when the hero is no longer intersecting AND its top edge has gone
    // above the viewport — i.e. the user has scrolled fully past it. Default
    // viewport root is more reliable across browsers than a zero-height
    // root strip via `rootMargin: '0px 0px -100% 0px'`, which can miss
    // subsequent threshold crossings on some Chrome builds.
    observer = new IntersectionObserver(
        (entries) => {
            for (const e of entries) {
                const show = !e.isIntersecting && e.boundingClientRect.top < 0;
                bar.classList.toggle('visible', show);
                bar.setAttribute('aria-hidden', show ? 'false' : 'true');
            }
        },
        { threshold: 0 }
    );
    observer.observe(hero);

    clickHandler = (ev) => {
        ev.preventDefault();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    };
    bar.addEventListener('click', clickHandler);
    observedBar = bar;
    observedHero = hero;
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
    observedHero = null;
}

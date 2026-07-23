let dotNetRef = null;
let listener = null;

const isEditableTarget = (el) => {
    if (!el) return false;
    if (el.matches?.('input, textarea, select')) return true;
    if (el.isContentEditable) return true;
    return false;
};

export function attach(ref) {
    dotNetRef = ref;
    listener = (e) => {
        if (isEditableTarget(document.activeElement)) return;
        if (e.key === 'ArrowLeft')  { e.preventDefault(); dotNetRef?.invokeMethodAsync('OnPrev'); }
        else if (e.key === 'ArrowRight') { e.preventDefault(); dotNetRef?.invokeMethodAsync('OnNext'); }
    };
    document.addEventListener('keydown', listener);
}

export function detach() {
    if (listener) {
        document.removeEventListener('keydown', listener);
        listener = null;
    }
    dotNetRef = null;
}

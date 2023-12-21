export function loadTradingViewScript(reference, settings, scriptSrcPath) {
    const script = document.createElement("script");
    script.src = scriptSrcPath;
    script.type = "text/javascript";
    script.async = true;
    script.innerHTML = JSON.stringify(settings);
    
    reference.appendChild(script);
}
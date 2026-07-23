export function loadTradingViewScript(reference, settingsJson, scriptSrcPath) {
    console.log(`loadTradingViewScript called with ${reference} at ${scriptSrcPath}`);
    console.log(settingsJson);
    
    const script = document.createElement("script");
    script.src = scriptSrcPath;
    script.type = "text/javascript";
    script.async = true;
    script.innerHTML = settingsJson;
    
    reference.appendChild(script);
}
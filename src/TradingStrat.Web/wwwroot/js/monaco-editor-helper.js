// Monaco Editor Helper for TradingStrat Python Strategy Editor
// Provides Python syntax highlighting, IntelliSense, and Blazor integration

let monacoEditorInstance = null;
let monacoEditorDisposables = [];

/**
 * Initialize Monaco Editor in the specified container
 * @param {string} containerId - The DOM element ID to host the editor
 * @param {string} initialCode - Initial Python code to display
 * @param {object} dotNetHelper - Blazor .NET interop reference for callbacks
 * @param {number} height - Editor height in pixels (default: 500)
 * @returns {Promise<void>}
 */
window.monacoEditorHelper = {
    initializeEditor: async function (containerId, initialCode, dotNetHelper, height = 500) {
        // Cleanup existing editor if any
        if (monacoEditorInstance) {
            this.disposeEditor();
        }

        // Dynamically load Monaco Editor from node_modules
        await this.loadMonacoLoader();

        // Configure Monaco loader
        require.config({
            paths: {
                'vs': '/node_modules/monaco-editor/min/vs'
            }
        });

        // Initialize Monaco Editor
        return new Promise((resolve, reject) => {
            require(['vs/editor/editor.main'], () => {
                try {
                    // Create editor instance
                    const container = document.getElementById(containerId);
                    if (!container) {
                        reject(new Error(`Container element '${containerId}' not found`));
                        return;
                    }

                    // Set container height
                    container.style.height = `${height}px`;

                    monacoEditorInstance = monaco.editor.create(container, {
                        value: initialCode || this.getDefaultPythonTemplate(),
                        language: 'python',
                        theme: 'vs-dark',
                        automaticLayout: true,
                        minimap: { enabled: true },
                        fontSize: 14,
                        lineNumbers: 'on',
                        roundedSelection: false,
                        scrollBeyondLastLine: false,
                        readOnly: false,
                        folding: true,
                        wordWrap: 'on',
                        tabSize: 4,
                        insertSpaces: true
                    });

                    // Register custom IntelliSense
                    this.registerTradingStratIntelliSense();

                    // Setup change callback to notify Blazor
                    const changeDisposable = monacoEditorInstance.onDidChangeModelContent(() => {
                        if (dotNetHelper) {
                            const code = monacoEditorInstance.getValue();
                            dotNetHelper.invokeMethodAsync('OnCodeChanged', code);
                        }
                    });
                    monacoEditorDisposables.push(changeDisposable);

                    resolve();
                } catch (error) {
                    reject(error);
                }
            });
        });
    },

    /**
     * Load Monaco Editor loader script
     */
    loadMonacoLoader: function () {
        return new Promise((resolve, reject) => {
            if (typeof require !== 'undefined') {
                resolve();
                return;
            }

            const script = document.createElement('script');
            script.src = '/node_modules/monaco-editor/min/vs/loader.js';
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    },

    /**
     * Register custom IntelliSense for TradingStrat Python API
     */
    registerTradingStratIntelliSense: function () {
        if (typeof monaco === 'undefined') return;

        const completionProvider = monaco.languages.registerCompletionItemProvider('python', {
            provideCompletionItems: (model, position) => {
                const suggestions = [
                    // Required function
                    {
                        label: 'generate_signal',
                        kind: monaco.languages.CompletionItemKind.Function,
                        insertText: [
                            'def generate_signal(index, price, cash, position):',
                            '    """',
                            '    Generate trading signal for current bar.',
                            '    ',
                            '    Args:',
                            '        index (int): Current bar index (0-based)',
                            '        price (float): Current closing price',
                            '        cash (float): Available cash',
                            '        position (int): Current shares held',
                            '    ',
                            '    Returns:',
                            '        dict: {"action": "buy"|"sell"|"hold", "quantity": int, "reason": str}',
                            '    """',
                            '    return {"action": "hold", "quantity": 0, "reason": "Not implemented"}',
                            ''
                        ].join('\n'),
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: 'Required function to generate trading signals. Called for each bar in the backtest.'
                    },
                    // Optional function
                    {
                        label: 'initialize',
                        kind: monaco.languages.CompletionItemKind.Function,
                        insertText: [
                            'def initialize(prices):',
                            '    """',
                            '    Optional: Pre-calculate indicators once before backtest.',
                            '    ',
                            '    Args:',
                            '        prices (dict): Historical price data as NumPy arrays',
                            '            - prices["close"]: Closing prices',
                            '            - prices["open"]: Opening prices',
                            '            - prices["high"]: High prices',
                            '            - prices["low"]: Low prices',
                            '            - prices["volume"]: Volume data',
                            '            - prices["dates"]: Date timestamps',
                            '    """',
                            '    pass',
                            ''
                        ].join('\n'),
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: 'Optional initialization function. Pre-calculate indicators here for better performance.'
                    },
                    // Common libraries
                    {
                        label: 'import talib',
                        kind: monaco.languages.CompletionItemKind.Module,
                        insertText: 'import talib',
                        documentation: 'TA-Lib technical analysis library (allowed)'
                    },
                    {
                        label: 'import numpy as np',
                        kind: monaco.languages.CompletionItemKind.Module,
                        insertText: 'import numpy as np',
                        documentation: 'NumPy numerical computing library (allowed)'
                    },
                    {
                        label: 'import pandas as pd',
                        kind: monaco.languages.CompletionItemKind.Module,
                        insertText: 'import pandas as pd',
                        documentation: 'Pandas data analysis library (allowed)'
                    },
                    // TA-Lib common indicators
                    {
                        label: 'talib.SMA',
                        kind: monaco.languages.CompletionItemKind.Function,
                        insertText: 'talib.SMA(prices["close"], timeperiod=${1:20})',
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: 'Simple Moving Average'
                    },
                    {
                        label: 'talib.EMA',
                        kind: monaco.languages.CompletionItemKind.Function,
                        insertText: 'talib.EMA(prices["close"], timeperiod=${1:12})',
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: 'Exponential Moving Average'
                    },
                    {
                        label: 'talib.RSI',
                        kind: monaco.languages.CompletionItemKind.Function,
                        insertText: 'talib.RSI(prices["close"], timeperiod=${1:14})',
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: 'Relative Strength Index'
                    },
                    {
                        label: 'talib.MACD',
                        kind: monaco.languages.CompletionItemKind.Function,
                        insertText: 'talib.MACD(prices["close"], fastperiod=${1:12}, slowperiod=${2:26}, signalperiod=${3:9})',
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: 'Moving Average Convergence/Divergence'
                    },
                    {
                        label: 'talib.BBANDS',
                        kind: monaco.languages.CompletionItemKind.Function,
                        insertText: 'talib.BBANDS(prices["close"], timeperiod=${1:20}, nbdevup=${2:2}, nbdevdn=${3:2})',
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: 'Bollinger Bands'
                    },
                    {
                        label: 'talib.ATR',
                        kind: monaco.languages.CompletionItemKind.Function,
                        insertText: 'talib.ATR(prices["high"], prices["low"], prices["close"], timeperiod=${1:14})',
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: 'Average True Range'
                    },
                    // Signal actions
                    {
                        label: 'buy_signal',
                        kind: monaco.languages.CompletionItemKind.Snippet,
                        insertText: '{"action": "buy", "quantity": ${1:quantity}, "reason": "${2:Buy reason}"}',
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: 'Return a buy signal'
                    },
                    {
                        label: 'sell_signal',
                        kind: monaco.languages.CompletionItemKind.Snippet,
                        insertText: '{"action": "sell", "quantity": ${1:position}, "reason": "${2:Sell reason}"}',
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: 'Return a sell signal'
                    },
                    {
                        label: 'hold_signal',
                        kind: monaco.languages.CompletionItemKind.Snippet,
                        insertText: '{"action": "hold", "quantity": 0, "reason": "${1:Hold reason}"}',
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: 'Return a hold signal'
                    }
                ];

                return { suggestions };
            }
        });

        monacoEditorDisposables.push(completionProvider);
    },

    /**
     * Get default Python template for new strategies
     */
    getDefaultPythonTemplate: function () {
        return `import talib

# Global variables for pre-calculated indicators
sma_20 = None
sma_50 = None

def initialize(prices):
    """
    Optional: Pre-calculate indicators once for better performance.

    Args:
        prices (dict): Historical price data as NumPy arrays
            - prices["close"], prices["open"], prices["high"]
            - prices["low"], prices["volume"], prices["dates"]
    """
    global sma_20, sma_50
    sma_20 = talib.SMA(prices["close"], timeperiod=20)
    sma_50 = talib.SMA(prices["close"], timeperiod=50)

def generate_signal(index, price, cash, position):
    """
    Generate trading signal for current bar.

    Args:
        index (int): Current bar index (0-based)
        price (float): Current closing price
        cash (float): Available cash
        position (int): Current shares held

    Returns:
        dict: {"action": "buy"|"sell"|"hold", "quantity": int, "reason": str}
    """
    # Wait for sufficient data
    if index < 50:
        return {"action": "hold", "quantity": 0, "reason": "Insufficient data"}

    # Golden cross (SMA20 crosses above SMA50) - Buy signal
    if sma_20[index-1] <= sma_50[index-1] and sma_20[index] > sma_50[index] and position == 0:
        quantity = int((cash * 0.95) / price)  # Use 95% of cash
        return {"action": "buy", "quantity": quantity, "reason": "Golden cross: SMA20 > SMA50"}

    # Death cross (SMA20 crosses below SMA50) - Sell signal
    if sma_20[index-1] >= sma_50[index-1] and sma_20[index] < sma_50[index] and position > 0:
        return {"action": "sell", "quantity": position, "reason": "Death cross: SMA20 < SMA50"}

    return {"action": "hold", "quantity": 0, "reason": "No signal"}
`;
    },

    /**
     * Get current code from the editor
     */
    getCode: function () {
        return monacoEditorInstance ? monacoEditorInstance.getValue() : '';
    },

    /**
     * Set code in the editor
     */
    setCode: function (code) {
        if (monacoEditorInstance) {
            monacoEditorInstance.setValue(code || '');
        }
    },

    /**
     * Set editor read-only state
     */
    setReadOnly: function (readOnly) {
        if (monacoEditorInstance) {
            monacoEditorInstance.updateOptions({ readOnly: readOnly });
        }
    },

    /**
     * Dispose the editor and cleanup resources
     */
    disposeEditor: function () {
        if (monacoEditorInstance) {
            monacoEditorInstance.dispose();
            monacoEditorInstance = null;
        }

        monacoEditorDisposables.forEach(disposable => {
            if (disposable && disposable.dispose) {
                disposable.dispose();
            }
        });
        monacoEditorDisposables = [];
    },

    /**
     * Resize the editor (call after container resize)
     */
    layout: function () {
        if (monacoEditorInstance) {
            monacoEditorInstance.layout();
        }
    }
};

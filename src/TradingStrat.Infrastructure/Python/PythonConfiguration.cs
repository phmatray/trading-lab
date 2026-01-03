namespace TradingStrat.Infrastructure.Python;

/// <summary>
/// Configuration options for Python.NET integration.
/// Loaded from appsettings.json under "Trading:Python" section.
/// </summary>
public class PythonConfiguration
{
    /// <summary>
    /// Whether Python strategy support is enabled.
    /// Set to false to disable Python execution entirely.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional explicit path to Python DLL/shared library.
    /// If null, auto-detection will be used based on platform:
    /// - Windows: C:\Python312\python312.dll
    /// - macOS: /opt/homebrew/opt/python@3.12/...
    /// - Linux: libpython3.12.so
    /// </summary>
    public string? PythonDllPath { get; set; }

    /// <summary>
    /// Maximum time allowed for Python initialize() execution.
    /// Default: 30 seconds. Prevents long-running initialization from blocking startup.
    /// </summary>
    public int InitializeTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum time allowed for Python generate_signal() execution per bar.
    /// Default: 5 seconds. Prevents infinite loops from hanging backtests.
    /// </summary>
    public int GenerateSignalTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Maximum memory usage in MB for Python execution.
    /// Default: 512 MB. Soft limit enforced via monitoring.
    /// </summary>
    public int MaxMemoryMB { get; set; } = 512;

    /// <summary>
    /// List of allowed Python libraries for import.
    /// Security whitelist enforced via custom __import__ hook.
    /// </summary>
    public List<string> AllowedLibraries { get; set; } =
    [
        "numpy", "np",
        "pandas", "pd",
        "talib",
        "math",
        "datetime",
        "collections",
        "itertools",
        "functools",
        "re",
        "json",
        "decimal",
        "fractions"
    ];
}

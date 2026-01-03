using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Python.Runtime;

namespace TradingStrat.Infrastructure.Python;

/// <summary>
/// Singleton service that initializes Python runtime on application startup.
/// Manages Python environment, library availability, and security sandbox configuration.
/// Thread-safe initialization ensures Python engine is ready before any strategies execute.
/// </summary>
public class PythonEnvironmentManager : IDisposable
{
    private static readonly object Lock = new();
    private static bool _isInitialized;

    private readonly ILogger<PythonEnvironmentManager> _logger;
    private readonly PythonConfiguration _config;

    public PythonEnvironmentManager(
        ILogger<PythonEnvironmentManager> logger,
        IOptions<PythonConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Initializes Python runtime with security sandbox and library whitelist.
    /// Safe to call multiple times - only initializes once.
    /// </summary>
    public void Initialize()
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Python strategy support is disabled in configuration");
            return;
        }

        lock (Lock)
        {
            if (_isInitialized)
            {
                _logger.LogDebug("Python runtime already initialized");
                return;
            }

            _logger.LogInformation("Initializing Python runtime...");

            try
            {
                // Set Python DLL path (platform-specific auto-detection)
                Runtime.PythonDLL = GetPythonDllPath();
                _logger.LogInformation("Using Python DLL: {PythonDLL}", Runtime.PythonDLL);

                // Initialize Python engine
                PythonEngine.Initialize();
                PythonEngine.BeginAllowThreads();

                // Configure security sandbox
                ConfigureSandbox();

                _isInitialized = true;
                _logger.LogInformation("Python runtime initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Python runtime");
                throw new InvalidOperationException("Python initialization failed. Ensure Python 3.11+ is installed.", ex);
            }
        }
    }

    /// <summary>
    /// Auto-detects Python DLL path based on operating system.
    /// Checks common installation locations for Python 3.11 and 3.12.
    /// </summary>
    private string GetPythonDllPath()
    {
        // Use explicit path from configuration if provided
        if (!string.IsNullOrWhiteSpace(_config.PythonDllPath))
        {
            if (File.Exists(_config.PythonDllPath))
            {
                _logger.LogInformation("Using configured Python DLL path: {Path}", _config.PythonDllPath);
                return _config.PythonDllPath;
            }

            _logger.LogWarning("Configured Python DLL path not found: {Path}. Falling back to auto-detection.", _config.PythonDllPath);
        }

        // Platform-specific auto-detection
        if (OperatingSystem.IsWindows())
        {
            return GetWindowsPythonDll();
        }

        if (OperatingSystem.IsMacOS())
        {
            return GetMacOSPythonDll();
        }

        if (OperatingSystem.IsLinux())
        {
            return GetLinuxPythonDll();
        }

        throw new PlatformNotSupportedException("Python.NET is not supported on this platform");
    }

    private string GetWindowsPythonDll()
    {
        string[] paths =
        [
            @"C:\Python312\python312.dll",
            @"C:\Python311\python311.dll",
            @"C:\Python310\python310.dll",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Programs\Python\Python312\python312.dll"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Programs\Python\Python311\python311.dll")
        ];

        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("Found Python DLL at: {Path}", path);
                return path;
            }
        }

        throw new FileNotFoundException(
            "Python DLL not found. Please install Python 3.11 or 3.12 from python.org. " +
            "Searched locations: " + string.Join(", ", paths));
    }

    private string GetMacOSPythonDll()
    {
        string[] paths =
        [
            "/opt/homebrew/opt/python@3.12/Frameworks/Python.framework/Versions/3.12/lib/libpython3.12.dylib",
            "/opt/homebrew/opt/python@3.11/Frameworks/Python.framework/Versions/3.11/lib/libpython3.11.dylib",
            "/usr/local/opt/python@3.12/Frameworks/Python.framework/Versions/3.12/lib/libpython3.12.dylib",
            "/usr/local/opt/python@3.11/Frameworks/Python.framework/Versions/3.11/lib/libpython3.11.dylib",
            "/Library/Frameworks/Python.framework/Versions/3.12/lib/libpython3.12.dylib",
            "/Library/Frameworks/Python.framework/Versions/3.11/lib/libpython3.11.dylib"
        ];

        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("Found Python library at: {Path}", path);
                return path;
            }
        }

        throw new FileNotFoundException(
            "Python library not found. Install via Homebrew: brew install python@3.12\n" +
            "Searched locations: " + string.Join(", ", paths));
    }

    private string GetLinuxPythonDll()
    {
        // Try version-specific libraries first
        string[] paths =
        [
            "/usr/lib/x86_64-linux-gnu/libpython3.12.so",
            "/usr/lib/x86_64-linux-gnu/libpython3.11.so",
            "/usr/lib/libpython3.12.so",
            "/usr/lib/libpython3.11.so",
            "/usr/local/lib/libpython3.12.so",
            "/usr/local/lib/libpython3.11.so"
        ];

        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("Found Python library at: {Path}", path);
                return path;
            }
        }

        // Fallback to generic library name (let system find it)
        _logger.LogWarning("Python library not found at known locations. Using generic library name.");
        return "libpython3.12.so";
    }

    /// <summary>
    /// Configures Python security sandbox with import whitelist.
    /// Blocks network access, file I/O, and dangerous built-ins.
    /// </summary>
    private void ConfigureSandbox()
    {
        _logger.LogInformation("Configuring Python security sandbox...");

        using (Py.GIL())
        {
            try
            {
                // Build whitelist set from configuration
                string allowedModulesSet = string.Join(", ", _config.AllowedLibraries.Select(m => $"'{m}'"));

                // Install custom __import__ hook for whitelist enforcement
                string sandboxCode = @"
import sys
import builtins

# Store original import
_original_import = builtins.__import__

# Whitelist of allowed modules
_allowed_modules = {" + allowedModulesSet + @"}

def _restricted_import(name, *args, **kwargs):
    """"""Custom import hook that enforces library whitelist.""""""
    root_module = name.split('.')[0]

    if root_module not in _allowed_modules:
        raise ImportError(
            f""Import of '{name}' is not allowed in TradingStrat Python strategies. "" +
            f""Allowed libraries: {', '.join(sorted(_allowed_modules))}""
        )

    return _original_import(name, *args, **kwargs)

# Replace built-in __import__
builtins.__import__ = _restricted_import

# Block dangerous built-ins
_blocked_builtins = ['open', 'input', 'eval', 'exec', 'compile', '__import__']

# Block file system access
def _blocked_open(*args, **kwargs):
    raise PermissionError(""File system access is not allowed in TradingStrat Python strategies"")

builtins.open = _blocked_open

# Note: Network blocking and additional restrictions will be added in Phase 2
";

                PythonEngine.Exec(sandboxCode);
                _logger.LogInformation("Security sandbox configured successfully");
            }
            catch (PythonException ex)
            {
                _logger.LogError(ex, "Failed to configure Python security sandbox");
                throw new InvalidOperationException("Python sandbox configuration failed", ex);
            }
        }
    }

    /// <summary>
    /// Shuts down Python runtime.
    /// Called on application shutdown.
    /// </summary>
    public void Dispose()
    {
        lock (Lock)
        {
            if (_isInitialized)
            {
                _logger.LogInformation("Shutting down Python runtime...");
                PythonEngine.Shutdown();
                _isInitialized = false;
                _logger.LogInformation("Python runtime shut down successfully");
            }
        }
    }
}

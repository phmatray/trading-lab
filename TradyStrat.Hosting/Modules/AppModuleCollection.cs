using System.Collections;
using System.Reflection;

namespace TheAppManager.Modules;

public sealed class AppModuleCollection : IEnumerable<IAppModule>
{
    private readonly List<IAppModule> _modules = [];

    public AppModuleCollection Add<TModule>() where TModule : IAppModule, new()
    {
        _modules.Add(new TModule());
        return this;
    }

    public AppModuleCollection Add(IAppModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        _modules.Add(module);
        return this;
    }

    public AppModuleCollection AddIf<TModule>(bool condition) where TModule : IAppModule, new()
    {
        if (condition) _modules.Add(new TModule());
        return this;
    }

    public AppModuleCollection AddFromAssemblyOf<TMarker>()
        => AddFromAssembly(typeof(TMarker).Assembly);

    public AppModuleCollection AddFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        foreach (var module in ModuleDiscovery.DiscoverModules(assembly))
            _modules.Add(module);
        return this;
    }

    public AppModuleCollection AddFromAssemblyOf<TMarker>(Func<Type, bool> predicate)
        => AddFromAssembly(typeof(TMarker).Assembly, predicate);

    public AppModuleCollection AddFromAssembly(Assembly assembly, Func<Type, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(predicate);
        foreach (var module in ModuleDiscovery.DiscoverModules(assembly))
            if (predicate(module.GetType()))
                _modules.Add(module);
        return this;
    }

    public AppModuleCollection Replace<TOld, TNew>()
        where TOld : IAppModule
        where TNew : IAppModule, new()
    {
        var index = _modules.FindIndex(m => m is TOld);
        if (index < 0)
            throw new InvalidOperationException(
                $"No module of type {typeof(TOld).FullName} is registered.");
        _modules[index] = new TNew();
        return this;
    }

    public IReadOnlyList<IAppModule> GetModules() => _modules;

    public IEnumerator<IAppModule> GetEnumerator() => _modules.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

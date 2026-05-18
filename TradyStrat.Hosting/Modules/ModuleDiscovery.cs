using System.Reflection;

namespace TheAppManager.Modules;

public static class ModuleDiscovery
{
    public static IEnumerable<IAppModule> DiscoverModules(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;
            if (!typeof(IAppModule).IsAssignableFrom(type)) continue;
            if (type.GetConstructor(Type.EmptyTypes) is null) continue;

            yield return (IAppModule)Activator.CreateInstance(type)!;
        }
    }
}

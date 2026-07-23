using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

namespace TradyStrat.Cli;

/// <summary>
/// Spectre.Console.Cli registrar that records Spectre's command/settings type
/// registrations and resolves them against a scope opened on the host's
/// <see cref="IServiceProvider"/>. The per-invocation scope is required because
/// the application's use cases (and the EF <c>DbContext</c> they consume) are
/// registered as Scoped, and the root provider's scope validator rejects them.
/// </summary>
internal sealed class HostTypeRegistrar : ITypeRegistrar
{
    private readonly List<(Type Service, Type Impl)> _typeRegs = [];
    private readonly List<(Type Service, object Instance)> _instanceRegs = [];
    private readonly List<(Type Service, Func<object> Factory)> _lazyRegs = [];
    private IHost? _host;

    /// <summary>Call after <c>builder.Build()</c> to wire the resolver.</summary>
    public void BindHost(IHost host) => _host = host;

    public ITypeResolver Build()
    {
        if (_host is null)
            throw new InvalidOperationException(
                "HostTypeRegistrar.BindHost(host) must be called after builder.Build() and before CommandApp.Run.");
        // One scope per command invocation. Spectre's CommandApp doesn't dispose
        // the resolver, so the scope leaks until process exit — acceptable for a
        // one-shot CLI.
        var scope = _host.Services.CreateScope();
        return new HostTypeResolver(_typeRegs, _instanceRegs, _lazyRegs, scope.ServiceProvider);
    }

    public void Register(Type service, Type implementation) =>
        _typeRegs.Add((service, implementation));

    public void RegisterInstance(Type service, object implementation) =>
        _instanceRegs.Add((service, implementation));

    public void RegisterLazy(Type service, Func<object> func) =>
        _lazyRegs.Add((service, func));

    private sealed class HostTypeResolver(
        IReadOnlyList<(Type Service, Type Impl)> typeRegs,
        IReadOnlyList<(Type Service, object Instance)> instanceRegs,
        IReadOnlyList<(Type Service, Func<object> Factory)> lazyRegs,
        IServiceProvider scoped) : ITypeResolver
    {
        public object? Resolve(Type? type)
        {
            if (type is null) return null;

            // Last-registered-wins (mirrors typical Spectre expectations).
            for (int i = instanceRegs.Count - 1; i >= 0; i--)
                if (instanceRegs[i].Service == type) return instanceRegs[i].Instance;

            for (int i = lazyRegs.Count - 1; i >= 0; i--)
                if (lazyRegs[i].Service == type) return lazyRegs[i].Factory();

            for (int i = typeRegs.Count - 1; i >= 0; i--)
                if (typeRegs[i].Service == type)
                    return ActivatorUtilities.CreateInstance(scoped, typeRegs[i].Impl);

            return scoped.GetService(type);
        }
    }
}

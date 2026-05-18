using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

namespace TradyStrat.Cli;

/// <summary>
/// Spectre.Console.Cli registrar that bridges Spectre's command/settings type
/// registrations to a built <see cref="IHost"/>.
///
/// Two providers, queried in order:
///   1. <c>_spectreServices</c> — mutable IServiceCollection that Spectre fills
///      with command + settings types. Built lazily when Spectre asks for the
///      resolver.
///   2. <c>_host.Services</c> — the long-lived application provider built by
///      <c>HostApplicationBuilder</c>, which started IHostedService instances
///      and configured logging.
///
/// Spectre asks the resolver for command types first; everything else
/// (IAiClient, IRepositoryBase&lt;T&gt;, etc.) flows through to the host.
/// </summary>
internal sealed class HostTypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _spectreServices = new ServiceCollection();
    private IHost? _host;

    /// <summary>Call after <c>builder.Build()</c> to wire the fallback provider.</summary>
    public void BindHost(IHost host) => _host = host;

    public ITypeResolver Build()
    {
        if (_host is null)
            throw new InvalidOperationException(
                "HostTypeRegistrar.BindHost(host) must be called after builder.Build() and before CommandApp.Run.");
        var spectreProvider = _spectreServices.BuildServiceProvider();
        return new CombinedTypeResolver(spectreProvider, _host.Services);
    }

    public void Register(Type service, Type implementation) =>
        _spectreServices.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) =>
        _spectreServices.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> func) =>
        _spectreServices.AddSingleton(service, _ => func());

    private sealed class CombinedTypeResolver(IServiceProvider spectre, IServiceProvider host) : ITypeResolver
    {
        // Spectre owns its own provider; the host owns its own. Neither disposes
        // the other. The host provider is disposed by `using var host = builder.Build()`.
        public object? Resolve(Type? type)
        {
            if (type is null) return null;
            return spectre.GetService(type) ?? host.GetService(type);
        }
    }
}

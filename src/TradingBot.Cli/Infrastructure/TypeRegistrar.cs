// <copyright file="TypeRegistrar.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace TradingBot.Cli.Infrastructure;

/// <summary>
/// Type registrar for integrating Microsoft.Extensions.DependencyInjection with Spectre.Console.Cli.
/// </summary>
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public TypeRegistrar(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <inheritdoc/>
    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    /// <inheritdoc/>
    public void Register(Type service, Type implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    /// <inheritdoc/>
    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    /// <inheritdoc/>
    public void RegisterLazy(Type service, Func<object> factory)
    {
        _services.AddSingleton(service, _ => factory());
    }

    private sealed class TypeResolver : ITypeResolver, IDisposable
    {
        private readonly IServiceProvider _provider;

        public TypeResolver(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public object? Resolve(Type? type)
        {
            return type == null ? null : _provider.GetService(type);
        }

        public void Dispose()
        {
            if (_provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

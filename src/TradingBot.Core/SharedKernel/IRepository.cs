// <copyright file="IRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.Specification;

namespace TradingBot.Core.SharedKernel;

/// <summary>
/// An abstraction for persistence, based on Ardalis.Specification.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T> : IRepositoryBase<T>
  where T : class, IAggregateRoot;

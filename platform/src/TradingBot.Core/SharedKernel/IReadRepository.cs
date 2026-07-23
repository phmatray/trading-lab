// <copyright file="IReadRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.Specification;

namespace TradingBot.Core.SharedKernel;

/// <summary>
/// An abstraction for read only persistence operations, based on Ardalis.Specification.
/// Use this primarily to fetch trackable domain entities, not for custom queries.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IReadRepository<T> : IReadRepositoryBase<T>
  where T : class, IAggregateRoot;

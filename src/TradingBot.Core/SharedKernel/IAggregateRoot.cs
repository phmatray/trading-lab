// <copyright file="IAggregateRoot.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.SharedKernel;

/// <summary>
/// Apply this marker interface only to aggregate root entities in your domain model.
/// Your repository implementation can use constraints to ensure it only operates on aggregate roots.
/// </summary>
public interface IAggregateRoot;

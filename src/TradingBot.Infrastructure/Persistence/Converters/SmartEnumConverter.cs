// <copyright file="SmartEnumConverter.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TradingBot.Core.Common;

namespace TradingBot.Infrastructure.Persistence.Converters;

/// <summary>
/// Value converter for SmartEnum types to store the underlying value in the database.
/// </summary>
/// <typeparam name="TEnum">The SmartEnum type.</typeparam>
/// <typeparam name="TValue">The underlying value type.</typeparam>
public class SmartEnumConverter<TEnum, TValue> : ValueConverter<TEnum, TValue>
    where TEnum : SmartEnum<TEnum, TValue>
    where TValue : IEquatable<TValue>, IComparable<TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SmartEnumConverter{TEnum, TValue}"/> class.
    /// </summary>
    public SmartEnumConverter()
        : base(
            smartEnum => smartEnum.Value,
            value => SmartEnum<TEnum, TValue>.FromValue(value))
    {
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.BasicApp.Contracts.Seeding;

namespace XiHan.BasicApp.Runtime.Seeding;

/// <summary>
/// 种子登记。
/// </summary>
/// <remarks>
/// 各包在 <c>Configure</c> 里 <c>AddSeeder&lt;T&gt;()</c>，宿主的 <c>seed</c> 命令经 <see cref="SeedRunner"/> 执行。
/// </remarks>
public static class SeedingServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个种子（作用域服务，每次执行在自己的作用域里构造）。
    /// </summary>
    public static IServiceCollection AddSeeder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSeeder>(this IServiceCollection services)
        where TSeeder : class, ISeeder
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISeeder, TSeeder>());
        return services;
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 租户版本权限白名单重算种子数据
/// </summary>
/// <remarks>
/// 与 <see cref="SaasTenantEditionSeeder"/>（Order 21）同一套幂等逻辑，在全部模块权限种子
/// （代码生成 100+、AI 200+、工作流 300+、打印 500+ 等）之后再执行一遍：版本白名单按
/// 「全部已启用权限减平台专属」重算，使外部模块权限在首次启动即进入企业版白名单，
/// 而不是等第二次启动才自愈。
/// </remarks>
public sealed class SaasTenantEditionReconcileSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasTenantEditionReconcileSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : SaasTenantEditionSeeder(clientResolver, logger, serviceProvider, currentTenant)
{
    /// <summary>
    /// 种子数据优先级（晚于全部模块权限种子段）
    /// </summary>
    public override int Order => 900;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]租户版本权限白名单重算种子数据";
}

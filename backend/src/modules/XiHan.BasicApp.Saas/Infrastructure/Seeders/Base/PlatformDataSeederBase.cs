// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Initializers;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 平台种子基类：整个播种过程在平台上下文（TenantId = 0）内执行，只播平台库
/// </summary>
/// <remarks>
/// 操作、资源、权限、菜单、套餐、参数这类记录属于平台，按 <c>BasicAppEntity</c> 的约定必须落在 TenantId = 0。
/// 它们的 TenantId 由写入拦截器按当时的租户上下文注入，而应用启动播种时上下文未必是平台，因此在这里显式切换。
/// 不切换的后果是静默的：行照常写入却落在了别的租户下，按 TenantId = 0 查找的一方（如菜单解析权限）都查不到。
/// 要写业务租户数据的种子（演示数据）在自己的代码里逐个切入目标租户。
/// </remarks>
[DataSeeding(Target = DbInitializationTarget.Platform)]
public abstract class PlatformDataSeederBase : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    protected PlatformDataSeederBase(ISqlSugarClientResolver clientResolver, ILogger logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 当前租户上下文（切换租户用）
    /// </summary>
    protected ICurrentTenant CurrentTenant => ServiceProvider.GetRequiredService<ICurrentTenant>();

    /// <summary>
    /// 在平台上下文内执行播种
    /// </summary>
    public override async Task SeedAsync()
    {
        using var platformScope = CurrentTenant.Change(null);
        await base.SeedAsync();
    }
}

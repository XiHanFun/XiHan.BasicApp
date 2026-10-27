// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Identity.Queries;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份能力包（Tier 1）。N0 是最小切片：<c>SysUser</c>（平台库）与 <c>SysUserStatistics</c>（租户库）两个实体，<c>users.create / get / list</c> 三个操作；
/// 登录、令牌、会话、2FA、第三方登录与模仿在 N1 补齐。装配显式：仓储、查询、权限、操作都在这里登记，没有约定扫描。
/// </summary>
public sealed class IdentityFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "identity",
        Package = "XiHan.BasicApp.Identity",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(IdentityCapabilities.Identity, IdentityCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(MultiTenancyCapabilities.Context, MultiTenancyCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Contributes = new Contributions { Permissions = IdentityPermissions.All.Select(static p => p.Code).ToList() },
        Entry = typeof(IdentityFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(IdentityMessages.Catalog);
        // 仓储登记由 Persistence.Generators 从实体类生成
        context.Services.AddGeneratedRepositories();
        context.Services.AddScoped<UserQueries>();
        // 权限码先登记，操作契约才能引用；未登记的权限码在装配时报错
        foreach (var permission in IdentityPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        // 操作登记由 Kernel.Generators 从 [Operation] 生成（含请求 / 响应 JSON Schema）
        context.AddGeneratedOperations();
    }
}

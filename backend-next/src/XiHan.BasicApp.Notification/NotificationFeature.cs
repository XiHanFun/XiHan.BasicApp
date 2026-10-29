// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Queries;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知能力包（Tier 2）：通知公告（租户库实体，平台一侧租户为 <c>platform</c>）与收件箱。接收人经 Contracts 的 <see cref="Contracts.Users.IRecipientSource"/> 解析
/// （Identity、Access、Organization 实现），推送经 <see cref="Contracts.Realtime.IRealtimePush"/>；邮件、短信与机器人渠道、模板随第五批后续步骤补入。
/// </summary>
public sealed class NotificationFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "notification",
        Package = "XiHan.BasicApp.Notification",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(NotificationCapabilities.Notification, NotificationCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(MultiTenancyCapabilities.Context, MultiTenancyCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = NotificationPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(NotificationFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(NotificationMessages.Catalog);
        context.Services.AddGeneratedRepositories();
        context.Services.AddScoped<NotificationService>();
        context.Services.AddScoped<InboxService>();
        context.Services.AddScoped<NotificationQueries>();
        context.Services.AddScoped<InboxQueries>();
        context.Services.AddPermissionCatalog("notification", NotificationPermissions.All);
        context.Services.AddPageCatalog("notification", NotificationPages.All);
        foreach (var permission in NotificationPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();
    }
}

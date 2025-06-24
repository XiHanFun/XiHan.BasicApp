#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppCoreModule
// Guid:dd5fa300-a07c-434f-9ac3-8d44c9476166
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/6/3 0:30:30
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using XiHan.Framework.AI;
using XiHan.Framework.Authorization;
using XiHan.Framework.BackgroundJobs;
using XiHan.Framework.BackgroundWorkers;
using XiHan.Framework.BlobStoring;
using XiHan.Framework.Bot;
using XiHan.Framework.Caching;
using XiHan.Framework.Caching.StackExchangeRedis;
using XiHan.Framework.CodeGeneration;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.DataFiltering;
using XiHan.Framework.Ddd.Application;
using XiHan.Framework.Ddd.Application.Contracts;
using XiHan.Framework.Ddd.Domain;
using XiHan.Framework.Ddd.Domain.Shared;
using XiHan.Framework.DistributedIds;
using XiHan.Framework.EventBus;
using XiHan.Framework.Gateway;
using XiHan.Framework.Http;
using XiHan.Framework.Http.Client;
using XiHan.Framework.Localization;
using XiHan.Framework.MultiTenancy;
using XiHan.Framework.ObjectMapping.Mapster;
using XiHan.Framework.Script;
using XiHan.Framework.SearchEngines;
using XiHan.Framework.Security;
using XiHan.Framework.Serialization;
using XiHan.Framework.Settings;
using XiHan.Framework.SqlSugarCore;
using XiHan.Framework.TextTemplating;
using XiHan.Framework.Threading;
using XiHan.Framework.Uow;
using XiHan.Framework.Utils.IO;
using XiHan.Framework.Validation;
using XiHan.Framework.VirtualFileSystem;
using XiHan.Framework.VirtualFileSystem.Options;

namespace XiHan.BasicApp.Core;

/// <summary>
/// XiHanBasicAppCore
/// </summary>
[DependsOn(
    typeof(XiHanAiModule),
    typeof(XiHanAuthorizationModule),
    typeof(XiHanBackgroundJobsModule),
    typeof(XiHanBackgroundWorkersModule),
    typeof(XiHanBlobStoringModule),
    typeof(XiHanBotModule),
    typeof(XiHanCachingModule),
    typeof(XiHanCachingStackExchangeRedisModule),
    typeof(XiHanCodeGenerationModule),
    typeof(XiHanDataFilteringModule),
    typeof(XiHanDddApplicationModule),
    typeof(XiHanDddApplicationContractsModule),
    typeof(XiHanDddDomainModule),
    typeof(XiHanDddDomainSharedModule),
    typeof(XiHanDistributedIdsModule),
    //typeof(XiHanEventBusModule),
    typeof(XiHanGatewayModule),
    typeof(XiHanHttpModule),
    typeof(XiHanHttpClientModule),
    typeof(XiHanLocalizationModule),
    //typeof(XiHanMultiTenancyModule),
    typeof(XiHanObjectMappingMapsterModule),
    typeof(XiHanScriptModule),
    typeof(XiHanSearchEnginesModule),
    typeof(XiHanSecurityModule),
    typeof(XiHanSerializationModule),
    //typeof(XiHanSettingsModule),
    typeof(XiHanSqlSugarCoreModule),
    typeof(XiHanTextTemplatingModule),
    typeof(XiHanThreadingModule),
    typeof(XiHanUowModule),
    typeof(XiHanValidationModule),
    typeof(XiHanVirtualFileSystemModule)
)]
public class XiHanBasicAppCoreModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // 配置虚拟文件系统的本地化资源目录
        Configure<VirtualFileSystemOptions>(config =>
        {
            _ = config
                .AddPhysical(DirectoryHelper.GetBaseDirectory())
                .AddPhysical("Localization/Resources");
        });
    }

    /// <summary>
    /// 应用初始化
    /// </summary>
    /// <param name="context"></param>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var serviceProvider = context.ServiceProvider;
        var virtualFileSystem = serviceProvider.GetRequiredService<IVirtualFileSystem>();

        // 订阅文件变化事件
        virtualFileSystem.OnFileChanged += (sender, args) =>
        {
            // 处理文件变化逻辑
            Console.WriteLine($"文件发生变化: {args.FilePath} {args.ChangeType}");
        };
        _ = virtualFileSystem.Watch("*.*");
    }
}

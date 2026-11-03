// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Services;
using XiHan.BasicApp.CodeGeneration.Queries;
using XiHan.BasicApp.CodeGeneration.Seeds;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 代码生成模块（Tier 3，可选）。
/// </summary>
/// <remarks>
/// 只在平台一侧（照 4.x）：外部数据源、表与列配置（经框架读库结构导入）、Scriban 模板、按 6.0 写法生成包与前端模块、生成历史与零代码只读运行时。
/// 外部库经框架的运行时数据源连接（<c>DataSourceRegistry</c>），口令与连接串经 <c>ISecretProtector</c> 加密存储。
/// 宿主按 <c>BasicApp:Modules:CodeGeneration:Enabled</c> 决定是否装配；关掉后权限目录与页面登记都不在，菜单种子删掉代码生成页，前端模块随菜单不再出现。
/// </remarks>
public sealed class CodeGenerationFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "codegeneration",
        Package = "XiHan.BasicApp.CodeGeneration",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(CodeGenerationCapabilities.CodeGeneration, CodeGenerationCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = CodeGenerationPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(CodeGenerationFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(CodeGenerationMessages.Catalog);
        context.Services.AddGeneratedRepositories();
        context.Services.AddSingleton(static services => CodeGenerationOptions.FromConfiguration(services.GetRequiredService<IConfiguration>()));
        context.Services.AddScoped<CodeGenDataSourceService>();
        context.Services.AddScoped<CodegenDatasourceQueries>();
        context.Services.AddScoped<CodeGenInference>();
        context.Services.AddScoped<CodeGenTableService>();
        context.Services.AddScoped<CodegenTableQueries>();
        context.Services.AddSingleton<CodeGenEnumCatalog>();
        context.Services.AddScoped<CodeGenContextBuilder>();
        context.Services.AddScoped<CodeGenEngine>();
        context.Services.AddScoped<CodeGenTemplateService>();
        context.Services.AddScoped<CodegenTemplateQueries>();
        context.Services.AddScoped<CodeGenProjectWriter>();
        context.Services.AddScoped<CodeGenGenerationService>();
        context.Services.AddScoped<CodegenHistoryQueries>();
        context.Services.AddScoped<CodeGenRuntimeService>();
        context.Services.AddSeeder<CodeGenTemplateSeeder>();
        context.Services.AddPermissionCatalog("codegeneration", CodeGenerationPermissions.All);
        // 字段安全：实体由响应类型生成，读、建、改的规则由 Access 的中间件统一套用（照 4.x 的登记范围）
        CodeGenerationFieldSecurity.Register(context.Services);
        context.Services.AddPageCatalog("codegeneration", CodeGenerationPages.All);
        foreach (var permission in CodeGenerationPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();
    }
}

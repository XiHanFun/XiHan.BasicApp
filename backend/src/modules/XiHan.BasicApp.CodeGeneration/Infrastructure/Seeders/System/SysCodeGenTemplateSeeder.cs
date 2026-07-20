#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysCodeGenTemplateSeeder
// Guid:5c4b3a2d-1e0f-4a9b-8c7d-6e5f4a3b2c1d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/21 10:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Seeders.System;

/// <summary>
/// 代码生成内置模板种子数据
/// 把 Templates/Backend/*.sbn 与 Templates/Frontend/*.sbn 作为嵌入资源种入 SysCodeGenTemplate(IsBuiltIn=true)
/// </summary>
public class SysCodeGenTemplateSeeder : DataSeederBase
{
    /// <summary>
    /// 内置模板分组
    /// </summary>
    private const string BackendCrudGroup = "backend-crud";

    /// <summary>
    /// 前端内置模板分组
    /// </summary>
    private const string FrontendCrudGroup = "frontend-crud";

    /// <summary>
    /// 前端接口路径表达式（模块小写目录）
    /// </summary>
    private const string FrontendApiPath = "src/api/modules/{{ ModuleName | string.downcase }}";

    /// <summary>
    /// 前端页面路径表达式（模块小写目录 + 实体 kebab 目录）
    /// </summary>
    private const string FrontendPagePath = "src/views/{{ ModuleName | string.downcase }}/{{ ClassNameKebab }}";

    /// <summary>
    /// 内置模板定义（嵌入资源后缀 → 模板元信息）
    /// 资源后缀按 ".Templates." + ResourceFile.Replace("/", ".") 与 GetManifestResourceNames() 匹配，
    /// 同时兼容 Backend/ 与 Frontend/ 子目录，避免硬编码命名空间出错。
    /// </summary>
    /// <remarks>
    /// 每个产物成对登记：机器模板（<see cref="ArtifactWriteMode.AlwaysOverwrite"/>，重新生成时覆盖）
    /// 与人类模板（<see cref="ArtifactWriteMode.WriteOnce"/>，仅首次创建、此后永不触碰）。
    /// 二者在语言层面拼接：L1 数据类经 partial 合并；L2 行为类经抽象基类与具体派生类继承；
    /// L3 前端经 re-export / transform 组合。前端页面 index.vue 无机器侧，整体归人类所有。
    /// </remarks>
    private static readonly IReadOnlyList<BuiltInTemplate> BuiltInTemplates =
    [
        // L1 数据层：partial 拆分
        new("backend.entity", "后端实体", BackendCrudGroup, "Backend/Entity.sbn", "{{ ClassName }}.Generated.cs", ".cs", "Domain/Entities"),
        new("backend.entity.manual", "后端实体（自定义）", BackendCrudGroup, "Backend/Entity.Manual.sbn", "{{ ClassName }}.cs", ".cs", "Domain/Entities", ArtifactWriteMode.WriteOnce),
        new("backend.dtos", "后端DTO", BackendCrudGroup, "Backend/Dtos.sbn", "{{ ClassName }}Dtos.Generated.cs", ".cs", "Application/Dtos"),
        new("backend.dtos.manual", "后端DTO（自定义）", BackendCrudGroup, "Backend/Dtos.Manual.sbn", "{{ ClassName }}Dtos.cs", ".cs", "Application/Dtos", ArtifactWriteMode.WriteOnce),
        new("backend.irepository", "后端仓储接口", BackendCrudGroup, "Backend/IRepository.sbn", "I{{ ClassName }}Repository.Generated.cs", ".cs", "Domain/Repositories"),
        new("backend.irepository.manual", "后端仓储接口（自定义）", BackendCrudGroup, "Backend/IRepository.Manual.sbn", "I{{ ClassName }}Repository.cs", ".cs", "Domain/Repositories", ArtifactWriteMode.WriteOnce),
        new("backend.repository", "后端仓储实现", BackendCrudGroup, "Backend/Repository.sbn", "{{ ClassName }}Repository.Generated.cs", ".cs", "Infrastructure/Repositories"),
        new("backend.repository.manual", "后端仓储实现（自定义）", BackendCrudGroup, "Backend/Repository.Manual.sbn", "{{ ClassName }}Repository.cs", ".cs", "Infrastructure/Repositories", ArtifactWriteMode.WriteOnce),
        new("backend.contracts", "后端应用契约", BackendCrudGroup, "Backend/Contracts.sbn", "I{{ ClassName }}Contracts.Generated.cs", ".cs", "Application/Contracts"),
        new("backend.contracts.manual", "后端应用契约（自定义）", BackendCrudGroup, "Backend/Contracts.Manual.sbn", "I{{ ClassName }}Contracts.cs", ".cs", "Application/Contracts", ArtifactWriteMode.WriteOnce),
        new("backend.mapper", "后端对象映射", BackendCrudGroup, "Backend/Mapper.sbn", "{{ ClassName }}ApplicationMapper.Generated.cs", ".cs", "Application/Mappers"),
        new("backend.mapper.manual", "后端对象映射（自定义）", BackendCrudGroup, "Backend/Mapper.Manual.sbn", "{{ ClassName }}ApplicationMapper.cs", ".cs", "Application/Mappers", ArtifactWriteMode.WriteOnce),

        // L2 行为层：Generation Gap（生成抽象基类 + 手写具体派生类）
        new("backend.appservice", "后端应用服务基类", BackendCrudGroup, "Backend/AppService.sbn", "{{ ClassName }}AppServiceBase.Generated.cs", ".cs", "Application/AppServices"),
        new("backend.appservice.manual", "后端应用服务", BackendCrudGroup, "Backend/AppService.Manual.sbn", "{{ ClassName }}AppService.cs", ".cs", "Application/AppServices", ArtifactWriteMode.WriteOnce),
        new("backend.queryservice", "后端查询服务基类", BackendCrudGroup, "Backend/QueryService.sbn", "{{ ClassName }}QueryServiceBase.Generated.cs", ".cs", "Application/QueryServices"),
        new("backend.queryservice.manual", "后端查询服务", BackendCrudGroup, "Backend/QueryService.Manual.sbn", "{{ ClassName }}QueryService.cs", ".cs", "Application/QueryServices", ArtifactWriteMode.WriteOnce),

        // L3 前端层：生成 base + 手写 re-export / transform
        new("frontend.types", "前端类型定义", FrontendCrudGroup, "Frontend/Types.sbn", "{{ ClassNameKebab }}.types.generated.ts", ".ts", FrontendApiPath),
        new("frontend.types.manual", "前端类型定义（自定义）", FrontendCrudGroup, "Frontend/Types.Manual.sbn", "{{ ClassNameKebab }}.types.ts", ".ts", FrontendApiPath, ArtifactWriteMode.WriteOnce),
        new("frontend.api", "前端接口请求", FrontendCrudGroup, "Frontend/Api.sbn", "{{ ClassNameKebab }}.generated.ts", ".ts", FrontendApiPath),
        new("frontend.api.manual", "前端接口请求（自定义）", FrontendCrudGroup, "Frontend/Api.Manual.sbn", "{{ ClassNameKebab }}.ts", ".ts", FrontendApiPath, ArtifactWriteMode.WriteOnce),
        new("frontend.schema", "前端页面Schema", FrontendCrudGroup, "Frontend/Schema.sbn", "{{ ClassNameKebab }}.schema.generated.ts", ".ts", FrontendPagePath),
        new("frontend.schema.manual", "前端页面Schema（自定义）", FrontendCrudGroup, "Frontend/Schema.Manual.sbn", "{{ ClassNameKebab }}.schema.ts", ".ts", FrontendPagePath, ArtifactWriteMode.WriteOnce),
        new("frontend.page", "前端列表页面", FrontendCrudGroup, "Frontend/Page.sbn", "index.vue", ".vue", FrontendPagePath, ArtifactWriteMode.WriteOnce),
    ];

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysCodeGenTemplateSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysCodeGenTemplateSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 105;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[CodeGeneration]代码生成内置模板种子";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var codes = BuiltInTemplates.Select(t => t.Code).ToList();
        var exists = await client.Queryable<SysCodeGenTemplate>().Where(t => codes.Contains(t.TemplateCode)).ToListAsync();
        var existsCodes = exists.Select(x => x.TemplateCode).ToHashSet();

        var assembly = typeof(SysCodeGenTemplateSeeder).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();
        var addList = new List<SysCodeGenTemplate>();
        var sort = 1;

        foreach (var template in BuiltInTemplates)
        {
            // 即使已存在也推进 Sort，保证插入项的排序与定义顺序一致
            var currentSort = sort++;

            if (existsCodes.Contains(template.Code))
            {
                continue;
            }

            // 资源名按 ".Templates." + ResourceFile（/ 转 .）后缀定位，兼容 Backend/ 与 Frontend/，避免硬编码命名空间
            var suffix = $".Templates.{template.ResourceFile.Replace("/", ".")}";
            var resourceName = resourceNames.FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (resourceName is null)
            {
                Logger.LogWarning("未找到内置模板嵌入资源：{Suffix}，跳过模板 {Code}", suffix, template.Code);
                continue;
            }

            string content;
            await using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream is null)
                {
                    Logger.LogWarning("内置模板嵌入资源流为空：{Resource}，跳过模板 {Code}", resourceName, template.Code);
                    continue;
                }

                using var reader = new StreamReader(stream);
                content = await reader.ReadToEndAsync();
            }

            addList.Add(new SysCodeGenTemplate
            {
                TemplateCode = template.Code,
                TemplateName = template.Name,
                TemplateGroup = template.Group,
                TemplateType = TemplateType.Single,
                TemplateEngine = TemplateEngine.Scriban,
                TemplateContent = content,
                FileExtension = template.FileExtension,
                FileNameExpression = template.FileNameExpression,
                FilePathExpression = template.FilePathExpression,
                WriteMode = template.WriteMode,
                IsBuiltIn = true,
                IsEnabled = true,
                Sort = currentSort,
                Status = EnableStatus.Enabled
            });
        }

        if (addList.Count == 0)
        {
            Logger.LogInformation("代码生成内置模板已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个代码生成内置模板", addList.Count);
    }

    /// <summary>
    /// 内置模板定义
    /// </summary>
    /// <param name="Code">模板编码</param>
    /// <param name="Name">模板名称</param>
    /// <param name="Group">模板分组</param>
    /// <param name="ResourceFile">嵌入资源相对路径（如 Backend/Entity.sbn 或 Frontend/Types.sbn）</param>
    /// <param name="FileNameExpression">生成文件名表达式</param>
    /// <param name="FileExtension">文件扩展名（如 .cs / .ts / .vue）</param>
    /// <param name="FilePathExpression">生成文件路径表达式（目录，可空）</param>
    /// <param name="WriteMode">写入策略（机器文件总是覆盖；人类文件仅首次创建）</param>
    private sealed record BuiltInTemplate(
        string Code,
        string Name,
        string Group,
        string ResourceFile,
        string FileNameExpression,
        string FileExtension,
        string? FilePathExpression,
        ArtifactWriteMode WriteMode = ArtifactWriteMode.AlwaysOverwrite);
}

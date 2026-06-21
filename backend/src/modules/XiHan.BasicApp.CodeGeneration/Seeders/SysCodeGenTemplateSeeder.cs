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

namespace XiHan.BasicApp.CodeGeneration.Seeders;

/// <summary>
/// 代码生成内置模板种子数据
/// 把 Templates/Backend/*.sbn 作为嵌入资源种入 SysCodeGenTemplate(IsBuiltIn=true)
/// </summary>
public class SysCodeGenTemplateSeeder : DataSeederBase
{
    /// <summary>
    /// 内置模板分组
    /// </summary>
    private const string BackendCrudGroup = "backend-crud";

    /// <summary>
    /// 内置模板定义（嵌入资源后缀 → 模板元信息）
    /// 资源后缀按 ".Templates.Backend.{File}.sbn" 与 GetManifestResourceNames() 匹配，避免硬编码命名空间出错。
    /// </summary>
    private static readonly IReadOnlyList<BuiltInTemplate> BuiltInTemplates =
    [
        new("backend.entity", "后端实体", BackendCrudGroup, "Entity.sbn", "{{ ClassName }}.cs"),
        new("backend.dtos", "后端DTO", BackendCrudGroup, "Dtos.sbn", "{{ ClassName }}Dtos.cs"),
        new("backend.irepository", "后端仓储接口", BackendCrudGroup, "IRepository.sbn", "I{{ ClassName }}Repository.cs"),
        new("backend.repository", "后端仓储实现", BackendCrudGroup, "Repository.sbn", "{{ ClassName }}Repository.cs"),
        new("backend.contracts", "后端应用契约", BackendCrudGroup, "Contracts.sbn", "I{{ ClassName }}Contracts.cs"),
        new("backend.mapper", "后端对象映射", BackendCrudGroup, "Mapper.sbn", "{{ ClassName }}ApplicationMapper.cs"),
        new("backend.appservice", "后端应用服务", BackendCrudGroup, "AppService.sbn", "{{ ClassName }}AppService.cs"),
        new("backend.queryservice", "后端查询服务", BackendCrudGroup, "QueryService.sbn", "{{ ClassName }}QueryService.cs"),
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
    public override int Order => 34;

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

            // 资源名按 ".Templates.Backend.{File}" 后缀定位，避免硬编码命名空间
            var suffix = $".Templates.Backend.{template.ResourceFile}";
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
                FileExtension = ".cs",
                FileNameExpression = template.FileNameExpression,
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
    /// <param name="ResourceFile">嵌入资源文件名（Templates/Backend 下）</param>
    /// <param name="FileNameExpression">生成文件名表达式</param>
    private sealed record BuiltInTemplate(string Code, string Name, string Group, string ResourceFile, string FileNameExpression);
}

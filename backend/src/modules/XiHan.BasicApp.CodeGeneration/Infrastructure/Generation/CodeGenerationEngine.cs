#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenerationEngine
// Guid:c0de9e00-0307-4a00-9000-000000000307
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 代码生成引擎（管线编排：建模 → 选模板 → 渲染 → 产出）
/// </summary>
/// <remarks>
/// 本类已接通"配置 → 渲染 → 产物"主链路，并按模板声明的 <see cref="ArtifactWriteMode"/>
/// 区分机器产物（总是覆盖）与人类产物（仅首次创建），保证重新生成不冲掉手写代码。
/// 待完善：树表/主子表上下文扩展（见 M1-1）。
/// </remarks>
public sealed class CodeGenerationEngine(
    ICodeGenTableRepository tableRepository,
    ICodeGenTableColumnRepository columnRepository,
    ICodeGenTemplateRepository templateRepository,
    ITemplateRendererResolver rendererResolver,
    ITypeMappingProvider typeMappingProvider,
    IGeneratedArtifactPackager packager,
    IGeneratedArtifactWriter artifactWriter,
    ILogger<CodeGenerationEngine> logger) : ICodeGenerationEngine
{
    private readonly ICodeGenTableRepository _tableRepository = tableRepository;
    private readonly ICodeGenTableColumnRepository _columnRepository = columnRepository;
    private readonly ICodeGenTemplateRepository _templateRepository = templateRepository;
    private readonly ITemplateRendererResolver _rendererResolver = rendererResolver;
    private readonly ITypeMappingProvider _typeMappingProvider = typeMappingProvider;
    private readonly IGeneratedArtifactPackager _packager = packager;
    private readonly IGeneratedArtifactWriter _artifactWriter = artifactWriter;
    private readonly ILogger<CodeGenerationEngine> _logger = logger;

    /// <inheritdoc />
    public Task<GenerationResult> PreviewAsync(GenerationRequest request, CancellationToken cancellationToken = default)
        => RenderCoreAsync(request, cancellationToken);

    /// <inheritdoc />
    public async Task<GenerationResult> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await RenderCoreAsync(request, cancellationToken);
        if (!result.Success)
        {
            return result;
        }

        switch (request.GenType)
        {
            case GenType.Zip:
                result.Package = await _packager.PackAsync(result.Artifacts, cancellationToken);
                break;

            case GenType.CustomPath:
                // 受控落盘：默认禁用 + 白名单根目录 + 路径穿越校验（fail-closed），审计经生成历史留痕
                var table = await _tableRepository.GetByIdAsync(request.TableId, cancellationToken);
                var writeResult = await _artifactWriter.WriteAsync(result.Artifacts, table?.GenPath, cancellationToken);
                if (!writeResult.Success)
                {
                    return GenerationResult.Fail(writeResult.Message ?? "自定义路径落盘失败。");
                }

                result.WrittenCount = writeResult.WrittenCount;
                result.SkippedPaths = writeResult.SkippedPaths;

                _logger.LogInformation(
                    "代码生成落盘完成：TableId={TableId}，路径={Path}，写入={Written}，跳过={Skipped}（人类文件已存在）",
                    request.TableId, table?.GenPath, writeResult.WrittenCount, writeResult.SkippedCount);
                break;

            case GenType.Preview:
            default:
                break;
        }

        return result;
    }

    /// <summary>
    /// 渲染核心：加载配置 → 构建上下文 → 渲染模板 → 产出文件
    /// </summary>
    private async Task<GenerationResult> RenderCoreAsync(GenerationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();

        var table = await _tableRepository.GetByIdAsync(request.TableId, cancellationToken);
        if (table is null)
        {
            return GenerationResult.Fail($"代码生成表配置不存在：{request.TableId}");
        }

        var columns = await _columnRepository.GetByTableIdAsync(table.BasicId, cancellationToken);
        var context = BuildContext(table, columns);

        // 无显式模板编码时，按表的模板类型（单表/树表/主子表）选取通用模板集；
        // 模板不按业务模块过滤（CRUD 模板对所有模块通用，此前误用 ModuleName 作分组导致匹配为空）
        var templates = request.TemplateCodes is { Count: > 0 }
            ? await _templateRepository.GetByCodesAsync(request.TemplateCodes, cancellationToken)
            : await _templateRepository.GetEnabledByTypeAsync(table.TemplateType, cancellationToken);

        if (templates.Count == 0)
        {
            return GenerationResult.Fail("未找到可用模板（请检查模板类型/编码与启用状态）。");
        }

        var artifacts = new List<GeneratedArtifact>(templates.Count);
        foreach (var template in templates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var renderer = _rendererResolver.Resolve(template.TemplateEngine);
            var content = await renderer.RenderAsync(template.TemplateContent ?? string.Empty, context, cancellationToken);
            var fileName = await ResolveFileNameAsync(renderer, template, context, cancellationToken);
            var relativePath = await ResolveRelativePathAsync(renderer, template, context, fileName, cancellationToken);

            artifacts.Add(new GeneratedArtifact(relativePath, fileName, content, template.TemplateCode, template.WriteMode));
        }

        // 二阶产物：菜单 + 按钮权限代码片段（待并入源码 → 重建库经既有 Seeder 链生效，非运行时写库）
        artifacts.AddRange(MenuPermissionArtifactGenerator.Build(context));

        stopwatch.Stop();
        return GenerationResult.Ok(artifacts, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// 由表配置 + 列配置构建模板上下文
    /// </summary>
    private CodeGenerationContext BuildContext(SysCodeGenTable table, IReadOnlyList<SysCodeGenTableColumn> columns)
    {
        var columnSchemas = columns.Select(column => MapColumn(table, column)).ToList();

        return new CodeGenerationContext
        {
            TableName = table.TableName,
            TableComment = table.TableComment,
            ClassName = table.ClassName,
            Namespace = table.Namespace,
            ModuleName = table.ModuleName,
            BusinessName = table.BusinessName,
            FunctionName = table.FunctionName,
            Author = table.Author,
            TemplateType = table.TemplateType,
            Columns = columnSchemas,
            PrimaryKey = columnSchemas.FirstOrDefault(column => column.IsPrimaryKey)
                ?? columnSchemas.FirstOrDefault(column => column.ColumnName == table.PrimaryKeyColumn),
            // 树表/主子表结构字段 + 上级菜单透出给模板/二阶产物（模板按 TemplateType 消费 Options.XxxColumn）
            Options = new Dictionary<string, object?>
            {
                ["PrimaryKeyColumn"] = table.PrimaryKeyColumn,
                ["TreeParentColumn"] = table.TreeParentColumn,
                ["TreeNameColumn"] = table.TreeNameColumn,
                ["MasterTableId"] = table.MasterTableId?.ToString(),
                ["MasterForeignKey"] = table.MasterForeignKey,
                ["ParentMenuId"] = table.ParentMenuId?.ToString()
            }
        };
    }

    /// <summary>
    /// 列配置 → 列模型；C#/TS 类型缺失时回退到类型映射器
    /// </summary>
    private ColumnSchema MapColumn(SysCodeGenTable table, SysCodeGenTableColumn column)
    {
        var schema = new ColumnSchema
        {
            ColumnName = column.ColumnName,
            ColumnComment = column.ColumnComment,
            DbType = column.ColumnType,
            CSharpType = column.CSharpType ?? string.Empty,
            CSharpProperty = column.CSharpProperty ?? string.Empty,
            TsType = column.TsType ?? string.Empty,
            IsPrimaryKey = column.IsPrimaryKey,
            IsIdentity = column.IsIdentity,
            IsNullable = column.IsNullable,
            IsRequired = column.IsRequired,
            Length = column.ColumnLength,
            DecimalDigits = column.DecimalDigits,
            HtmlType = column.HtmlType,
            QueryType = column.QueryType,
            DictSelectorType = column.DictSelectorType,
            DictCode = column.DictCode,
            EnumTypeName = column.EnumTypeName,
            ConstValues = column.ConstValues
        };

        // 列配置未填类型时，按 DB 类型回退映射（导入流程会预填，此处为兜底）
        if (string.IsNullOrWhiteSpace(schema.CSharpType) || string.IsNullOrWhiteSpace(schema.TsType))
        {
            var mapping = _typeMappingProvider.Map(table.DatabaseType, column.ColumnType, column.IsNullable);
            if (string.IsNullOrWhiteSpace(schema.CSharpType))
            {
                schema.CSharpType = mapping.CSharpType;
            }

            if (string.IsNullOrWhiteSpace(schema.TsType))
            {
                schema.TsType = mapping.TsType;
            }
        }

        return schema;
    }

    /// <summary>
    /// 解析输出文件名（优先模板 FileNameExpression，回退 ClassName + 扩展名）
    /// </summary>
    private async Task<string> ResolveFileNameAsync(
        ITemplateRenderer renderer,
        SysCodeGenTemplate template,
        CodeGenerationContext context,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(template.FileNameExpression))
        {
            try
            {
                var rendered = await renderer.RenderAsync(template.FileNameExpression, context, cancellationToken);
                if (!string.IsNullOrWhiteSpace(rendered))
                {
                    return rendered.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "模板 {Code} 文件名表达式渲染失败，回退默认命名。", template.TemplateCode);
            }
        }

        var extension = string.IsNullOrWhiteSpace(template.FileExtension) ? ".cs" : template.FileExtension.Trim();
        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        return context.ClassName + extension;
    }

    /// <summary>
    /// 解析输出相对路径（优先模板 FilePathExpression 作为目录，拼接文件名）
    /// </summary>
    private async Task<string> ResolveRelativePathAsync(
        ITemplateRenderer renderer,
        SysCodeGenTemplate template,
        CodeGenerationContext context,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(template.FilePathExpression))
        {
            return fileName;
        }

        try
        {
            var directory = await renderer.RenderAsync(template.FilePathExpression, context, cancellationToken);
            directory = directory?.Trim().Replace('\\', '/').TrimEnd('/');
            return string.IsNullOrWhiteSpace(directory) ? fileName : $"{directory}/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "模板 {Code} 路径表达式渲染失败，回退无目录输出。", template.TemplateCode);
            return fileName;
        }
    }
}

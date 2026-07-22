// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Repositories;

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
    IPermissionRepository permissionRepository,
    ILogger<CodeGenerationEngine> logger) : ICodeGenerationEngine
{
    private readonly ICodeGenTableRepository _tableRepository = tableRepository;
    private readonly ICodeGenTableColumnRepository _columnRepository = columnRepository;
    private readonly ICodeGenTemplateRepository _templateRepository = templateRepository;
    private readonly ITemplateRendererResolver _rendererResolver = rendererResolver;
    private readonly ITypeMappingProvider _typeMappingProvider = typeMappingProvider;
    private readonly IGeneratedArtifactPackager _packager = packager;
    private readonly IGeneratedArtifactWriter _artifactWriter = artifactWriter;
    private readonly IPermissionRepository _permissionRepository = permissionRepository;
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
        var (context, contextError) = await BuildContextAsync(table, columns, cancellationToken);
        if (context is null)
        {
            return GenerationResult.Fail(contextError ?? "构建生成上下文失败。");
        }

        // 无显式模板编码时，按表的模板类型（单表/树表/主子表）选取通用模板集；
        // 模板不按业务模块过滤（CRUD 模板对所有模块通用，此前误用 ModuleName 作分组导致匹配为空）
        var templates = request.TemplateCodes is { Count: > 0 }
            ? await _templateRepository.GetByCodesAsync(request.TemplateCodes, cancellationToken)
            : await _templateRepository.GetEnabledByTypeAsync(table.TemplateType, cancellationToken);

        // 生成范围裁剪：按模板分组前缀（backend-* / frontend-*）过滤
        templates = FilterByScope(templates, table.GenerationScope);

        if (templates.Count == 0)
        {
            return GenerationResult.Fail("未找到可用模板（请检查模板类型/编码、启用状态与生成范围）。");
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

        // 二阶产物：菜单/权限接线代码（待并入源码 → 重建库经既有 Seeder 链生效，非运行时写库）。
        // 属后端接线，仅后端与全部范围生成；纯前端不需要（其后端与菜单权限已在别处到位）。
        if (table.GenerationScope != GenerationScope.FrontendOnly)
        {
            var collidingCodes = await FindCollidingPermissionCodesAsync(context, cancellationToken);
            artifacts.AddRange(MenuPermissionArtifactGenerator.Build(context, collidingCodes));
            artifacts.Add(PermissionSeedArtifactGenerator.Build(context));
            artifacts.Add(PageDescriptorArtifactGenerator.Build(context));
            artifacts.AddRange(SeederArtifactGenerator.Build(context));
        }

        stopwatch.Stop();
        return GenerationResult.Ok(artifacts, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// 按生成范围裁剪模板（依模板分组前缀 backend-* / frontend-* 判定归属）
    /// </summary>
    private static IReadOnlyList<SysCodeGenTemplate> FilterByScope(IReadOnlyList<SysCodeGenTemplate> templates, GenerationScope scope)
    {
        return scope switch
        {
            GenerationScope.BackendOnly => [.. templates.Where(template => IsBackend(template.TemplateGroup))],
            GenerationScope.FrontendOnly => [.. templates.Where(template => IsFrontend(template.TemplateGroup))],
            _ => templates
        };
    }

    /// <summary>
    /// 模板分组是否属后端
    /// </summary>
    private static bool IsBackend(string? templateGroup)
        => templateGroup?.Contains("backend", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// 模板分组是否属前端
    /// </summary>
    private static bool IsFrontend(string? templateGroup)
        => templateGroup?.Contains("frontend", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// 可裁剪的写操作全集（读取基线 list/detail 始终生成，不在此列）
    /// </summary>
    private static readonly string[] CrudActions = ["create", "update", "delete"];

    /// <summary>
    /// 归一化包含操作：null/空（未配置或全选）→ 全开；非空则按规范集合过滤
    /// </summary>
    private static IReadOnlyList<string> NormalizeEnabledActions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return CrudActions;
        }

        var selected = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(action => action.ToLowerInvariant())
            .ToHashSet();
        return [.. CrudActions.Where(selected.Contains)];
    }

    /// <summary>
    /// 查已存在的权限码（生成前的全局唯一性预检；仅用于 README 顶部醒目告警）
    /// </summary>
    /// <remarks>
    /// fail-open：权限库读取异常不应挡住生成（唯一性告警是提示性的），异常时记日志并返回空集。
    /// </remarks>
    private async Task<IReadOnlyCollection<string>> FindCollidingPermissionCodesAsync(CodeGenerationContext context, CancellationToken cancellationToken)
    {
        var candidateCodes = MenuPermissionArtifactShared.EffectiveActions(context)
            .Select(action => $"{context.TableName}:{action}")
            .ToList();
        if (candidateCodes.Count == 0)
        {
            return [];
        }

        try
        {
            var existing = await _permissionRepository.GetByCodesAsync(candidateCodes, cancellationToken);
            return [.. existing.Select(permission => permission.PermissionCode)];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "权限码唯一性预检失败，跳过冲突提示（不影响生成）。");
            return [];
        }
    }

    /// <summary>
    /// 由表配置 + 列配置构建模板上下文
    /// </summary>
    /// <remarks>
    /// 树表/主子表的结构字段在此解析为强类型的列模型与关联表引用；
    /// 解析不出来时返回错误而非静默降级——否则模板会渲染出引用了不存在属性的代码，
    /// 问题要到编译期才暴露。
    /// </remarks>
    private async Task<(CodeGenerationContext? Context, string? Error)> BuildContextAsync(
        SysCodeGenTable table,
        IReadOnlyList<SysCodeGenTableColumn> columns,
        CancellationToken cancellationToken)
    {
        var columnSchemas = columns.Select(column => MapColumn(table, column)).ToList();

        var context = new CodeGenerationContext
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
            EnabledActions = NormalizeEnabledActions(table.EnabledActions),
            Columns = columnSchemas,
            PrimaryKey = columnSchemas.FirstOrDefault(column => column.IsPrimaryKey)
                ?? columnSchemas.FirstOrDefault(column => column.ColumnName == table.PrimaryKeyColumn),
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

        if (table.TemplateType == TemplateType.Tree)
        {
            var error = ResolveTreeColumns(table, columnSchemas, context);
            if (error is not null)
            {
                return (null, error);
            }
        }

        if (table.TemplateType == TemplateType.MasterDetail)
        {
            var (master, error) = await ResolveMasterTableAsync(table, columnSchemas, cancellationToken);
            if (error is not null)
            {
                return (null, error);
            }

            context.MasterTable = master;
        }

        // 反查以本表为主表的子表（本表为主表时生成明细区）；与本表自身的模板类型无关
        context.DetailTables = await ResolveDetailTablesAsync(table, cancellationToken);

        return (context, null);
    }

    /// <summary>
    /// 解析树表的父级列与显示名列（fail-closed）
    /// </summary>
    private static string? ResolveTreeColumns(SysCodeGenTable table, List<ColumnSchema> columnSchemas, CodeGenerationContext context)
    {
        if (string.IsNullOrWhiteSpace(table.TreeParentColumn))
        {
            return $"表 {table.TableName} 的模板类型为树表，但未配置父级列（TreeParentColumn）。";
        }

        var parent = columnSchemas.FirstOrDefault(column =>
            string.Equals(column.ColumnName, table.TreeParentColumn, StringComparison.OrdinalIgnoreCase));
        if (parent is null)
        {
            return $"表 {table.TableName} 配置的父级列 {table.TreeParentColumn} 不在列配置中，请重新导入或同步表结构。";
        }

        if (string.IsNullOrWhiteSpace(table.TreeNameColumn))
        {
            return $"表 {table.TableName} 的模板类型为树表，但未配置显示名列（TreeNameColumn）。";
        }

        var name = columnSchemas.FirstOrDefault(column =>
            string.Equals(column.ColumnName, table.TreeNameColumn, StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            return $"表 {table.TableName} 配置的显示名列 {table.TreeNameColumn} 不在列配置中，请重新导入或同步表结构。";
        }

        context.TreeParentColumn = parent;
        context.TreeNameColumn = name;
        return null;
    }

    /// <summary>
    /// 解析本表所属的主表（fail-closed）
    /// </summary>
    private async Task<(RelatedTableRef? Master, string? Error)> ResolveMasterTableAsync(
        SysCodeGenTable table,
        List<ColumnSchema> columnSchemas,
        CancellationToken cancellationToken)
    {
        if (table.MasterTableId is not { } masterTableId)
        {
            return (null, $"表 {table.TableName} 的模板类型为主子表，但未配置主表（MasterTableId）。");
        }

        if (string.IsNullOrWhiteSpace(table.MasterForeignKey))
        {
            return (null, $"表 {table.TableName} 的模板类型为主子表，但未配置指向主表的外键列（MasterForeignKey）。");
        }

        var foreignKey = columnSchemas.FirstOrDefault(column =>
            string.Equals(column.ColumnName, table.MasterForeignKey, StringComparison.OrdinalIgnoreCase));
        if (foreignKey is null)
        {
            return (null, $"表 {table.TableName} 配置的外键列 {table.MasterForeignKey} 不在列配置中，请重新导入或同步表结构。");
        }

        var masterTable = await _tableRepository.GetByIdAsync(masterTableId, cancellationToken);
        if (masterTable is null)
        {
            return (null, $"表 {table.TableName} 配置的主表（Id={masterTableId}）不存在，请重新选择主表。");
        }

        var masterColumns = await _columnRepository.GetByTableIdAsync(masterTable.BasicId, cancellationToken);
        return (BuildRelatedTableRef(masterTable, masterColumns, foreignKey), null);
    }

    /// <summary>
    /// 反查以本表为主表的子表集合（无匹配时返回空集合，不是错误）
    /// </summary>
    private async Task<IReadOnlyList<RelatedTableRef>> ResolveDetailTablesAsync(SysCodeGenTable table, CancellationToken cancellationToken)
    {
        var detailTables = await _tableRepository.GetByMasterTableIdAsync(table.BasicId, cancellationToken);
        if (detailTables.Count == 0)
        {
            return [];
        }

        var refs = new List<RelatedTableRef>(detailTables.Count);
        foreach (var detail in detailTables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var detailColumns = await _columnRepository.GetByTableIdAsync(detail.BasicId, cancellationToken);
            var foreignKey = detailColumns
                .Where(column => string.Equals(column.ColumnName, detail.MasterForeignKey, StringComparison.OrdinalIgnoreCase))
                .Select(column => MapColumn(detail, column))
                .FirstOrDefault();

            // 子表未配好外键列则跳过：主表侧的明细区无从取数，但不应因此让主表整体生成失败
            if (foreignKey is null)
            {
                _logger.LogWarning(
                    "子表 {DetailTable} 的外键列 {ForeignKey} 不在列配置中，已跳过其明细区生成。",
                    detail.TableName, detail.MasterForeignKey);
                continue;
            }

            refs.Add(BuildRelatedTableRef(detail, detailColumns, foreignKey));
        }

        return refs;
    }

    /// <summary>
    /// 表配置 + 列配置 → 关联表引用
    /// </summary>
    private RelatedTableRef BuildRelatedTableRef(SysCodeGenTable table, IReadOnlyList<SysCodeGenTableColumn> columns, ColumnSchema foreignKey) => new()
    {
        TableId = table.BasicId,
        TableName = table.TableName,
        TableComment = table.TableComment,
        ClassName = table.ClassName,
        ClassNameCamel = NamingConventions.Camelize(table.ClassName),
        ClassNameKebab = NamingConventions.Kebabize(table.ClassName),
        ModuleName = table.ModuleName,
        Namespace = table.Namespace,
        ForeignKeyColumn = foreignKey.ColumnName,
        ForeignKeyProperty = foreignKey.CSharpProperty,
        Columns = [.. columns.Select(column => MapColumn(table, column))]
    };

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

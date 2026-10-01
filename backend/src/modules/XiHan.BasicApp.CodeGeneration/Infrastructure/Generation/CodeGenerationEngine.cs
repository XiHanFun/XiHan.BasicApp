// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Repositories;
using MenuType = XiHan.BasicApp.Saas.Domain.Entities.MenuType;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 代码生成引擎（管线编排：建模 → 选模板 → 渲染 → 产出）
/// </summary>
/// <remarks>
/// 本类已接通"配置 → 渲染 → 产物"主链路，并按模板声明的 <see cref="ArtifactWriteMode"/>
/// 区分自动产物（总是覆盖）与手动产物（仅首次创建），保证重新生成不冲掉手写代码。
/// 待完善：树表/主子表上下文扩展（见 M1-1）。
/// </remarks>
public sealed partial class CodeGenerationEngine(
    ICodeGenTableRepository tableRepository,
    ICodeGenTableColumnRepository columnRepository,
    ICodeGenTemplateRepository templateRepository,
    ITemplateRendererResolver rendererResolver,
    ITypeMappingProvider typeMappingProvider,
    IEnumTypeCatalog enumTypeCatalog,
    IEntityMetadataCatalog entityCatalog,
    IGeneratedArtifactPackager packager,
    IGeneratedArtifactWriter artifactWriter,
    IPermissionRepository permissionRepository,
    IMenuRepository menuRepository,
    ILogger<CodeGenerationEngine> logger) : ICodeGenerationEngine
{
    private readonly ICodeGenTableRepository _tableRepository = tableRepository;
    private readonly ICodeGenTableColumnRepository _columnRepository = columnRepository;
    private readonly ICodeGenTemplateRepository _templateRepository = templateRepository;
    private readonly ITemplateRendererResolver _rendererResolver = rendererResolver;
    private readonly ITypeMappingProvider _typeMappingProvider = typeMappingProvider;
    private readonly IEnumTypeCatalog _enumTypeCatalog = enumTypeCatalog;
    private readonly IEntityMetadataCatalog _entityCatalog = entityCatalog;
    private readonly IGeneratedArtifactPackager _packager = packager;
    private readonly IGeneratedArtifactWriter _artifactWriter = artifactWriter;
    private readonly IPermissionRepository _permissionRepository = permissionRepository;
    private readonly IMenuRepository _menuRepository = menuRepository;
    private readonly ILogger<CodeGenerationEngine> _logger = logger;

    /// <summary>
    /// 预览生成（仅返回产物内容，不打包、不落盘）
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成结果（含产物清单）</returns>
    public Task<GenerationResult> PreviewAsync(GenerationRequest request, CancellationToken cancellationToken = default)
        => RenderCoreAsync(request, cancellationToken);

    /// <summary>
    /// 执行生成（按 GenType 分流：Zip 打包 / 落盘）
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成结果（Zip 时含 Package 字节流）</returns>
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

            case GenType.Project:
                // 生成到项目：后端写进与命名空间同名的模块项目，前端写进前端工程（位置由配置推导，默认关闭），审计经生成历史留痕
                var table = await _tableRepository.GetByIdAsync(request.TableId, cancellationToken);
                var writeResult = await _artifactWriter.WriteToProjectAsync(result.Artifacts, table?.Namespace, cancellationToken);
                if (!writeResult.Success)
                {
                    return GenerationResult.Fail(writeResult.Message ?? "生成到项目失败。");
                }

                result.WrittenCount = writeResult.WrittenCount;
                result.SkippedPaths = writeResult.SkippedPaths;
                result.TargetRoots = writeResult.TargetRoots;

                _logger.LogInformation(
                    "代码生成到项目完成：TableId={TableId}，位置={Roots}，写入={Written}，跳过={Skipped}（手动文件已存在）",
                    request.TableId, string.Join("；", writeResult.TargetRoots), writeResult.WrittenCount, writeResult.SkippedCount);
                break;

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

        // 表由手写实体建出：沿用那个实体，不再生成实体（否则与它重复定义）
        if (context.ExistingEntityNamespace is not null)
        {
            templates = [.. templates.Where(template => template.TemplateCode is not (EntityTemplateCode or EntityManualTemplateCode))];
        }

        if (templates.Count == 0)
        {
            return GenerationResult.Fail("未找到可用模板（请检查模板类型/编码、启用状态与生成范围）。");
        }

        var artifacts = new List<GeneratedArtifact>(templates.Count);
        foreach (var template in templates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var renderer = _rendererResolver.Resolve(template.TemplateEngine);

            string content;
            string fileName;
            string relativePath;
            try
            {
                content = await renderer.RenderAsync(template.TemplateContent ?? string.Empty, context, cancellationToken);
                fileName = await ResolveFileNameAsync(renderer, template, context, cancellationToken);
                relativePath = await ResolveRelativePathAsync(renderer, template, context, fileName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 冒泡会绕过调用方的历史留痕，且异常里不带模板身份，用户只看到一句渲染失败
                _logger.LogError(ex, "模板 {TemplateCode} 渲染失败（表 {Table}）", template.TemplateCode, table.TableName);
                return GenerationResult.Fail($"模板 {template.TemplateCode}（{template.TemplateName}）渲染失败：{ex.Message}");
            }

            artifacts.Add(new GeneratedArtifact(relativePath, fileName, content, template.TemplateCode, template.WriteMode, SideOf(template.TemplateGroup)));
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
    /// 模板产物的归属：生成到项目时据此决定写进后端项目还是前端工程
    /// </summary>
    private static ArtifactSide? SideOf(string? templateGroup)
        => IsBackend(templateGroup) ? ArtifactSide.Backend : IsFrontend(templateGroup) ? ArtifactSide.Frontend : null;

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
            EnabledActions = CodeGenActions.Normalize(table.EnabledActions),
            Columns = columnSchemas,
            PrimaryKey = columnSchemas.FirstOrDefault(column => column.IsPrimaryKey)
                ?? columnSchemas.FirstOrDefault(column => column.ColumnName == table.PrimaryKeyColumn),
            Options = new Dictionary<string, object?>
            {
                ["PrimaryKeyColumn"] = table.PrimaryKeyColumn,
                ["TreeParentColumn"] = table.TreeParentColumn,
                ["TreeNameColumn"] = table.TreeNameColumn,
                ["MasterTableId"] = table.MasterTableId?.ToString(),
                ["MasterForeignKey"] = table.MasterForeignKey
            }
        };

        // 表已有手写实体（实体建表的常规路径）：沿用它，类名须一致，生成的代码按它的命名空间引用
        var existingEntity = FindHandWrittenEntity(table.TableName);
        if (existingEntity is not null)
        {
            if (!string.Equals(existingEntity.Name, table.ClassName, StringComparison.Ordinal))
            {
                return (null, $"表 {table.TableName} 已有实体 {existingEntity.FullName}，表配置的类名却是 {table.ClassName}：生成的代码沿用这个实体，请把类名改成 {existingEntity.Name}。");
            }

            context.ExistingEntityNamespace = existingEntity.Namespace;
        }

        var parentMenuError = await ResolveParentMenuAsync(table, context, cancellationToken);
        if (parentMenuError is not null)
        {
            return (null, parentMenuError);
        }

        // 页面码是表级推导，在这里一次校验：模块名是自由输入，填中文或带空格照样能两端一致地产出，
        // 但前端权限码卫生门禁的码形正则匹配不上，整页按钮码会被静默跳过检查。
        // 放在渲染器里校验会漏掉空模板早退，也会把与页面码无关的纯后端生成一并挡住。
        var pageCode = MenuPermissionArtifactShared.PageCode(context);
        if (!PageCodeRegex().IsMatch(pageCode))
        {
            return (null, $"表 {table.TableName} 推导出的页面码 {pageCode} 不合规：模块名须为 [a-z][a-z0-9_-]*，请在表配置里改成英文模块名。");
        }

        // 存量配置可能早于保存侧校验写入，生成前再判一次，不产出调不通的导入按钮
        var actionConflict = CodeGenActions.FindConflict(context.EnabledActions);
        if (actionConflict is not null)
        {
            return (null, $"表 {table.TableName} 的{actionConflict}");
        }

        var dictError = ValidateDictSelectors(table, columnSchemas);
        if (dictError is not null)
        {
            return (null, dictError);
        }

        var relationError = await ResolveRelationsAsync(table, columnSchemas, cancellationToken);
        if (relationError is not null)
        {
            return (null, relationError);
        }

        var uniqueError = ValidateUniqueColumns(table, columnSchemas);
        if (uniqueError is not null)
        {
            return (null, uniqueError);
        }

        if (context.EnabledActions.Contains(CodeGenActions.Status))
        {
            var statusError = ResolveStatusColumn(table, columnSchemas, context);
            if (statusError is not null)
            {
                return (null, statusError);
            }
        }

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
    /// 页面码合规判据（与前端权限码卫生门禁的码形正则同源）
    /// </summary>
    [GeneratedRegex(@"^[a-z][a-z0-9_-]*(?:\.[a-z0-9_-]+)+$")]
    private static partial Regex PageCodeRegex();

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

        // 显示名列承载展开箭头，且树节点 DTO 由列表列推导：不勾「列表显示」会让树既没有展开列、
        // 前端父级选择器也取不到该属性
        if (!name.IsList)
        {
            return $"表 {table.TableName} 的显示名列 {table.TreeNameColumn} 未勾选「列表显示」，树表无法渲染展开列，请在列配置中勾选。";
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
            IsUnique = column.IsUnique,
            IsList = column.IsList,
            IsInsert = column.IsInsert,
            IsEdit = column.IsEdit,
            IsQuery = column.IsQuery,
            Length = column.ColumnLength,
            DecimalDigits = column.DecimalDigits,
            HtmlType = column.HtmlType,
            QueryType = column.QueryType,
            DictSelectorType = column.DictSelectorType,
            DictCode = column.DictCode,
            EnumTypeName = column.EnumTypeName,
            ConstValues = column.ConstValues,
            RelationTableId = column.RelationTableId,
            RelationLabelColumn = column.RelationLabelColumn
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

        ResolveEnumFacts(table, column, schema);
        return schema;
    }

    /// <summary>
    /// 解析枚举列事实（短名/命名空间/首成员）
    /// </summary>
    /// <remarks>
    /// 判据是「持久化的 C# 类型 == 解析出的枚举短名」：旧表配置里该字段仍是 int，
    /// 于是契约变更只发生在用户对表主动执行重新同步之后，已上线的表不会被动改。
    /// 解析不到只告警不阻断，产物退化为无选项来源的下拉。
    /// </remarks>
    private void ResolveEnumFacts(SysCodeGenTable table, SysCodeGenTableColumn column, ColumnSchema schema)
    {
        if (schema.DictSelectorType != DictSelectorType.EnumSelector)
        {
            return;
        }

        var declaredType = schema.CSharpType.TrimEnd('?');
        if (_enumTypeCatalog.TryResolve(schema.EnumTypeName, out var facts)
            && string.Equals(declaredType, facts.ShortName, StringComparison.Ordinal))
        {
            schema.EnumTypeShortName = facts.ShortName;
            schema.EnumNamespace = facts.Namespace;
            schema.EnumDefaultMember = facts.DefaultMemberName;
            return;
        }

        _logger.LogWarning(
            "表 {Table} 的列 {Column} 配置为枚举选择器，但类型 {EnumType} 未解析、或 C# 类型仍为 {CSharpType}；本次生成的下拉不接选项来源，请对该表执行重新同步。",
            table.TableName, column.ColumnName, schema.EnumTypeName, schema.CSharpType);
    }

    /// <summary>
    /// 校验字典选择器列
    /// </summary>
    /// <remarks>
    /// 字典下拉按字典编码取选项，选中值是字典项编码（文本）。没填字典编码会产出一个取不到选项的下拉，
    /// 非文本列（含 long 标识）装不下字典项编码、提交必被后端拒，两者都在生成期挡下。
    /// 基类托管列与主键不进任何产物，不校验。
    /// </remarks>
    private static string? ValidateDictSelectors(SysCodeGenTable table, IReadOnlyList<ColumnSchema> columns)
    {
        foreach (var column in columns)
        {
            if (column.DictSelectorType != DictSelectorType.DictSelector
                || column.IsPrimaryKey
                || GeneratedColumnNames.IsBaseColumn(column.ColumnName))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(column.DictCode))
            {
                return $"表 {table.TableName} 的列 {column.ColumnName} 选了字典选择器但没填字典编码，请在列配置里选择字典。";
            }

            if (column.CSharpType.TrimEnd('?') != "string")
            {
                return $"表 {table.TableName} 的列 {column.ColumnName} 选了字典选择器，但 C# 类型是 {column.CSharpType}：字典项按编码（文本）存储，列须为 string。";
            }
        }

        return null;
    }

    /// <summary>
    /// 实体模板编码（自动文件 / 手动文件）：表已有手写实体时不生成
    /// </summary>
    private const string EntityTemplateCode = "backend.entity";

    private const string EntityManualTemplateCode = "backend.entity.manual";

    /// <summary>
    /// 生成器给生成的实体打的工具名（<see cref="GeneratedCodeAttribute"/>），据此区分生成的与手写的
    /// </summary>
    private const string GeneratedCodeTool = "XiHan.CodeGen";

    /// <summary>
    /// 解析父菜单（fail-closed）：表配置存菜单主键，生成的菜单登记按菜单码挂靠
    /// </summary>
    /// <remarks>
    /// 只能挂在平台目录下：页面挂在页面或按钮下没有意义，租户菜单不进平台种子。目录须有菜单码，菜单登记靠它找父级。
    /// 选的若是在菜单管理里手建的目录，它只存在于当前库；新建的库里没有它，汇总菜单种子会因找不到父菜单报错。
    /// </remarks>
    private async Task<string?> ResolveParentMenuAsync(SysCodeGenTable table, CodeGenerationContext context, CancellationToken cancellationToken)
    {
        if (table.ParentMenuId is not { } parentMenuId)
        {
            return null;
        }

        var parent = await _menuRepository.GetByIdAsync(parentMenuId, cancellationToken);
        if (parent is null || parent.TenantId != 0)
        {
            return $"表 {table.TableName} 配置的父菜单（{parentMenuId}）不存在或不是平台菜单：请在表配置里重新选择父菜单。";
        }

        if (parent.MenuType != MenuType.Directory)
        {
            return $"表 {table.TableName} 配置的父菜单「{parent.MenuName}」不是目录：页面只能挂在目录下，请重新选择。";
        }

        if (string.IsNullOrWhiteSpace(parent.MenuCode))
        {
            return $"表 {table.TableName} 配置的父菜单「{parent.MenuName}」没有菜单码：菜单登记按菜单码找父级，请先在菜单管理里给它填菜单码。";
        }

        context.ParentMenuCode = parent.MenuCode;
        return null;
    }

    /// <summary>
    /// 找表对应的手写实体
    /// </summary>
    /// <remarks>
    /// 本仓库的表一般由实体自动建出，导入后实体已在代码里，生成时沿用它。
    /// 生成器产出的实体带 <see cref="GeneratedCodeAttribute"/>（工具名 XiHan.CodeGen），那是生成器自己的，照常重新生成；
    /// 外部库的表没有实体，同样照常生成。
    /// </remarks>
    private Type? FindHandWrittenEntity(string tableName)
    {
        if (!_entityCatalog.TryGetEntityType(tableName, out var entityType))
        {
            return null;
        }

        var generated = entityType.GetCustomAttributes<GeneratedCodeAttribute>(inherit: false)
            .Any(attribute => attribute.Tool == GeneratedCodeTool);
        return generated ? null : entityType;
    }

    /// <summary>
    /// 状态列的枚举短名（平台统一的启用/停用枚举）
    /// </summary>
    private const string StatusEnumName = "EnableStatus";

    /// <summary>
    /// 解析状态切换用的状态列（fail-closed）
    /// </summary>
    /// <remarks>
    /// 取表里已解析成 EnableStatus 枚举的业务列：有名为 Status 的就用它，否则须恰好一列。
    /// 找不到或有多列又没有名为 Status 的，都不猜，直接报错。
    /// </remarks>
    private static string? ResolveStatusColumn(SysCodeGenTable table, IReadOnlyList<ColumnSchema> columnSchemas, CodeGenerationContext context)
    {
        var candidates = columnSchemas
            .Where(column => column.EnumTypeShortName == StatusEnumName
                && !column.IsPrimaryKey
                && !GeneratedColumnNames.IsBaseColumn(column.ColumnName))
            .ToList();
        var status = candidates.FirstOrDefault(column => column.CSharpProperty == "Status")
            ?? (candidates.Count == 1 ? candidates[0] : null);
        if (status is not null)
        {
            // 行内启停按列表行上的状态值决定是启用还是停用，状态列必须进列表
            if (!status.IsList)
            {
                return $"表 {table.TableName} 的状态列 {status.ColumnName} 没有勾选「列表」：行内启用/停用要按列表里的状态值切换，请在列配置里勾选。";
            }

            context.StatusColumn = status;
            return null;
        }

        return candidates.Count == 0
            ? $"表 {table.TableName} 勾选了状态切换，但没有 {StatusEnumName} 类型的状态列：请把状态列的选项来源设为枚举 {StatusEnumName} 并重新同步表结构。"
            : $"表 {table.TableName} 有多个 {StatusEnumName} 列（{string.Join("、", candidates.Select(column => column.ColumnName))}），无法确定状态切换用哪一列：把状态列的属性名定为 Status。";
    }

    /// <summary>
    /// 校验唯一列：二进制与布尔列做不了唯一校验
    /// </summary>
    private static string? ValidateUniqueColumns(SysCodeGenTable table, IReadOnlyList<ColumnSchema> columnSchemas)
    {
        foreach (var column in columnSchemas)
        {
            if (!column.IsUnique || column.IsPrimaryKey || GeneratedColumnNames.IsBaseColumn(column.ColumnName))
            {
                continue;
            }

            var type = column.CSharpType.TrimEnd('?');
            if (CSharpTypeFacts.IsBinary(column.CSharpType) || type is "bool" or "Boolean")
            {
                return $"表 {table.TableName} 的列 {column.ColumnName} 勾了唯一，但 {column.CSharpType} 类型做不了唯一校验，请取消。";
            }
        }

        return null;
    }

    /// <summary>
    /// 解析关联选择器列的目标（fail-closed）
    /// </summary>
    /// <remarks>
    /// 本列存被关联记录的主键，须为 long；显示列须为目标表的文本列；关联树要求目标是配好父级列的树表，
    /// 显示列缺省取其名称列。任何一项对不上都在生成期挡下，不产出编译不过或下拉取不到数的代码。
    /// 目标可以是本表（自关联，如「上级负责人」指向同一张人员表）。
    /// </remarks>
    private async Task<string?> ResolveRelationsAsync(
        SysCodeGenTable table,
        IReadOnlyList<ColumnSchema> columnSchemas,
        CancellationToken cancellationToken)
    {
        foreach (var column in columnSchemas)
        {
            if (column.DictSelectorType is not (DictSelectorType.TableSelector or DictSelectorType.TreeSelector)
                || column.IsPrimaryKey
                || GeneratedColumnNames.IsBaseColumn(column.ColumnName))
            {
                continue;
            }

            var where = $"表 {table.TableName} 的列 {column.ColumnName}";
            if (column.CSharpType.TrimEnd('?') != "long")
            {
                return $"{where} 选了关联选择器，但 C# 类型是 {column.CSharpType}：关联按被关联记录的主键存储，列须为 long。";
            }

            if (column.RelationTableId is not > 0)
            {
                return $"{where} 选了关联选择器但没选关联的表，请在列配置里选择。";
            }

            var target = column.RelationTableId == table.BasicId
                ? table
                : await _tableRepository.GetByIdAsync(column.RelationTableId.Value, cancellationToken);
            if (target is null)
            {
                return $"{where} 关联的表配置（{column.RelationTableId}）不存在，请在列配置里重新选择。";
            }

            var targetColumns = target.BasicId == table.BasicId
                ? columnSchemas
                : [.. (await _columnRepository.GetByTableIdAsync(target.BasicId, cancellationToken)).Select(targetColumn => MapColumn(target, targetColumn))];

            var isTree = column.DictSelectorType == DictSelectorType.TreeSelector;
            string? parentProperty = null;
            if (isTree)
            {
                if (target.TemplateType != TemplateType.Tree || string.IsNullOrWhiteSpace(target.TreeParentColumn))
                {
                    return $"{where} 选了关联树，但关联的表 {target.TableName} 不是树表：它的表配置须为树表模板并选好父级列。";
                }

                var parent = FindColumn(targetColumns, target.TreeParentColumn);
                if (parent is null || parent.CSharpType.TrimEnd('?') != "long")
                {
                    return $"{where} 关联的树表 {target.TableName} 的父级列 {target.TreeParentColumn} 不在列配置中或不是 long 列。";
                }

                parentProperty = parent.CSharpProperty;
            }

            var labelColumn = string.IsNullOrWhiteSpace(column.RelationLabelColumn)
                ? isTree ? target.TreeNameColumn : null
                : column.RelationLabelColumn;
            if (string.IsNullOrWhiteSpace(labelColumn))
            {
                return $"{where} 选了关联表但没选显示列，请在列配置里选择。";
            }

            var label = FindColumn(targetColumns, labelColumn);
            if (label is null)
            {
                return $"{where} 的显示列 {labelColumn} 不在关联的表 {target.TableName} 的列配置中。";
            }

            if (label.CSharpType.TrimEnd('?') != "string")
            {
                return $"{where} 的显示列 {labelColumn} 是 {label.CSharpType}：下拉按文本显示，显示列须为 string。";
            }

            var targetNamespace = MenuPermissionArtifactShared.ResolveNamespace(new CodeGenerationContext
            {
                ClassName = target.ClassName,
                Namespace = target.Namespace,
                ModuleName = target.ModuleName
            });
            column.Relation = new RelationTarget
            {
                TableId = target.BasicId,
                TableName = target.TableName,
                TableComment = target.TableComment,
                ClassName = target.ClassName,
                // 目标表已有实体时用它的真实类型（实体建表的常规路径），否则按目标表配置推导生成后的位置
                EntityTypeQualified = _entityCatalog.TryGetEntityType(target.TableName, out var targetEntity)
                    ? targetEntity.FullName!
                    : $"{targetNamespace}.Domain.Entities.{target.ClassName}",
                LabelProperty = label.CSharpProperty,
                ParentProperty = parentProperty,
                IsTree = isTree
            };
        }

        return null;

        static ColumnSchema? FindColumn(IEnumerable<ColumnSchema> candidates, string? columnName)
            => candidates.FirstOrDefault(candidate => string.Equals(candidate.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
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

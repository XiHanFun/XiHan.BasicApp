// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using XiHan.BasicApp.CodeGeneration.Application.Contracts;
using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.CodeGeneration.Application.AppServices;

/// <summary>
/// 代码生成编排应用服务（导入 → 预览 → 生成 → 下载）
/// </summary>
/// <remarks>
/// 引擎面对外入口：负责权限/事务/DTO 转换/历史留痕。
/// 导入闭环：扫描库表 → 构建表/列配置 → 落库 → 返回详情；
/// 生成闭环：调用引擎后无论成败均写入 <see cref="SysCodeGenHistory"/> 留痕。
/// </remarks>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "代码生成")]
public sealed class CodeGenerationAppService(
    ICodeGenerationEngine generationEngine,
    IDatabaseSchemaImporter schemaImporter,
    ITableConfigInferrer inferrer,
    ICodeGenTableRepository tableRepository,
    ICodeGenTableColumnRepository tableColumnRepository,
    ICodeGenHistoryRepository historyRepository,
    ICodeGenTableQueryService tableQueryService,
    ICurrentUser currentUser) : CodeGenerationApplicationService, ICodeGenerationAppService
{
    private readonly ICodeGenerationEngine _generationEngine = generationEngine;
    private readonly IDatabaseSchemaImporter _schemaImporter = schemaImporter;
    private readonly ITableConfigInferrer _inferrer = inferrer;
    private readonly ICodeGenTableRepository _tableRepository = tableRepository;
    private readonly ICodeGenTableColumnRepository _tableColumnRepository = tableColumnRepository;
    private readonly ICodeGenHistoryRepository _historyRepository = historyRepository;
    private readonly ICodeGenTableQueryService _tableQueryService = tableQueryService;
    private readonly ICurrentUser _currentUser = currentUser;

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<IReadOnlyList<string>> ListDatabaseTablesAsync(CodeGenDbTableQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var tables = await _schemaImporter.ListTablesAsync(input.DataSourceId?.ToString(), cancellationToken);
        if (string.IsNullOrWhiteSpace(input.Keyword))
        {
            return tables;
        }

        var keyword = input.Keyword.Trim();
        return [.. tables.Where(table => table.Contains(keyword, StringComparison.OrdinalIgnoreCase))];
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Import)]
    public async Task<CodeGenTableDetailDto> ImportTableAsync(CodeGenImportTableDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var tableName = input.TableName?.Trim();
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new UserFriendlyException(new ResourceLocalizableString("Errors", "CodeGeneration.TableNameRequired"), "数据库表名不能为空。");
        }

        // 1) 去重：同一目标表禁止重复配置
        if (await _tableRepository.ExistsTableNameAsync(tableName, null, cancellationToken))
        {
            throw new UserFriendlyException(new ResourceLocalizableString("Errors", "CodeGeneration.TableAlreadyConfigured", tableName), $"数据库表“{tableName}”已配置，请勿重复导入。");
        }

        var tableId = await BuildTableAndColumnsAsync(tableName, input.DataSourceId, input.DatabaseType, input, cancellationToken);

        // 返回导入后的详情
        return await _tableQueryService.GetDetailAsync(tableId, cancellationToken)
            ?? throw new InvalidOperationException("导入成功但读取表配置详情失败。");
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Import)]
    public async Task<CodeGenImportTablesResultDto> ImportTablesAsync(CodeGenImportTablesDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 不包整体事务（不标 UnitOfWork）：逐表隔离，一张失败不回滚其余已成功的表
        var succeeded = new List<string>();
        var failed = new List<CodeGenImportFailureDto>();

        foreach (var rawName in input.TableNames ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tableName = rawName?.Trim();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                continue;
            }

            try
            {
                if (await _tableRepository.ExistsTableNameAsync(tableName, null, cancellationToken))
                {
                    failed.Add(new CodeGenImportFailureDto { TableName = tableName, Reason = "已配置，跳过" });
                    continue;
                }

                await BuildTableAndColumnsAsync(tableName, input.DataSourceId, input.DatabaseType, null, cancellationToken);
                succeeded.Add(tableName);
            }
            catch (Exception ex)
            {
                failed.Add(new CodeGenImportFailureDto { TableName = tableName, Reason = ex.Message });
            }
        }

        return new CodeGenImportTablesResultDto { Succeeded = succeeded, Failed = failed };
    }

    /// <inheritdoc />
    [HttpPost]
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Import)]
    public async Task<CodeGenSchemaSyncResultDto> SyncSchemaAsync(long tableId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var table = await _tableRepository.GetByIdAsync(tableId, cancellationToken)
            ?? throw new UserFriendlyException(new ResourceLocalizableString("Errors", "CodeGeneration.TableConfigNotFound", tableId), $"表配置不存在：{tableId}");

        // 1) 按来源数据源重新扫描 + 重新推断
        var schema = await _schemaImporter.ImportTableAsync(table.TableName, table.DataSourceId?.ToString(), cancellationToken)
            ?? throw new InvalidOperationException($"数据库表“{table.TableName}”不存在或无法读取结构。");
        var suggestion = _inferrer.Infer(schema, new InferenceContext(_currentUser.UserName, table.DatabaseType));

        // 2) 表级：未冻结的字段跟随重新推断（TableComment 始终以库为准）
        table.TableComment = schema.TableComment;
        table.PrimaryKeyColumn = suggestion.PrimaryKeyColumn;
        SyncTableField(table, nameof(SysCodeGenTable.ClassName), suggestion.ClassName, value => table.ClassName = value!);
        SyncTableField(table, nameof(SysCodeGenTable.Namespace), suggestion.Namespace, value => table.Namespace = value);
        SyncTableField(table, nameof(SysCodeGenTable.ModuleName), suggestion.ModuleName, value => table.ModuleName = value);
        SyncTableField(table, nameof(SysCodeGenTable.BusinessName), suggestion.BusinessName, value => table.BusinessName = value);
        SyncTableField(table, nameof(SysCodeGenTable.FunctionName), suggestion.FunctionName, value => table.FunctionName = value);
        await _tableRepository.UpdateAsync(table, cancellationToken);

        // 3) 列级三路合并
        var existing = await _tableColumnRepository.GetByTableIdAsync(tableId, cancellationToken);
        var existingByName = existing.ToDictionary(column => column.ColumnName, StringComparer.OrdinalIgnoreCase);
        var latestNames = new HashSet<string>(suggestion.Columns.Select(column => column.ColumnName), StringComparer.OrdinalIgnoreCase);

        var addedColumns = new List<SysCodeGenTableColumn>();
        var addedNames = new List<string>();
        var updatedCount = 0;

        foreach (var inferred in suggestion.Columns)
        {
            if (existingByName.TryGetValue(inferred.ColumnName, out var current))
            {
                if (MergeColumn(current, inferred))
                {
                    await _tableColumnRepository.UpdateAsync(current, cancellationToken);
                    updatedCount++;
                }
            }
            else
            {
                addedColumns.Add(MapInferredColumn(tableId, inferred));
                addedNames.Add(inferred.ColumnName);
            }
        }

        if (addedColumns.Count > 0)
        {
            await _tableColumnRepository.AddRangeAsync(addedColumns, cancellationToken);
        }

        // 库中已删除的列 → 物理删除该列配置
        var removedNames = new List<string>();
        foreach (var current in existing.Where(column => !latestNames.Contains(column.ColumnName)))
        {
            await _tableColumnRepository.DeleteByIdAsync(current.BasicId, cancellationToken);
            removedNames.Add(current.ColumnName);
        }

        return new CodeGenSchemaSyncResultDto
        {
            AddedCount = addedNames.Count,
            UpdatedCount = updatedCount,
            RemovedCount = removedNames.Count,
            AddedColumns = addedNames,
            RemovedColumns = removedNames
        };
    }

    /// <summary>
    /// 建表 + 建列并落库（单表导入与批量导入共用；不含去重与事务，事务由调用方决定）
    /// </summary>
    private async Task<long> BuildTableAndColumnsAsync(
        string tableName,
        long? dataSourceId,
        DatabaseType databaseType,
        CodeGenImportTableDto? overrides,
        CancellationToken cancellationToken)
    {
        var schema = await _schemaImporter.ImportTableAsync(tableName, dataSourceId?.ToString(), cancellationToken)
            ?? throw new InvalidOperationException($"数据库表“{tableName}”不存在或无法读取结构。");

        // 推断配置：能推断的一律不问；用户在导入弹窗显式填的字段覆盖推断值
        var suggestion = _inferrer.Infer(schema, new InferenceContext(_currentUser.UserName, databaseType));

        var table = new SysCodeGenTable
        {
            TableName = tableName,
            TableComment = schema.TableComment,
            ClassName = string.IsNullOrWhiteSpace(overrides?.ClassName) ? suggestion.ClassName : overrides.ClassName.Trim(),
            Namespace = FirstNonBlank(overrides?.Namespace, suggestion.Namespace),
            ModuleName = FirstNonBlank(overrides?.ModuleName, suggestion.ModuleName),
            BusinessName = FirstNonBlank(overrides?.BusinessName, suggestion.BusinessName),
            FunctionName = FirstNonBlank(overrides?.FunctionName, suggestion.FunctionName),
            Author = FirstNonBlank(overrides?.Author, suggestion.Author),
            DatabaseType = databaseType,
            // 记住来源数据源：同步表结构与重新生成据此定位来源库，缺了会跑到主库上
            DataSourceId = dataSourceId,
            PrimaryKeyColumn = suggestion.PrimaryKeyColumn,
            TemplateType = suggestion.TemplateType,
            TreeParentColumn = suggestion.TreeParentColumn,
            TreeNameColumn = suggestion.TreeNameColumn,
            GenStatus = GenStatus.NotGenerated,
            GenType = GenType.Zip,
            Status = EnableStatus.Enabled
        };

        var tableId = await _tableRepository.AddReturnIdAsync(table, cancellationToken);

        var columns = suggestion.Columns.Select(column => MapInferredColumn(tableId, column)).ToList();
        if (columns.Count > 0)
        {
            await _tableColumnRepository.AddRangeAsync(columns, cancellationToken);
        }

        return tableId;
    }

    /// <summary>
    /// 推断列建议 → 列实体
    /// </summary>
    private static SysCodeGenTableColumn MapInferredColumn(long tableId, ColumnConfigSuggestion column) => new()
    {
        TableId = tableId,
        ColumnName = column.ColumnName,
        ColumnComment = column.ColumnComment,
        ColumnType = column.ColumnType,
        CSharpType = column.CSharpType,
        CSharpProperty = column.CSharpProperty,
        TsType = column.TsType,
        HtmlType = column.HtmlType,
        QueryType = column.QueryType,
        DictSelectorType = column.DictSelectorType,
        EnumTypeName = column.EnumTypeName,
        ColumnLength = column.Length,
        DecimalDigits = column.DecimalDigits,
        IsPrimaryKey = column.IsPrimaryKey,
        IsIdentity = column.IsIdentity,
        IsNullable = column.IsNullable,
        IsRequired = column.IsRequired,
        IsList = column.IsList,
        IsInsert = column.IsInsert,
        IsEdit = column.IsEdit,
        IsQuery = column.IsQuery,
        Sort = column.Sort,
        Status = EnableStatus.Enabled
    };

    /// <summary>
    /// 同步表级字段：未被人工修改的字段跟随推断值覆盖
    /// </summary>
    private static void SyncTableField(SysCodeGenTable table, string fieldName, string? inferredValue, Action<string?> setter)
    {
        if (!UserModifiedFieldSet.Contains(table.UserModifiedFields, fieldName))
        {
            setter(inferredValue);
        }
    }

    /// <summary>
    /// 合并已存在列：结构性字段始终以库为准；推断字段仅在未被人工修改时覆盖。返回是否有变化
    /// </summary>
    private static bool MergeColumn(SysCodeGenTableColumn current, ColumnConfigSuggestion inferred)
    {
        var modified = UserModifiedFieldSet.Parse(current.UserModifiedFields);
        var changed = false;

        // 结构性字段：始终以数据库为准（不是人工可配项，不受冻结影响）
        changed |= Set(() => current.ColumnType, value => current.ColumnType = value, inferred.ColumnType);
        changed |= Set(() => current.ColumnComment, value => current.ColumnComment = value, inferred.ColumnComment);
        changed |= Set(() => current.IsPrimaryKey, value => current.IsPrimaryKey = value, inferred.IsPrimaryKey);
        changed |= Set(() => current.IsIdentity, value => current.IsIdentity = value, inferred.IsIdentity);
        changed |= Set(() => current.IsNullable, value => current.IsNullable = value, inferred.IsNullable);
        changed |= Set(() => current.ColumnLength, value => current.ColumnLength = value, inferred.Length);
        changed |= Set(() => current.DecimalDigits, value => current.DecimalDigits = value, inferred.DecimalDigits);

        // 推断字段：未冻结才覆盖
        changed |= SetIfNotFrozen(modified, nameof(SysCodeGenTableColumn.CSharpType), () => current.CSharpType, value => current.CSharpType = value, inferred.CSharpType);
        changed |= SetIfNotFrozen(modified, nameof(SysCodeGenTableColumn.TsType), () => current.TsType, value => current.TsType = value, inferred.TsType);
        changed |= SetIfNotFrozen(modified, nameof(SysCodeGenTableColumn.HtmlType), () => current.HtmlType, value => current.HtmlType = value, inferred.HtmlType);
        changed |= SetIfNotFrozen(modified, nameof(SysCodeGenTableColumn.QueryType), () => current.QueryType, value => current.QueryType = value, inferred.QueryType);

        return changed;
    }

    /// <summary>
    /// 赋值并返回是否发生变化
    /// </summary>
    private static bool Set<T>(Func<T> getter, Action<T> setter, T value)
    {
        if (Equals(getter(), value))
        {
            return false;
        }

        setter(value);
        return true;
    }

    /// <summary>
    /// 未冻结才赋值
    /// </summary>
    private static bool SetIfNotFrozen<T>(HashSet<string> modified, string fieldName, Func<T> getter, Action<T> setter, T value)
    {
        return !modified.Contains(fieldName) && Set(getter, setter, value);
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<CodeGenResultDto> PreviewAsync(CodeGenPreviewRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var request = new GenerationRequest
        {
            TableId = input.TableId,
            TemplateCodes = input.TemplateCodes,
            GenType = GenType.Preview
        };

        var result = await _generationEngine.PreviewAsync(request, cancellationToken);
        return ToDto(result);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Execute)]
    public async Task<CodeGenResultDto> GenerateAsync(CodeGenGenerateRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = new GenerationRequest
        {
            TableId = input.TableId,
            TemplateCodes = input.TemplateCodes,
            GenType = input.GenType
        };

        var result = await _generationEngine.GenerateAsync(request, cancellationToken);

        // 无论成功失败均写一条历史留痕
        await WriteHistoryAsync(input, result, cancellationToken);

        return ToDto(result);
    }

    /// <summary>
    /// 写入一条代码生成历史记录（成功/失败均留痕）
    /// </summary>
    private async Task WriteHistoryAsync(CodeGenGenerateRequestDto input, GenerationResult result, CancellationToken cancellationToken)
    {
        var table = await _tableRepository.GetByIdAsync(input.TableId, cancellationToken);

        var totalSize = result.Artifacts.Sum(artifact => (long)Encoding.UTF8.GetByteCount(artifact.Content ?? string.Empty));

        var history = new SysCodeGenHistory
        {
            TableId = input.TableId,
            TableName = table?.TableName ?? string.Empty,
            BatchNumber = BuildBatchNumber(),
            GenStatus = result.Success ? GenStatus.Generated : GenStatus.Failed,
            GenType = input.GenType,
            GenTime = DateTimeOffset.UtcNow,
            Duration = result.DurationMilliseconds,
            FileCount = result.Artifacts.Count,
            TotalSize = totalSize,
            ErrorMessage = result.Success ? null : result.Message,
            OperatorId = _currentUser.UserId,
            OperatorName = _currentUser.UserName
        };

        await _historyRepository.AddAsync(history, cancellationToken);
    }

    /// <summary>
    /// 构建生成批次号：CG + UTC 时间戳 + 短随机
    /// </summary>
    private static string BuildBatchNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var random = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"CG{timestamp}{random}";
    }

    /// <summary>
    /// 引擎结果 → 应用层 DTO（Zip 包体以 Base64 透出，待 S3 改为流式下载令牌）
    /// </summary>
    private static CodeGenResultDto ToDto(GenerationResult result) => new()
    {
        Success = result.Success,
        Message = result.Message,
        FileCount = result.Artifacts.Count,
        DurationMilliseconds = result.DurationMilliseconds,
        Artifacts = [.. result.Artifacts.Select(artifact => new CodeGenArtifactDto
        {
            RelativePath = artifact.RelativePath,
            FileName = artifact.FileName,
            Content = artifact.Content,
            TemplateCode = artifact.TemplateCode,
            WriteMode = artifact.WriteMode
        })],
        WrittenCount = result.WrittenCount,
        SkippedPaths = result.SkippedPaths,
        PackageBase64 = result.Package is null ? null : Convert.ToBase64String(result.Package)
    };

    /// <summary>
    /// 用户显式输入优先，空白则回退推断值（去首尾空白）
    /// </summary>
    private static string? FirstNonBlank(string? userInput, string? inferred)
        => string.IsNullOrWhiteSpace(userInput) ? inferred : userInput.Trim();
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenerationAppService
// Guid:c0de9e00-0705-4a00-9000-000000000705
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text;
using XiHan.BasicApp.CodeGeneration.Application.Contracts;
using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
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
    ITypeMappingProvider typeMappingProvider,
    ICodeGenTableRepository tableRepository,
    ICodeGenTableColumnRepository tableColumnRepository,
    ICodeGenHistoryRepository historyRepository,
    ICodeGenTableQueryService tableQueryService,
    ICurrentUser currentUser) : CodeGenerationApplicationService, ICodeGenerationAppService
{
    private readonly ICodeGenerationEngine _generationEngine = generationEngine;
    private readonly IDatabaseSchemaImporter _schemaImporter = schemaImporter;
    private readonly ITypeMappingProvider _typeMappingProvider = typeMappingProvider;
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

        // 2) 扫描库表结构
        var schema = await _schemaImporter.ImportTableAsync(tableName, input.DataSourceId?.ToString(), cancellationToken)
            ?? throw new InvalidOperationException($"数据库表“{tableName}”不存在或无法读取结构。");

        // 3) 构建表配置
        var className = string.IsNullOrWhiteSpace(input.ClassName) ? Pascalize(tableName) : input.ClassName.Trim();
        var table = new SysCodeGenTable
        {
            TableName = tableName,
            TableComment = schema.TableComment,
            ClassName = className,
            Namespace = NormalizeNullable(input.Namespace),
            ModuleName = NormalizeNullable(input.ModuleName),
            BusinessName = NormalizeNullable(input.BusinessName),
            FunctionName = NormalizeNullable(input.FunctionName),
            Author = NormalizeNullable(input.Author),
            DatabaseType = input.DatabaseType,
            // 记住来源数据源：同步表结构与重新生成据此定位来源库，缺了会跑到主库上
            DataSourceId = input.DataSourceId,
            PrimaryKeyColumn = schema.PrimaryKeyColumn,
            GenStatus = GenStatus.NotGenerated,
            TemplateType = TemplateType.Single,
            GenType = GenType.Zip,
            Status = EnableStatus.Enabled
        };

        // 4) 持久化表配置（BasicId 由框架雪花算法自动填充）
        var tableId = await _tableRepository.AddReturnIdAsync(table, cancellationToken);

        // 5) 构建列配置
        var columns = new List<SysCodeGenTableColumn>(schema.Columns.Count);
        var sort = 0;
        foreach (var column in schema.Columns)
        {
            var mapping = _typeMappingProvider.Map(input.DatabaseType, column.DbType, column.IsNullable);
            columns.Add(new SysCodeGenTableColumn
            {
                TableId = tableId,
                ColumnName = column.ColumnName,
                ColumnComment = column.ColumnComment,
                ColumnType = column.DbType,
                CSharpType = mapping.CSharpType,
                CSharpProperty = Pascalize(column.ColumnName),
                TsType = mapping.TsType,
                HtmlType = mapping.DefaultHtmlType,
                QueryType = mapping.DefaultQueryType,
                ColumnLength = column.Length,
                DecimalDigits = column.DecimalDigits,
                IsPrimaryKey = column.IsPrimaryKey,
                IsIdentity = column.IsIdentity,
                IsNullable = column.IsNullable,
                IsRequired = column.IsRequired,
                IsList = true,
                IsInsert = true,
                IsEdit = true,
                IsQuery = false,
                Sort = sort++,
                Status = EnableStatus.Enabled
            });
        }

        if (columns.Count > 0)
        {
            await _tableColumnRepository.AddRangeAsync(columns, cancellationToken);
        }

        // 6) 返回导入后的详情
        return await _tableQueryService.GetDetailAsync(tableId, cancellationToken)
            ?? throw new InvalidOperationException("导入成功但读取表配置详情失败。");
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
    /// 把下划线/空格/连字符分隔的标识转为 PascalCase（如 sys_user → SysUser）
    /// </summary>
    private static string Pascalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var segments = value.Split(['_', ' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder(value.Length);
        foreach (var segment in segments)
        {
            builder.Append(char.ToUpperInvariant(segment[0]));
            if (segment.Length > 1)
            {
                builder.Append(segment[1..]);
            }
        }

        return builder.Length == 0 ? value : builder.ToString();
    }

    /// <summary>
    /// 空白归一：纯空白转 null，否则去首尾空白
    /// </summary>
    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

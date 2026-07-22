// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Application.Contracts;
using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.CodeGeneration.Application.AppServices;

/// <summary>
/// 零代码运行时（只读）应用服务
/// </summary>
/// <remarks>
/// 给定一张已配置且启用的 <see cref="SysCodeGenTable"/>（TableName 指向数据库真实表），
/// 运行时按其列配置（<see cref="SysCodeGenTableColumn"/>）暴露字段 schema，并按表名动态分页只读数据，
/// 全程不生成/编译任何实体代码。本切片仅 schema/list，写入/DDL 留待后续阶段。
/// </remarks>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "零代码运行时")]
public sealed class DynamicRuntimeAppService : CodeGenerationApplicationService, IDynamicRuntimeAppService
{
    private readonly ICodeGenTableRepository _tableRepository;
    private readonly ICodeGenTableColumnRepository _columnRepository;
    private readonly ISqlSugarClientResolver _clientResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DynamicRuntimeAppService(
        ICodeGenTableRepository tableRepository,
        ICodeGenTableColumnRepository columnRepository,
        ISqlSugarClientResolver clientResolver)
    {
        _tableRepository = tableRepository;
        _columnRepository = columnRepository;
        _clientResolver = clientResolver;
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<DynamicRuntimeSchemaDto> GetSchemaAsync(long tableId, CancellationToken ct = default)
    {
        var table = await GetEnabledTableAsync(tableId, ct);
        var columns = await _columnRepository.GetByTableIdAsync(tableId, ct);

        var columnDtos = columns
            .Select(column => new DynamicRuntimeColumnDto
            {
                ColumnName = column.ColumnName,
                PropertyName = ToCamelCase(string.IsNullOrWhiteSpace(column.CSharpProperty) ? column.ColumnName : column.CSharpProperty!),
                Label = column.ColumnComment,
                TsType = column.TsType,
                HtmlType = column.HtmlType.ToString(),
                QueryType = column.QueryType.ToString(),
                IsList = column.IsList,
                IsQuery = column.IsQuery,
                IsRequired = column.IsRequired
            })
            .ToList();

        return new DynamicRuntimeSchemaDto
        {
            TableId = table.BasicId,
            TableName = table.TableName,
            ClassName = table.ClassName,
            TableComment = table.TableComment,
            Columns = columnDtos
        };
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<DynamicRuntimePageResultDto> GetPageAsync(DynamicRuntimePageQueryDto input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ct.ThrowIfCancellationRequested();

        var pageIndex = input.PageIndex < 1 ? 1 : input.PageIndex;
        var pageSize = input.PageSize < 1 ? 20 : input.PageSize;

        // 安全：表名只来自已配置且启用的 SysCodeGenTable 记录，绝不直接使用用户传入的表名字符串，故无 SQL 注入面。
        var table = await GetEnabledTableAsync(input.TableId, ct);

        var client = _clientResolver.GetCurrentClient();
        RefAsync<int> total = 0;
        var rows = await client.Queryable<Dictionary<string, object>>()
            .AS(table.TableName)
            .ToPageListAsync(pageIndex, pageSize, total, ct);

        return new DynamicRuntimePageResultDto
        {
            Rows = rows,
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 获取已配置且启用的表配置（为空或非启用时抛友好异常）
    /// </summary>
    private async Task<SysCodeGenTable> GetEnabledTableAsync(long tableId, CancellationToken ct)
    {
        if (tableId <= 0)
        {
            throw new ArgumentException("表配置主键必须大于 0。", nameof(tableId));
        }

        ct.ThrowIfCancellationRequested();

        var table = await _tableRepository.GetByIdAsync(tableId, ct);
        return table is null
            ? throw new ArgumentException($"未找到主键为 {tableId} 的代码生成表配置。", nameof(tableId))
            : table.Status != EnableStatus.Enabled
            ? throw new ArgumentException($"代码生成表配置（{table.TableName}）未启用，无法在零代码运行时访问。", nameof(tableId))
            : table;
    }

    /// <summary>
    /// 转换为 camelCase（首字母小写）
    /// </summary>
    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}

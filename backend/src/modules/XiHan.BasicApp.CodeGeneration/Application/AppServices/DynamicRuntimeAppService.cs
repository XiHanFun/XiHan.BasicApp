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
using XiHan.Framework.MultiTenancy.Abstractions;

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
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DynamicRuntimeAppService(
        ICodeGenTableRepository tableRepository,
        ICodeGenTableColumnRepository columnRepository,
        ISqlSugarClientResolver clientResolver,
        ICurrentTenant currentTenant)
    {
        _tableRepository = tableRepository;
        _columnRepository = columnRepository;
        _clientResolver = clientResolver;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 获取指定已配置表的字段 schema
    /// </summary>
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

    /// <summary>
    /// 按表名动态分页查询指定已配置表的数据（只读）
    /// </summary>
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<DynamicRuntimePageResultDto> GetPageAsync(DynamicRuntimePageQueryDto input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ct.ThrowIfCancellationRequested();

        var pageIndex = input.PageIndex < 1 ? 1 : input.PageIndex;
        var pageSize = input.PageSize < 1 ? 20 : input.PageSize;

        // 安全：表名只来自已配置且启用的 SysCodeGenTable 记录，绝不直接使用用户传入的表名字符串，故无 SQL 注入面。
        var table = await GetEnabledTableAsync(input.TableId, ct);
        var columns = await _columnRepository.GetByTableIdAsync(table.BasicId, ct);

        var client = _clientResolver.GetCurrentClient();
        RefAsync<int> total = 0;

        // 必须走「无实体查询 + DataTable」：把 Dictionary<string, object> 当实体交给 Queryable，
        // SqlSugar 会反射字典自身的成员并把 Comparer/Count/Keys 当成列查，PostgreSQL 直接报
        // 42703 column "comparer" does not exist。
        // 原始表不经实体读过滤，租户与软删除条件按实体读过滤同口径显式补上
        var query = client.Queryable<object>()
            .AS(table.TableName)
            .Select("*");
        query = ApplyTenancyFilter(query, columns, table.TableName);
        var deletedColumn = FindColumn(columns, "isdeleted", "is_deleted");
        if (deletedColumn is not null)
        {
            query = query.Where($"{query.QueryBuilder.Builder.GetTranslationColumnName(deletedColumn)} = @isDeleted", new { isDeleted = false });
        }

        var dataTable = await query.ToDataTablePageAsync(pageIndex, pageSize, total);
        var rows = client.Utilities.DataTableToDictionaryList(dataTable);

        return new DynamicRuntimePageResultDto
        {
            Rows = rows,
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 租户条件：有租户列的表是业务数据，与生成实体同口径严格隔离——每个上下文（平台即 0 号租户）只看自己的行；
    /// 没有租户列的表不归属任何租户，只在平台上下文开放
    /// </summary>
    private ISugarQueryable<object> ApplyTenancyFilter(
        ISugarQueryable<object> query,
        IReadOnlyList<SysCodeGenTableColumn> columns,
        string tableName)
    {
        var tenantId = _currentTenant.Id ?? 0;
        var tenantColumn = FindColumn(columns, "tenantid", "tenant_id");
        if (tenantColumn is null)
        {
            return tenantId == 0
                ? query
                : throw new InvalidOperationException($"表 {tableName} 没有租户列，不归属任何租户，只能在平台访问。");
        }

        var column = query.QueryBuilder.Builder.GetTranslationColumnName(tenantColumn);
        return query.Where($"{column} = @tenantId", new { tenantId });
    }

    private static string? FindColumn(IReadOnlyList<SysCodeGenTableColumn> columns, params string[] names)
    {
        return columns
            .Select(column => column.ColumnName)
            .FirstOrDefault(name => names.Contains(name, StringComparer.OrdinalIgnoreCase));
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

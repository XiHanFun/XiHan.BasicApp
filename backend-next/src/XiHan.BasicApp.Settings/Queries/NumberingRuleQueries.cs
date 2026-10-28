// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.BasicApp.Settings.Operations.NumberingRules;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Queries;

/// <summary>
/// 编号规则列表查询（SqlSugar 直查，平台库）：平台主体只看平台规则；租户主体看自己的规则与 <c>AllowTenantUse</c> 的平台规则。
/// 关键字匹配编码与名称；筛选 <c>code</c>、<c>name</c>、<c>status</c>、<c>owner</c>（<c>platform</c> / <c>tenant</c>）、<c>resetCycle</c>；排序 <c>code</c>、<c>name</c>、<c>order</c>、<c>createdAt</c>。
/// </summary>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class NumberingRuleQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldCode = "code";
    private const string FieldName = "name";
    private const string FieldStatus = "status";
    private const string FieldOwner = "owner";
    private const string FieldResetCycle = "resetCycle";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出编号规则。
    /// </summary>
    public async ValueTask<Result<PageResult<NumberingRuleResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        const string platform = TenantIds.Platform;
        var query = TenantScopedQuery.For<SysNumberingRule>(unitOfWork.ConnectionFor(typeof(SysNumberingRule)), context);
        query = SettingsScope.IsPlatform(owner)
            ? query.Where(r => r.OwnerTenantId == platform)
            : query.Where(r => r.OwnerTenantId == owner || (r.OwnerTenantId == platform && r.AllowTenantUse));
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(r => r.Code.Contains(keyword) || r.Name.Contains(keyword));
        }

        foreach (var filter in request.Filters)
        {
            var filtered = Apply(query, filter);
            if (!filtered.TryGetValue(out query))
            {
                return filtered.Error!;
            }
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(r => r.Order, OrderByType.Asc).OrderBy(r => r.Code, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCode => query.OrderBy(r => r.Code, direction),
                FieldName => query.OrderBy(r => r.Name, direction),
                FieldOrder => query.OrderBy(r => r.Order, direction),
                FieldCreatedAt => query.OrderBy(r => r.CreatedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(SettingsErrors.QueryUnsupported, SettingsMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<NumberingRuleResponse>(rows.Select(NumberingRuleResponse.From).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysNumberingRule>> Apply(ISugarQueryable<SysNumberingRule> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        const string platform = TenantIds.Platform;
        return (filter.Field, filter.Operator) switch
        {
            (FieldCode, "eq") => Result.Success(query.Where(r => r.Code == value)),
            (FieldCode, "contains") => Result.Success(query.Where(r => r.Code.Contains(value))),
            (FieldName, "contains") => Result.Success(query.Where(r => r.Name.Contains(value))),
            (FieldStatus, "eq") when Enum.TryParse<SettingStatus>(value, ignoreCase: true, out var status) => Result.Success(query.Where(r => r.Status == status)),
            (FieldOwner, "eq") when value == "platform" => Result.Success(query.Where(r => r.OwnerTenantId == platform)),
            (FieldOwner, "eq") when value == "tenant" => Result.Success(query.Where(r => r.OwnerTenantId != platform)),
            (FieldResetCycle, "eq") when Enum.TryParse<NumberingResetCycle>(value, ignoreCase: true, out var cycle) => Result.Success(query.Where(r => r.ResetCycle == cycle)),
            _ => Error.Validation(SettingsErrors.QueryUnsupported, SettingsMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}

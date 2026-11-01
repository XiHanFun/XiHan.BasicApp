// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Domain.Services;
using XiHan.BasicApp.Printing.Operations.PrintTemplates;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Printing.Queries;

/// <summary>
/// 打印模板列表查询（SqlSugar 直查，平台库）。
/// </summary>
/// <remarks>
/// 作用域筛选 <c>scope</c>（<c>eq</c>）：平台一侧只有全局模板（全部状态，<c>Tenant</c> 即 <c>PRINTING_VALIDATION_004</c>），租户一侧缺省本租户、<c>Global</c> 是启用且开放的全局模板；
/// 作用域条件在客户端筛选之后追加，客户端改不动边界（4.x 同）。关键字匹配编码、名称、数据源与备注；
/// 筛选 <c>templateCode</c>（<c>eq</c> / <c>contains</c>）、<c>templateName</c>（<c>contains</c>）、<c>dataSourceCode</c>（<c>eq</c> / <c>contains</c>）、<c>status</c>、<c>allowTenantUse</c>；
/// 排序 <c>templateCode</c>、<c>templateName</c>、<c>dataSourceCode</c>、<c>status</c>、<c>order</c>、<c>createdAt</c>，缺省按排序号再按编码。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class PrintTemplateQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldScope = "scope";
    private const string FieldTemplateCode = "templateCode";
    private const string FieldTemplateName = "templateName";
    private const string FieldDataSourceCode = "dataSourceCode";
    private const string FieldStatus = "status";
    private const string FieldAllowTenantUse = "allowTenantUse";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出模板。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    public async ValueTask<Result<PageResult<PrintTemplateResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var side = PrintTemplateService.SideOf(context);
        var scoped = ScopeOf(request);
        if (!scoped.TryGetValue(out var scope))
        {
            return scoped.Error!;
        }

        if (PrintTemplateService.IsPlatform(side) && scope == PrintTemplateScope.Tenant)
        {
            return Error.Validation(PrintingErrors.ScopeInvalid, PrintingMessages.ScopeInvalid);
        }

        var query = TenantScopedQuery.For<SysPrintTemplate>(unitOfWork.ConnectionFor(typeof(SysPrintTemplate)), context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(t => t.TemplateCode.Contains(keyword) || t.TemplateName.Contains(keyword) || (t.DataSourceCode != null && t.DataSourceCode.Contains(keyword)) || (t.Remark != null && t.Remark.Contains(keyword)));
        }

        foreach (var filter in request.Filters.Where(static f => !string.Equals(f.Field, FieldScope, StringComparison.Ordinal)))
        {
            var filtered = Apply(query, filter);
            if (!filtered.TryGetValue(out query))
            {
                return filtered.Error!;
            }
        }

        const string platform = TenantIds.Platform;
        if (PrintTemplateService.IsPlatform(side))
        {
            query = query.Where(t => t.OwnerTenantId == platform);
        }
        else if (scope == PrintTemplateScope.Global)
        {
            query = query.Where(t => t.OwnerTenantId == platform && t.AllowTenantUse && t.Status == PrintTemplateStatus.Active);
        }
        else
        {
            query = query.Where(t => t.OwnerTenantId == side);
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(t => t.Order, OrderByType.Asc).OrderBy(t => t.TemplateCode, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldTemplateCode => query.OrderBy(t => t.TemplateCode, direction),
                FieldTemplateName => query.OrderBy(t => t.TemplateName, direction),
                FieldDataSourceCode => query.OrderBy(t => t.DataSourceCode, direction),
                FieldStatus => query.OrderBy(t => t.Status, direction),
                FieldOrder => query.OrderBy(t => t.Order, direction),
                FieldCreatedAt => query.OrderBy(t => t.CreatedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(PrintingErrors.QueryUnsupported, PrintingMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<PrintTemplateResponse>(rows.Select(PrintTemplateResponse.From).ToList(), total.Value, request.Page);
    }

    /// <summary>
    /// 请求里的作用域：没写即 <c>Auto</c>（平台是全局，租户是本租户）。
    /// </summary>
    private static Result<PrintTemplateScope> ScopeOf(PageRequest request)
    {
        var scope = PrintTemplateScope.Auto;
        foreach (var filter in request.Filters.Where(static f => string.Equals(f.Field, FieldScope, StringComparison.Ordinal)))
        {
            if (!string.Equals(filter.Operator, "eq", StringComparison.Ordinal) || !Enum.TryParse(filter.Value, ignoreCase: true, out scope) || !Enum.IsDefined(scope))
            {
                return Error.Validation(PrintingErrors.QueryUnsupported, PrintingMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));
            }
        }

        return scope;
    }

    private static Result<ISugarQueryable<SysPrintTemplate>> Apply(ISugarQueryable<SysPrintTemplate> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldTemplateCode, "eq") => Result.Success(query.Where(t => t.TemplateCode == value)),
            (FieldTemplateCode, "contains") => Result.Success(query.Where(t => t.TemplateCode.Contains(value))),
            (FieldTemplateName, "contains") => Result.Success(query.Where(t => t.TemplateName.Contains(value))),
            (FieldDataSourceCode, "eq") => Result.Success(query.Where(t => t.DataSourceCode == value)),
            (FieldDataSourceCode, "contains") => Result.Success(query.Where(t => t.DataSourceCode != null && t.DataSourceCode.Contains(value))),
            (FieldStatus, "eq") when Enum.TryParse<PrintTemplateStatus>(value, ignoreCase: true, out var status) => Result.Success(query.Where(t => t.Status == status)),
            (FieldAllowTenantUse, "eq") when bool.TryParse(value, out var open) => Result.Success(query.Where(t => t.AllowTenantUse == open)),
            _ => Error.Validation(PrintingErrors.QueryUnsupported, PrintingMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}

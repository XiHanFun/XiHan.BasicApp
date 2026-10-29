// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Operations.Messaging;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Queries;

/// <summary>
/// 模板、邮件与短信配置、发送记录的列表（都只看当前一侧）。筛选与排序逐个登记，未登记的报 <c>NOTIFICATION_VALIDATION_005</c>。
/// </summary>
/// <param name="unitOfWork">工作单元。</param>
public sealed class MessagingQueries(SqlSugarUnitOfWork unitOfWork)
{
    /// <summary>
    /// 模板：关键字匹配编码与名称；筛选渠道 <c>eq / in</c>、是否启用 <c>eq</c>；缺省按排序、编码。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>一页模板。</returns>
    public Task<Result<PageResult<MessageTemplateResponse>>> TemplatesAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SideRows.SideOf(context);
        var query = Owned<SysMessageTemplate>().Where(t => t.OwnerTenantId == owner);
        if (Keyword(request) is { } keyword)
        {
            query = query.Where(t => t.Code.Contains(keyword) || t.Name.Contains(keyword));
        }

        return PageAsync(query, request, context, (q, f, v) => (f.Field, f.Operator) switch
        {
            ("channel", "eq" or "in") when Enums.Of<TemplateChannel>(v) is { } channels => q.Where(t => channels.Contains(t.Channel)),
            ("isEnabled", "eq") when bool.TryParse(v, out var enabled) => q.Where(t => t.IsEnabled == enabled),
            _ => null,
        }, static (q, field, d) => field switch
        {
            "order" => q.OrderBy(t => t.Order, d),
            "code" => q.OrderBy(t => t.Code, d),
            "createdAt" => q.OrderBy(t => t.CreatedAt, d),
            _ => null,
        }, static q => q.OrderBy(t => t.Order).OrderBy(t => t.Code), MessageTemplateResponse.From);
    }

    /// <summary>
    /// 邮件配置：关键字匹配编码、名称与主机；筛选是否启用；缺省缺省配置在前、再按排序。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>一页配置。</returns>
    public Task<Result<PageResult<EmailConfigResponse>>> EmailConfigsAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SideRows.SideOf(context);
        var query = Owned<SysEmailConfig>().Where(c => c.OwnerTenantId == owner);
        if (Keyword(request) is { } keyword)
        {
            query = query.Where(c => c.Code.Contains(keyword) || c.Name.Contains(keyword) || c.SmtpHost.Contains(keyword));
        }

        return PageAsync(query, request, context, (q, f, v) => (f.Field, f.Operator) switch
        {
            ("isEnabled", "eq") when bool.TryParse(v, out var enabled) => q.Where(c => c.IsEnabled == enabled),
            _ => null,
        }, static (q, field, d) => field switch
        {
            "order" => q.OrderBy(c => c.Order, d),
            "createdAt" => q.OrderBy(c => c.CreatedAt, d),
            _ => null,
        }, static q => q.OrderBy(c => c.IsDefault, OrderByType.Desc).OrderBy(c => c.Order).OrderBy(c => c.Code), EmailConfigResponse.From);
    }

    /// <summary>
    /// 短信配置：关键字匹配编码与名称；筛选服务商 <c>eq / in</c>、是否启用；缺省缺省配置在前、再按排序。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>一页配置。</returns>
    public Task<Result<PageResult<SmsConfigResponse>>> SmsConfigsAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SideRows.SideOf(context);
        var query = Owned<SysSmsConfig>().Where(c => c.OwnerTenantId == owner);
        if (Keyword(request) is { } keyword)
        {
            query = query.Where(c => c.Code.Contains(keyword) || c.Name.Contains(keyword));
        }

        return PageAsync(query, request, context, (q, f, v) => (f.Field, f.Operator) switch
        {
            ("provider", "eq" or "in") when Enums.Of<SmsProvider>(v) is { } providers => q.Where(c => providers.Contains(c.Provider)),
            ("isEnabled", "eq") when bool.TryParse(v, out var enabled) => q.Where(c => c.IsEnabled == enabled),
            _ => null,
        }, static (q, field, d) => field switch
        {
            "order" => q.OrderBy(c => c.Order, d),
            "createdAt" => q.OrderBy(c => c.CreatedAt, d),
            _ => null,
        }, static q => q.OrderBy(c => c.IsDefault, OrderByType.Desc).OrderBy(c => c.Order).OrderBy(c => c.Code), SmsConfigResponse.From);
    }

    /// <summary>
    /// 邮件记录：关键字匹配收件地址与主题；筛选状态 <c>eq / in</c>、模板与业务类型 <c>eq</c>、写入时间 <c>ge / le</c>；缺省按写入时间倒序。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>一页记录。</returns>
    public Task<Result<PageResult<EmailRecordResponse>>> EmailsAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SideRows.SideOf(context);
        var query = Owned<SysEmail>().Where(e => e.OwnerTenantId == owner);
        if (Keyword(request) is { } keyword)
        {
            query = query.Where(e => e.ToEmail.Contains(keyword) || e.Subject.Contains(keyword));
        }

        return PageAsync(query, request, context, (q, f, v) => (f.Field, f.Operator) switch
        {
            ("status", "eq" or "in") when Enums.Of<MessageSendStatus>(v) is { } statuses => q.Where(e => statuses.Contains(e.Status)),
            ("templateCode", "eq") => q.Where(e => e.TemplateCode == v),
            ("businessType", "eq") => q.Where(e => e.BusinessType == v),
            ("createdAt", "ge") when DateTimeOffset.TryParse(v, out var from) => q.Where(e => e.CreatedAt >= from),
            ("createdAt", "le") when DateTimeOffset.TryParse(v, out var to) => q.Where(e => e.CreatedAt <= to),
            _ => null,
        }, static (q, field, d) => field switch
        {
            "createdAt" => q.OrderBy(e => e.CreatedAt, d),
            "sentAt" => q.OrderBy(e => e.SentAt, d),
            _ => null,
        }, static q => q.OrderBy(e => e.CreatedAt, OrderByType.Desc), EmailRecordResponse.From);
    }

    /// <summary>
    /// 短信记录：关键字匹配手机号与正文；筛选同邮件记录；缺省按写入时间倒序。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>一页记录。</returns>
    public Task<Result<PageResult<SmsRecordResponse>>> SmsAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SideRows.SideOf(context);
        var query = Owned<SysSms>().Where(s => s.OwnerTenantId == owner);
        if (Keyword(request) is { } keyword)
        {
            query = query.Where(s => s.ToPhone.Contains(keyword) || s.Content.Contains(keyword));
        }

        return PageAsync(query, request, context, (q, f, v) => (f.Field, f.Operator) switch
        {
            ("status", "eq" or "in") when Enums.Of<MessageSendStatus>(v) is { } statuses => q.Where(s => statuses.Contains(s.Status)),
            ("templateCode", "eq") => q.Where(s => s.TemplateCode == v),
            ("businessType", "eq") => q.Where(s => s.BusinessType == v),
            ("createdAt", "ge") when DateTimeOffset.TryParse(v, out var from) => q.Where(s => s.CreatedAt >= from),
            ("createdAt", "le") when DateTimeOffset.TryParse(v, out var to) => q.Where(s => s.CreatedAt <= to),
            _ => null,
        }, static (q, field, d) => field switch
        {
            "createdAt" => q.OrderBy(s => s.CreatedAt, d),
            "sentAt" => q.OrderBy(s => s.SentAt, d),
            _ => null,
        }, static q => q.OrderBy(s => s.CreatedAt, OrderByType.Desc), SmsRecordResponse.From);
    }

    private static string? Keyword(PageRequest request) => string.IsNullOrWhiteSpace(request.Keyword) ? null : request.Keyword.Trim();

    private ISugarQueryable<T> Owned<T>()
        where T : Contracts.Entities.AppAuditedEntity, new()
        => unitOfWork.ConnectionFor(typeof(T)).Queryable<T>().Where(Undeleted<T>());

    /// <summary>
    /// 未删除的条件按具体类型的属性建（与 <c>TenantScopedQuery</c> 同一做法，泛型基类上的成员表达式 SqlSugar 译不出列名）。
    /// </summary>
    private static Expression<Func<T, bool>> Undeleted<T>()
        where T : Contracts.Entities.AppAuditedEntity
    {
        var row = Expression.Parameter(typeof(T), "e");
        return Expression.Lambda<Func<T, bool>>(Expression.Not(Expression.Property(row, nameof(Contracts.Entities.AppAuditedEntity.IsDeleted))), row);
    }

    private static async Task<Result<PageResult<TResponse>>> PageAsync<T, TResponse>(
        ISugarQueryable<T> query,
        PageRequest request,
        InvocationContext context,
        Func<ISugarQueryable<T>, PageFilter, string, ISugarQueryable<T>?> filter,
        Func<ISugarQueryable<T>, string, OrderByType, ISugarQueryable<T>?> sort,
        Func<ISugarQueryable<T>, ISugarQueryable<T>> fallback,
        Func<T, TResponse> project)
    {
        foreach (var f in request.Filters)
        {
            var filtered = filter(query, f, f.Value ?? string.Empty);
            if (filtered is null)
            {
                return Error.Validation(NotificationErrors.QueryUnsupported, NotificationMessages.FilterUnsupported, MessageArguments.Of(("field", f.Field), ("operator", f.Operator)));
            }

            query = filtered;
        }

        if (request.Sorts.Count == 0)
        {
            query = fallback(query);
        }

        foreach (var s in request.Sorts)
        {
            var sorted = sort(query, s.Field, s.Descending ? OrderByType.Desc : OrderByType.Asc);
            if (sorted is null)
            {
                return Error.Validation(NotificationErrors.QueryUnsupported, NotificationMessages.SortUnsupported, MessageArguments.Of(("field", s.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<TResponse>(rows.Select(project).ToList(), total.Value, request.Page);
    }
}

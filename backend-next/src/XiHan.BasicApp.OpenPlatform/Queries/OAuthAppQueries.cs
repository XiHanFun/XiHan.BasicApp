// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.BasicApp.OpenPlatform.Operations.OauthApps;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.OpenPlatform.Queries;

/// <summary>
/// OAuth 应用的分页查询（平台库实体，只给平台管理用）。
/// </summary>
/// <param name="unitOfWork">工作单元。</param>
public sealed class OAuthAppQueries(SqlSugarUnitOfWork unitOfWork)
{
    /// <summary>
    /// 关键字匹配名称、描述、ClientId、主页与备注；筛选应用类型 <c>eq / in</c>、客户端类型、启停、跳过同意页 <c>eq</c>，<c>personal eq</c> 只看我的应用（或只看平台的）；
    /// 缺省按启停（启用在前）、创建时间倒序、名称排（4.x 同）。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>一页应用。</returns>
    public async Task<Result<PageResult<OAuthAppResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = unitOfWork.ConnectionFor(typeof(SysOAuthApp)).Queryable<SysOAuthApp>().Where(a => !a.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(a => a.AppName.Contains(keyword) || a.ClientId.Contains(keyword) || (a.AppDescription != null && a.AppDescription.Contains(keyword)) || (a.Homepage != null && a.Homepage.Contains(keyword)) || (a.Remark != null && a.Remark.Contains(keyword)));
        }

        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            ISugarQueryable<SysOAuthApp>? filtered = (filter.Field, filter.Operator) switch
            {
                ("appType", "eq" or "in") when EnumsOf<OAuthAppType>(value) is { } types => query.Where(a => types.Contains(a.AppType)),
                ("clientType", "eq") when Enum.TryParse<OAuthClientType>(value, ignoreCase: true, out var clientType) => query.Where(a => a.ClientType == clientType),
                ("isEnabled", "eq") when bool.TryParse(value, out var enabled) => query.Where(a => a.IsEnabled == enabled),
                ("skipConsent", "eq") when bool.TryParse(value, out var skip) => query.Where(a => a.SkipConsent == skip),
                ("personal", "eq") when bool.TryParse(value, out var personal) => personal ? query.Where(a => a.OwnerUserId != null) : query.Where(a => a.OwnerUserId == null),
                _ => null,
            };
            if (filtered is null)
            {
                return Error.Validation(OpenPlatformErrors.QueryUnsupported, OpenPlatformMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));
            }

            query = filtered;
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(a => a.IsEnabled, OrderByType.Desc).OrderBy(a => a.CreatedAt, OrderByType.Desc).OrderBy(a => a.AppName);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            ISugarQueryable<SysOAuthApp>? sorted = sort.Field switch
            {
                "appName" => query.OrderBy(a => a.AppName, direction),
                "clientId" => query.OrderBy(a => a.ClientId, direction),
                "accessTokenLifetime" => query.OrderBy(a => a.AccessTokenLifetime, direction),
                "createdAt" => query.OrderBy(a => a.CreatedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(OpenPlatformErrors.QueryUnsupported, OpenPlatformMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<OAuthAppResponse>([.. rows.Select(OAuthAppResponse.From)], total.Value, request.Page);
    }

    private static List<T>? EnumsOf<T>(string value)
        where T : struct, Enum
    {
        var result = new List<T>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<T>(part, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            {
                return null;
            }

            result.Add(parsed);
        }

        return result.Count == 0 ? null : result;
    }
}

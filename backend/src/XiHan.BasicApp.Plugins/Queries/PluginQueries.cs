// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Plugins.Domain.Entities;
using XiHan.BasicApp.Plugins.Operations.Plugins;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Plugins.Queries;

/// <summary>
/// 插件列表查询（SqlSugar 直查，平台库）
/// </summary>
/// <remarks>
/// 关键字匹配短名、清单 ID、发布者与备注；筛选 <c>name</c> / <c>manifestId</c>（<c>eq</c> / <c>contains</c>）、<c>isEnabled</c>、<c>isolation</c>；
/// 排序 <c>name</c>、<c>manifestId</c>、<c>version</c>、<c>createdAt</c>，缺省按短名
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元</param>
public sealed class PluginQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldName = "name";
    private const string FieldManifestId = "manifestId";
    private const string FieldVersion = "version";
    private const string FieldIsEnabled = "isEnabled";
    private const string FieldIsolation = "isolation";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出插件
    /// </summary>
    /// <param name="request">分页请求</param>
    /// <param name="context">调用上下文</param>
    public async ValueTask<Result<PageResult<PluginResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysPlugin>(unitOfWork.ConnectionFor(typeof(SysPlugin)), context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(p => p.Name.Contains(keyword) || p.ManifestId.Contains(keyword) || (p.Publisher != null && p.Publisher.Contains(keyword)) || (p.Remark != null && p.Remark.Contains(keyword)));
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
            query = query.OrderBy(p => p.Name, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldName => query.OrderBy(p => p.Name, direction),
                FieldManifestId => query.OrderBy(p => p.ManifestId, direction),
                FieldVersion => query.OrderBy(p => p.Version, direction),
                FieldCreatedAt => query.OrderBy(p => p.CreatedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(PluginsErrors.QueryUnsupported, PluginsMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<PluginResponse>(rows.Select(PluginResponse.From).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysPlugin>> Apply(ISugarQueryable<SysPlugin> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldName, "eq") => Result.Success(query.Where(p => p.Name == value)),
            (FieldName, "contains") => Result.Success(query.Where(p => p.Name.Contains(value))),
            (FieldManifestId, "eq") => Result.Success(query.Where(p => p.ManifestId == value)),
            (FieldManifestId, "contains") => Result.Success(query.Where(p => p.ManifestId.Contains(value))),
            (FieldIsEnabled, "eq") when bool.TryParse(value, out var enabled) => Result.Success(query.Where(p => p.IsEnabled == enabled)),
            (FieldIsolation, "eq") when Enum.TryParse<PluginIsolation>(value, ignoreCase: true, out var isolation) && Enum.IsDefined(isolation) => Result.Success(query.Where(p => p.Isolation == isolation)),
            _ => Error.Validation(PluginsErrors.QueryUnsupported, PluginsMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}

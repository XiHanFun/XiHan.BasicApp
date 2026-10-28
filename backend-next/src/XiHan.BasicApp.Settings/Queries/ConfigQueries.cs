// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Operations.Configs;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Queries;

/// <summary>
/// 参数列表查询（SqlSugar 直查，平台库，读共享）：关键字匹配键、名称与分组；筛选 <c>key</c>、<c>name</c>、<c>group</c>、<c>dataType</c>、<c>status</c>、
/// <c>owner</c>（<c>platform</c> / <c>tenant</c>）、<c>isEncrypted</c>、<c>isBuiltIn</c>；排序 <c>key</c>、<c>name</c>、<c>group</c>、<c>order</c>、<c>createdAt</c>。
/// </summary>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class ConfigQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldKey = "key";
    private const string FieldName = "name";
    private const string FieldGroup = "group";
    private const string FieldDataType = "dataType";
    private const string FieldStatus = "status";
    private const string FieldOwner = "owner";
    private const string FieldEncrypted = "isEncrypted";
    private const string FieldBuiltIn = "isBuiltIn";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出参数。
    /// </summary>
    public async ValueTask<Result<PageResult<ConfigResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysConfig>(unitOfWork.ConnectionFor(typeof(SysConfig)), context).OwnedByCurrentTenantOrPlatform(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(c => c.Key.Contains(keyword) || c.Name.Contains(keyword) || (c.Group != null && c.Group.Contains(keyword)));
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
            query = query.OrderBy(c => c.Group, OrderByType.Asc).OrderBy(c => c.Order, OrderByType.Asc).OrderBy(c => c.Key, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldKey => query.OrderBy(c => c.Key, direction),
                FieldName => query.OrderBy(c => c.Name, direction),
                FieldGroup => query.OrderBy(c => c.Group, direction),
                FieldOrder => query.OrderBy(c => c.Order, direction),
                FieldCreatedAt => query.OrderBy(c => c.CreatedAt, direction),
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
        return new PageResult<ConfigResponse>(rows.Select(ConfigResponse.From).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysConfig>> Apply(ISugarQueryable<SysConfig> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        const string platform = TenantIds.Platform;
        return (filter.Field, filter.Operator) switch
        {
            (FieldKey, "eq") => Result.Success(query.Where(c => c.Key == value)),
            (FieldKey, "contains") => Result.Success(query.Where(c => c.Key.Contains(value))),
            (FieldName, "contains") => Result.Success(query.Where(c => c.Name.Contains(value))),
            (FieldGroup, "eq") => Result.Success(query.Where(c => c.Group == value)),
            (FieldDataType, "eq") when Enum.TryParse<ConfigDataType>(value, ignoreCase: true, out var dataType) => Result.Success(query.Where(c => c.DataType == dataType)),
            (FieldStatus, "eq") when Enum.TryParse<SettingStatus>(value, ignoreCase: true, out var status) => Result.Success(query.Where(c => c.Status == status)),
            (FieldOwner, "eq") when value == "platform" => Result.Success(query.Where(c => c.OwnerTenantId == platform)),
            (FieldOwner, "eq") when value == "tenant" => Result.Success(query.Where(c => c.OwnerTenantId != platform)),
            (FieldEncrypted, "eq") when bool.TryParse(value, out var encrypted) => Result.Success(query.Where(c => c.IsEncrypted == encrypted)),
            (FieldBuiltIn, "eq") when bool.TryParse(value, out var builtIn) => Result.Success(query.Where(c => c.IsBuiltIn == builtIn)),
            _ => Error.Validation(SettingsErrors.QueryUnsupported, SettingsMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Queries;

/// <summary>
/// 用户的分页查询。
/// </summary>
/// <remarks>
/// 直接用 SqlSugar 客户端（按实体落点从工作单元取连接），以 <see cref="TenantScopedQuery"/> 起手补软删除条件；租户主体只看注册地是当前租户的用户。
/// </remarks>
/// <param name="unitOfWork">当前调用的工作单元；查询与同一调用里的写入共用连接与事务。</param>
public sealed class UserQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldUserName = "userName";
    private const string FieldDisplayName = "displayName";
    private const string FieldEmail = "email";
    private const string FieldStatus = "status";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出用户。
    /// </summary>
    /// <remarks>
    /// 筛选只接受 <c>userName / displayName / email</c> 的 <c>eq / contains</c> 与 <c>status</c> 的 <c>eq</c>；排序只接受 <c>userName / displayName / createdAt</c>，缺省按创建时间倒序。
    /// </remarks>
    public async ValueTask<Result<PageResult<UserResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysUser>(unitOfWork.ConnectionFor(typeof(SysUser)), context);
        if (context.GetSecurity().Tenant?.Id is { } tenantId)
        {
            query = query.Where(u => u.HomeTenantId == tenantId);
        }

        // 数据范围（方案 5.6）：受限主体只看得到自己与可见部门里的用户
        query = query.ApplyDataScope(await DataScopeQuery.ResolveAsync(context).ConfigureAwait(false), static u => u.Id);

        // 字段安全（方案 5.9）：读受保护的字段不能拿来筛选与排序（按结果可反推原值），关键字搜索也不再覆盖它们
        var rules = await FieldSecurityQuery.ResolveAsync(context, IdentityFieldSecurity.Users).ConfigureAwait(false);
        if (rules.FirstProtected(request) is { } protectedField)
        {
            return Error.Denied(IdentityErrors.FieldProtected, IdentityMessages.FieldProtected, MessageArguments.Of(("field", protectedField)));
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            switch (rules.CanSearch(IdentityFieldSecurity.UserName), rules.CanSearch(IdentityFieldSecurity.DisplayName))
            {
                case (true, true):
                    query = query.Where(u => u.UserName.Contains(keyword) || u.DisplayName.Contains(keyword));
                    break;
                case (true, false):
                    query = query.Where(u => u.UserName.Contains(keyword));
                    break;
                case (false, true):
                    query = query.Where(u => u.DisplayName.Contains(keyword));
                    break;
                default:
                    // 关键字能搜的字段都受保护：没有可匹配的行
                    return new PageResult<UserResponse>([], 0, request.Page);
            }
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
            query = query.OrderBy(u => u.CreatedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldUserName => query.OrderBy(u => u.UserName, direction),
                FieldDisplayName => query.OrderBy(u => u.DisplayName, direction),
                FieldCreatedAt => query.OrderBy(u => u.CreatedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(IdentityErrors.QueryUnsupported, IdentityMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<UserResponse>(rows.Select(u => UserResponse.From(u, rules)).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysUser>> Apply(ISugarQueryable<SysUser> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldUserName, "eq") => Result.Success(query.Where(u => u.UserName == value)),
            (FieldUserName, "contains") => Result.Success(query.Where(u => u.UserName.Contains(value))),
            (FieldDisplayName, "eq") => Result.Success(query.Where(u => u.DisplayName == value)),
            (FieldDisplayName, "contains") => Result.Success(query.Where(u => u.DisplayName.Contains(value))),
            (FieldEmail, "eq") => Result.Success(query.Where(u => u.Email == value)),
            (FieldEmail, "contains") => Result.Success(query.Where(u => u.Email != null && u.Email.Contains(value))),
            (FieldStatus, "eq") when Enum.TryParse<UserStatus>(value, ignoreCase: true, out var status) => Result.Success(query.Where(u => u.Status == status)),
            _ => Error.Validation(IdentityErrors.QueryUnsupported, IdentityMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}

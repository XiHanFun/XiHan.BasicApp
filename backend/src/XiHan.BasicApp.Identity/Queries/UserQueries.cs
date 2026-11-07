// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using SqlSugar;
using XiHan.BasicApp.Contracts.Organization;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Queries;

/// <summary>
/// 用户的分页查询
/// </summary>
/// <remarks>
/// 直接用 SqlSugar 客户端（按实体落点从工作单元取连接），以 <see cref="TenantScopedQuery"/> 起手补软删除条件；租户主体看注册地是当前租户的用户与本租户的外部成员
/// 角色与部门在别的包：按这一页的用户经 Contracts 批量取，账号安全在本包，同样按这一页批量取
/// </remarks>
/// <param name="unitOfWork">当前调用的工作单元；查询与同一调用里的写入共用连接与事务</param>
/// <param name="recipients">按角色、部门找人的来源（Access 与 Organization 实现，用于筛选）</param>
/// <param name="sideUsers">这一侧的用户（外部成员）</param>
/// <param name="roles">角色名称（Access 实现）</param>
/// <param name="organization">主部门名称（Organization 实现）</param>
/// <param name="clock">时钟</param>
public sealed class UserQueries(SqlSugarUnitOfWork unitOfWork, IEnumerable<IRecipientSource> recipients, SideUsers sideUsers, IEnumerable<IRoleMemberDirectory> roles, IEnumerable<IOrganizationDirectory> organization, TimeProvider clock)
{
    private const string FieldUserName = "userName";
    private const string FieldDisplayName = "displayName";
    private const string FieldEmail = "email";
    private const string FieldStatus = "status";
    private const string FieldCreatedAt = "createdAt";
    private const string FieldHomeTenantId = "homeTenantId";
    private const string FieldPhoneNumber = "phoneNumber";
    private const string FieldGender = "gender";
    private const string FieldRoleId = "roleId";
    private const string FieldDepartmentId = "departmentId";
    private const string FieldIsSupportMember = "isSupportMember";

    /// <summary>
    /// 分页列出用户
    /// </summary>
    /// <remarks>
    /// 筛选只接受 <c>userName / displayName / email</c> 的 <c>eq / contains</c> 、<c>status</c> 的 <c>eq</c> 、<c>homeTenantId</c> 的 <c>eq / ne</c>（平台侧挑某租户的用户或平台账号；租户侧只在本租户的用户与成员里挑）与 <c>isSupportMember</c> 的 <c>eq</c>（取值 <c>true / false</c>，只能配角色的人不该出现在别的选人入口）；排序只接受 <c>userName / displayName / createdAt</c>，缺省按创建时间倒序
    /// </remarks>
    public async ValueTask<Result<PageResult<UserListItemResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysUser>(unitOfWork.ConnectionFor(typeof(SysUser)), context);
        var tenantId = context.GetSecurity().Tenant?.Id;
        IReadOnlySet<Guid> support = new HashSet<Guid>();
        if (tenantId is not null)
        {
            // 外部成员：别处注册、生效中的普通成员也算这一侧的用户；运维支持成员也列出来，带标记、账号只读
            support = await sideUsers.SupportMembersOfAsync(tenantId, context.CancellationToken).ConfigureAwait(false);
            var members = (await sideUsers.MembersOfAsync(tenantId, context.CancellationToken).ConfigureAwait(false)).Concat(support).ToList();
            query = members.Count == 0
                ? query.Where(u => u.HomeTenantId == tenantId)
                : query.Where(u => u.HomeTenantId == tenantId || members.Contains(u.Id));
        }

        // 数据范围（方案 5.6）：受限主体只看得到自己与可见部门里的用户
        query = query.ApplyDataScope(await DataScopeQuery.ResolveAsync(context).ConfigureAwait(false), static u => u.Id);

        // 字段安全（方案 5.9）：筛选与排序的门控、响应打码在 Access 的中间件里；关键字搜索不再覆盖读受保护的字段（按结果可反推原值）
        var rules = await FieldSecurityQuery.ResolveAsync(context, IdentityFieldSecurity.Users).ConfigureAwait(false);

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
                    return new PageResult<UserListItemResponse>([], 0, request.Page);
            }
        }

        foreach (var filter in request.Filters)
        {
            // 按角色、部门筛：先经对应来源找出这些人（角色只算启用的与生效中的指派，部门含下级），再收窄
            if (filter.Field is FieldRoleId or FieldDepartmentId)
            {
                var members = await MembersAsync(filter, context).ConfigureAwait(false);
                if (!members.TryGetValue(out var ids))
                {
                    return members.Error!;
                }

                query = query.Where(u => ids.Contains(u.Id));
                continue;
            }

            if (filter.Field == FieldIsSupportMember)
            {
                if (filter.Operator != "eq" || !bool.TryParse(filter.Value, out var wanted))
                {
                    return Error.Validation(IdentityErrors.QueryUnsupported, IdentityMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));
                }

                if (support.Count == 0 && wanted)
                {
                    return new PageResult<UserListItemResponse>([], 0, request.Page);
                }

                if (support.Count > 0)
                {
                    var supportIds = support.ToList();
                    query = wanted ? query.Where(u => supportIds.Contains(u.Id)) : query.Where(u => !supportIds.Contains(u.Id));
                }

                continue;
            }

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
        return new PageResult<UserListItemResponse>(await ComposeAsync(rows, tenantId, support, context).ConfigureAwait(false), total.Value, request.Page);
    }

    /// <summary>
    /// 把这一页的用户配上角色、部门与安全标记
    /// </summary>
    /// <remarks>
    /// 每种各一次批量查询，不随行数增加；角色与部门没装对应的包即没有
    /// </remarks>
    private async Task<List<UserListItemResponse>> ComposeAsync(List<SysUser> rows, string? tenantId, IReadOnlySet<Guid> support, InvocationContext context)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(static u => u.Id).ToList();
        var roleNames = roles.FirstOrDefault() is { } directory ? await directory.RoleNamesAsync(context, ids).ConfigureAwait(false) : new Dictionary<Guid, IReadOnlyList<string>>();
        var departments = tenantId is not null && organization.FirstOrDefault() is { } tree ? await tree.PrimaryDepartmentNamesAsync(context, ids).ConfigureAwait(false) : new Dictionary<Guid, string>();
        // 只取要的列（投影到实体，走实体映射的时间转换）；没有安全行的按缺省值（没开双因素、没在锁定期）
        var securities = (await TenantScopedQuery.For<SysUserSecurity>(unitOfWork.ConnectionFor(typeof(SysUserSecurity)), context)
                .Where(s => ids.Contains(s.UserId))
                .Select(s => new SysUserSecurity { UserId = s.UserId, TwoFactorEnabled = s.TwoFactorEnabled, LockoutEndAt = s.LockoutEndAt })
                .ToListAsync(context.CancellationToken).ConfigureAwait(false))
            .ToDictionary(static s => s.UserId);
        var now = clock.GetUtcNow();
        return rows.Select(user =>
        {
            var security = securities.GetValueOrDefault(user.Id);
            return UserListItemResponse.From(
                UserResponse.From(user, tenantId, support.Contains(user.Id)),
                roleNames.GetValueOrDefault(user.Id) ?? [],
                departments.GetValueOrDefault(user.Id),
                user.Status == UserStatus.Locked || security?.LockoutEndAt > now,
                security?.TwoFactorEnabled == true);
        }).ToList();
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
            (FieldHomeTenantId, "eq") => Result.Success(query.Where(u => u.HomeTenantId == value)),
            (FieldHomeTenantId, "ne") => Result.Success(query.Where(u => u.HomeTenantId != value)),
            (FieldStatus, "eq") when Enum.TryParse<UserStatus>(value, ignoreCase: true, out var status) => Result.Success(query.Where(u => u.Status == status)),
            (FieldStatus, "in") when ParseAll<UserStatus>(value) is { } statuses => Result.Success(query.Where(u => statuses.Contains(u.Status))),
            (FieldPhoneNumber, "eq") => Result.Success(query.Where(u => u.PhoneNumber == value)),
            (FieldPhoneNumber, "contains") => Result.Success(query.Where(u => u.PhoneNumber != null && u.PhoneNumber.Contains(value))),
            (FieldGender, "eq") when Enum.TryParse<UserGender>(value, ignoreCase: true, out var gender) => Result.Success(query.Where(u => u.Gender == gender)),
            (FieldGender, "in") when ParseAll<UserGender>(value) is { } genders => Result.Success(query.Where(u => genders.Contains(u.Gender))),
            (FieldCreatedAt, "ge") when TryParseTime(value, out var from) => Result.Success(query.Where(u => u.CreatedAt >= from)),
            (FieldCreatedAt, "gt") when TryParseTime(value, out var after) => Result.Success(query.Where(u => u.CreatedAt > after)),
            (FieldCreatedAt, "le") when TryParseTime(value, out var to) => Result.Success(query.Where(u => u.CreatedAt <= to)),
            (FieldCreatedAt, "lt") when TryParseTime(value, out var before) => Result.Success(query.Where(u => u.CreatedAt < before)),
            _ => Error.Validation(IdentityErrors.QueryUnsupported, IdentityMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
    /// <summary>
    /// 按角色或部门筛选时，经对应来源找出的用户
    /// </summary>
    /// <remarks>
    /// 取值是一个 ID（<c>eq</c>）或逗号分隔的多个（<c>in</c>）；没装对应的包即不支持这个筛选
    /// </remarks>
    private async ValueTask<Result<List<Guid>>> MembersAsync(PageFilter filter, InvocationContext context)
    {
        var kind = filter.Field == FieldRoleId ? RecipientKind.Role : RecipientKind.Department;
        var source = recipients.FirstOrDefault(r => r.Kind == kind);
        var targets = (filter.Value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(static v => Guid.TryParse(v, out var id) ? id : (Guid?)null).ToList();
        if (source is null || filter.Operator is not ("eq" or "in") || targets.Count == 0 || targets.Any(static id => id is null) || (filter.Operator == "eq" && targets.Count != 1))
        {
            return Error.Validation(IdentityErrors.QueryUnsupported, IdentityMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));
        }

        var members = await source.ResolveAsync(context, targets.Select(static id => id!.Value).ToList()).ConfigureAwait(false);
        return members.ToList();
    }

    /// <summary>
    /// 逗号分隔的枚举值，有一个认不出即为空
    /// </summary>
    private static List<TEnum>? ParseAll<TEnum>(string value)
        where TEnum : struct, Enum
    {
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parsed = parts.Select(static p => Enum.TryParse<TEnum>(p, ignoreCase: true, out var v) ? v : (TEnum?)null).ToList();
        return parsed.Count == 0 || parsed.Any(static v => v is null) ? null : parsed.Select(static v => v!.Value).ToList();
    }

    /// <summary>
    /// ISO 8601 时间（没写时区按 UTC）
    /// </summary>
    private static bool TryParseTime(string value, out DateTimeOffset time)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out time);
}

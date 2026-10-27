// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 创建用户：JWT 认证 → 主体声明解析租户 → 按契约判定 <c>identity.users.create</c> → Validate 阶段按 Schema 与自校验拒绝坏请求 → 工作单元内写用户（平台库）与用户统计（租户库），两边同一调用一起提交或回滚。
/// N0 为验证三种协议同时暴露；按 5.4 的规则，N1 定操作表时管理类写操作收回到只 <c>Http</c>（内部服务需要时加 <c>Grpc</c>）。
/// </summary>
/// <param name="users">用户仓储（默认连接）。</param>
/// <param name="statistics">用户统计仓储（带分片的调用落 <c>shard</c> 连接）。</param>
/// <param name="ids">标识生成器（UUID v7）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Grpc | Expose.Mcp, Permission = IdentityPermissions.UsersCreate)]
public sealed class CreateUser(IRepository<SysUser, Guid> users, IRepository<SysUserStatistics, Guid> statistics, IIdGenerator<Guid> ids) : IOperation<CreateUserRequest, UserResponse>
{
    /// <summary>操作 ID。</summary>
    public const string Id = "users.create";

    /// <inheritdoc />
    public async ValueTask<Result<UserResponse>> HandleAsync(CreateUserRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var tenantId = context.GetSecurity().Tenant?.Id;
        if (tenantId is null)
        {
            return Error.Validation(IdentityErrors.TenantRequired, IdentityMessages.TenantRequired);
        }

        var userName = request.UserName.Trim();
        var taken = await users.CountAsync(
            EntityQuery.All<SysUser>().Where(new Specification<SysUser>(u => u.HomeTenantId == tenantId && u.UserName == userName)),
            context.CancellationToken).ConfigureAwait(false);
        if (taken > 0)
        {
            return Error.Conflict(IdentityErrors.UserNameTaken, IdentityMessages.UserNameTaken, MessageArguments.Of(("userName", userName), ("tenant", tenantId)));
        }

        var user = new SysUser
        {
            Id = ids.NewId(),
            UserName = userName,
            DisplayName = request.DisplayName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            HomeTenantId = tenantId,
            Status = UserStatus.Active,
        };

        // 仓储返回 Result 而不是抛异常：失败直接作为操作结果，工作单元回滚两边的写入
        var added = await users.AddAsync(user, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var stats = await statistics.AddAsync(new SysUserStatistics { Id = ids.NewId(), UserId = user.Id }, context.CancellationToken).ConfigureAwait(false);
        return stats.IsFailure ? stats.Error! : UserResponse.From(user);
    }
}

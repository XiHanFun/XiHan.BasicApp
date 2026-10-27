// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 创建用户：JWT 认证 → 主体声明解析租户 → 按契约判定 <c>identity.users.create</c> → Validate 阶段按 Schema 与自校验拒绝坏请求 → 工作单元内写用户、账号安全（可带初始密码）与用户统计（租户库）。
/// 注册地租户：租户主体只能建本租户的用户；平台主体可指定租户，不指定即平台账号。统计行只在当前租户就是注册地时创建（租户库写入需要租户上下文），其余情形在首次登录时补。
/// 暴露给 HTTP、内部服务调用（gRPC）与 MCP 工具（不暴露给 AI 工具目录）；N1 的操作表定稿时按 5.4 复核暴露面。
/// </summary>
/// <param name="users">用户仓储（默认连接）。</param>
/// <param name="statistics">用户统计仓储（带分片的调用落 <c>shard</c> 连接）。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="ids">标识生成器（UUID v7）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Grpc | Expose.Mcp, Permission = IdentityPermissions.UsersCreate)]
public sealed class CreateUser(IRepository<SysUser, Guid> users, IRepository<SysUserStatistics, Guid> statistics, UserAccountService accounts, IIdGenerator<Guid> ids) : IOperation<CreateUserRequest, UserResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "users.create";

    /// <inheritdoc />
    public async ValueTask<Result<UserResponse>> HandleAsync(CreateUserRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var currentTenant = context.GetSecurity().Tenant?.Id;
        var requested = string.IsNullOrWhiteSpace(request.HomeTenantId) ? null : request.HomeTenantId.Trim();
        if (currentTenant is not null && requested is not null && !string.Equals(currentTenant, requested, StringComparison.Ordinal))
        {
            return Error.Validation(IdentityErrors.TenantMismatch, IdentityMessages.TenantMismatch, MessageArguments.Of(("requested", requested), ("current", currentTenant)));
        }

        var homeTenant = currentTenant ?? requested ?? TenantIds.Platform;
        var userName = request.UserName.Trim();
        if ((await accounts.FindByNameAsync(homeTenant, userName, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Conflict(IdentityErrors.UserNameTaken, IdentityMessages.UserNameTaken, MessageArguments.Of(("userName", userName), ("tenant", homeTenant)));
        }

        var user = new SysUser
        {
            Id = ids.NewId(),
            UserName = userName,
            DisplayName = request.DisplayName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            HomeTenantId = homeTenant,
            Status = UserStatus.Active,
        };

        // 仓储返回 Result 而不是抛异常：失败直接作为操作结果，工作单元回滚全部写入
        var added = await users.AddAsync(user, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var securityResult = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        if (request.Password is { Length: > 0 })
        {
            var set = await accounts.SetPasswordAsync(security, request.Password, mustChange: false, context.CancellationToken).ConfigureAwait(false);
            if (set.IsFailure)
            {
                return set.Error!;
            }
        }

        if (currentTenant is not null && string.Equals(currentTenant, homeTenant, StringComparison.Ordinal))
        {
            var stats = await statistics.AddAsync(new SysUserStatistics { Id = ids.NewId(), UserId = user.Id }, context.CancellationToken).ConfigureAwait(false);
            if (stats.IsFailure)
            {
                return stats.Error!;
            }
        }

        return UserResponse.From(user);
    }
}

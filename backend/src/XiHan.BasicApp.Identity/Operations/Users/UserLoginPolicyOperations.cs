// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// <c>users.set-login-policy</c> 的请求
/// </summary>
/// <remarks>
/// 标识由路由值 <c>{id}</c> 绑定
/// </remarks>
/// <param name="Id">用户 ID</param>
/// <param name="AllowMultiLogin">是否允许多端登录；不允许时登录只保留最新的一个会话</param>
/// <param name="MaxLoginDevices">允许多端登录时的最大设备数，超过时吊销最早的会话；0 即不限</param>
public sealed record SetUserLoginPolicyRequest(Guid Id, bool AllowMultiLogin, [Range(0, 99)] int MaxLoginDevices);

/// <summary>
/// 用户的登录策略
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="AllowMultiLogin">是否允许多端登录</param>
/// <param name="MaxLoginDevices">最大设备数，0 即不限</param>
public sealed record UserLoginPolicyResponse(Guid UserId, bool AllowMultiLogin, int MaxLoginDevices)
{
    /// <summary>
    /// 由账号安全投影
    /// </summary>
    public static UserLoginPolicyResponse From(SysUserSecurity security)
    {
        ArgumentNullException.ThrowIfNull(security);
        return new UserLoginPolicyResponse(security.UserId, !security.DenyMultiLogin, security.MaxLoginDevices);
    }
}

/// <summary>
/// 查用户的登录策略
/// </summary>
/// <remarks>
/// <c>GET /api/users/{id}:login-policy</c>。租户主体只看本租户、受数据范围约束，范围外按不存在回答
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="accounts">账号域服务</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = IdentityPermissions.UsersRead)]
public sealed class GetUserLoginPolicy(IRepository<SysUser, Guid> users, UserAccountService accounts) : IOperation<GetUserRequest, UserLoginPolicyResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "users.login-policy";

    /// <inheritdoc />
    public async ValueTask<Result<UserLoginPolicyResponse>> HandleAsync(GetUserRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var (targets, _) = await UserBatch.ResolveAsync(users, [request.Id], context, rejectSelf: false).ConfigureAwait(false);
        if (targets.Count == 0)
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var security = await accounts.SecurityOfAsync(targets[0].Id, context.CancellationToken).ConfigureAwait(false);
        return security.TryGetValue(out var row) ? UserLoginPolicyResponse.From(row) : security.Error!;
    }
}

/// <summary>
/// 设置用户的登录策略
/// </summary>
/// <remarks>
/// <c>POST /api/users/{id}:set-login-policy</c>。下次登录生效，已有的会话不动
/// 在登录时执行（<see cref="UserAccountService.EnforceLoginPolicyAsync"/>）
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="securities">账号安全仓储</param>
/// <param name="accounts">账号域服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.UsersLoginPolicy)]
public sealed class SetUserLoginPolicy(IRepository<SysUser, Guid> users, IRepository<SysUserSecurity, Guid> securities, UserAccountService accounts) : IOperation<SetUserLoginPolicyRequest, UserLoginPolicyResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "users.set-login-policy";

    /// <inheritdoc />
    public async ValueTask<Result<UserLoginPolicyResponse>> HandleAsync(SetUserLoginPolicyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var (targets, _) = await UserBatch.ResolveAsync(users, [request.Id], context, rejectSelf: false).ConfigureAwait(false);
        if (targets.Count == 0)
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var securityResult = await accounts.SecurityOfAsync(targets[0].Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        security.DenyMultiLogin = !request.AllowMultiLogin;
        security.MaxLoginDevices = request.MaxLoginDevices;
        var updated = await securities.UpdateAsync(security, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : UserLoginPolicyResponse.From(security);
    }
}

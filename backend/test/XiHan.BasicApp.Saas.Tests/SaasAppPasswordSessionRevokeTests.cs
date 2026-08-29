// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 改密与锁定后必须吊销会话的结构约束。
/// </summary>
/// <remarks>
/// 安全戳（<c>SysUserSecurity.SecurityStamp</c>）只被写、没有任何地方读，
/// 会话闸门判的是会话行的状态与有效期。因此"改完密码旧令牌失效"这件事
/// 完全靠这几条路显式吊销会话，摘掉即静默失效——用调用图把它钉住。
///
/// 这几个应用服务的依赖多、且基类走属性注入，端到端实例化不现实，
/// 故与仓内既有做法一致，用 IL 调用图探针断言"方法体里到底调没调那一下"。
/// </remarks>
public sealed class SaasAppPasswordSessionRevokeTests
{
    /// <summary>
    /// 管理员重置密码后吊销该用户全部会话。
    /// </summary>
    [Fact]
    public void ResetUserPasswordAsync_ShouldRevokeUserSessions()
    {
        Assert.True(
            ReachesRevokeByUserId(Method<UserSecurityAppService>("ResetUserPasswordAsync")),
            "重置密码必须吊销该用户全部会话，否则旧令牌一直用到会话自然过期。");
    }

    /// <summary>
    /// 管理员重置密码后一并吊销由该用户发起的模仿会话。
    /// </summary>
    [Fact]
    public void ResetUserPasswordAsync_ShouldRevokeImpersonationSessions()
    {
        Assert.True(
            ReachesRevokeByImpersonatorUserId(Method<UserSecurityAppService>("ResetUserPasswordAsync")),
            "模仿会话行的 UserId 是被模仿者，须按模仿者另吊销一次。");
    }

    /// <summary>
    /// 锁定账号后吊销该用户全部会话。
    /// </summary>
    [Fact]
    public void UpdateUserLockAsync_ShouldRevokeUserSessions()
    {
        Assert.True(
            ReachesRevokeByUserId(Method<UserSecurityAppService>("UpdateUserLockAsync")),
            "锁定账号必须吊销会话，否则锁定要等到会话自然过期才生效。");
    }

    /// <summary>
    /// 锁定账号后一并吊销由该用户发起的模仿会话。
    /// </summary>
    [Fact]
    public void UpdateUserLockAsync_ShouldRevokeImpersonationSessions()
    {
        Assert.True(
            ReachesRevokeByImpersonatorUserId(Method<UserSecurityAppService>("UpdateUserLockAsync")),
            "模仿会话行的 UserId 是被模仿者，须按模仿者另吊销一次。");
    }

    /// <summary>
    /// 自助改密后撤销其它会话（当前会话保留，由 RevokeOtherSessionsAsync 的语义保证）。
    /// </summary>
    [Fact]
    public void ChangePasswordAsync_ShouldRevokeOtherSessions()
    {
        var reached = SaasAppIlCallGraph.Reaches(
            Method<ProfileAppService>("ChangePasswordAsync"),
            typeof(ProfileAppService),
            callee => callee.DeclaringType == typeof(IProfileDomainService)
                && callee.Name == nameof(IProfileDomainService.RevokeOtherSessionsAsync));

        Assert.True(reached, "自助改密必须踢掉其它设备，否则旧令牌一直用到会话自然过期。");
    }

    /// <summary>
    /// 删除用户与停用用户这两条既有路径不得回退。
    /// </summary>
    [Theory]
    [InlineData("DeleteUserAsync")]
    [InlineData("UpdateUserStatusAsync")]
    public void UserAppService_ShouldRevokeBothSessionKinds(string methodName)
    {
        var method = Method<UserAppService>(methodName);

        Assert.True(ReachesRevokeByUserId(method), $"{methodName} 必须吊销该用户全部会话。");
        Assert.True(ReachesRevokeByImpersonatorUserId(method), $"{methodName} 必须吊销由该用户发起的模仿会话。");
    }

    private static bool ReachesRevokeByUserId(MethodInfo method)
    {
        return ReachesSessionRepository(method, nameof(IUserSessionRepository.RevokeByUserIdAsync));
    }

    private static bool ReachesRevokeByImpersonatorUserId(MethodInfo method)
    {
        return ReachesSessionRepository(method, nameof(IUserSessionRepository.RevokeByImpersonatorUserIdAsync));
    }

    private static bool ReachesSessionRepository(MethodInfo method, string calleeName)
    {
        return SaasAppIlCallGraph.Reaches(
            method,
            method.DeclaringType!,
            callee => callee.DeclaringType == typeof(IUserSessionRepository) && callee.Name == calleeName);
    }

    private static MethodInfo Method<TService>(string name)
    {
        return typeof(TService).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{typeof(TService).Name} 上找不到方法 {name}。");
    }
}

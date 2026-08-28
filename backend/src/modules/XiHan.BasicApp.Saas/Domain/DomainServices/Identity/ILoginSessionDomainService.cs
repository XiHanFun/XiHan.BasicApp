// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 登录会话领域服务
/// </summary>
public interface ILoginSessionDomainService
{
    /// <summary>
    /// 签发密码登录会话与 OAuth Token
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="security">用户安全配置</param>
    /// <param name="tenantId">租户标识</param>
    /// <param name="sessionBusinessId">业务会话标识</param>
    /// <param name="accessTokenJti">访问令牌 JTI</param>
    /// <param name="tokenResult">令牌结果</param>
    /// <param name="deviceId">设备标识</param>
    /// <param name="client">客户端信息</param>
    /// <param name="now">当前时间</param>
    /// <param name="initialLockReason">初始锁定原因（如默认密码登录的强制改密）；null 表示不锁定</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录会话签发结果</returns>
    Task<LoginSessionIssueResult> IssuePasswordLoginAsync(
        SysUser user,
        SysUserSecurity? security,
        long? tenantId,
        string sessionBusinessId,
        string accessTokenJti,
        JwtTokenResult tokenResult,
        string? deviceId,
        ClientInfo client,
        DateTimeOffset now,
        string? initialLockReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 切换租户：复用当前登录会话，轮换访问令牌并把会话行租户戳迁移到目标上下文
    /// </summary>
    /// <remarks>
    /// 切换租户是同一登录会话的上下文迁移，不是一次新登录：不新建会话行（避免设备列表每切一次多一台「设备」），
    /// 也不发布登录成功事件（避免每切一次误报「账号在新设备登录」）。
    /// </remarks>
    /// <param name="session">当前登录会话</param>
    /// <param name="targetTenantId">目标租户标识；空表示平台运维态（租户戳落 0）</param>
    /// <param name="accessTokenJti">新访问令牌 JTI</param>
    /// <param name="tokenResult">新令牌结果</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的用户会话</returns>
    Task<SysUserSession> SwitchTenantAsync(
        SysUserSession session,
        long? targetTenantId,
        string accessTokenJti,
        JwtTokenResult tokenResult,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 签发模仿登录会话：新建一条被模仿者身份的独立会话行并落令牌台账
    /// </summary>
    /// <remarks>
    /// 与密码登录的三点差异：不做同设备顶下线（模仿会话与发起人本体常在同一设备）、
    /// 不回写被模仿者的登录痕迹（LastLoginIp / LastSecurityCheckTime）、
    /// 过期时间取 <paramref name="lifetime"/> 而非刷新令牌寿命。
    /// </remarks>
    /// <param name="target">被模仿者</param>
    /// <param name="originSession">发起人的当前会话</param>
    /// <param name="impersonatorUserId">模仿者用户标识</param>
    /// <param name="impersonatorUserName">模仿者用户名</param>
    /// <param name="impersonatorTenantId">模仿者发起时所处租户</param>
    /// <param name="sessionBusinessId">模仿会话的业务标识</param>
    /// <param name="accessTokenJti">访问令牌 JTI</param>
    /// <param name="tokenResult">令牌结果</param>
    /// <param name="reason">模仿事由</param>
    /// <param name="lifetime">模仿会话存活时长</param>
    /// <param name="client">客户端信息</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模仿会话</returns>
    Task<SysUserSession> IssueImpersonationAsync(
        SysUser target,
        SysUserSession originSession,
        long impersonatorUserId,
        string? impersonatorUserName,
        long? impersonatorTenantId,
        string sessionBusinessId,
        string accessTokenJti,
        JwtTokenResult tokenResult,
        string? reason,
        TimeSpan lifetime,
        ClientInfo client,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 吊销模仿会话并撤销其关联 OAuth Token
    /// </summary>
    /// <param name="impersonationSession">模仿会话</param>
    /// <param name="reason">吊销原因</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已吊销的模仿会话</returns>
    Task<SysUserSession> RevokeImpersonationAsync(
        SysUserSession impersonationSession,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 退出当前登录会话并撤销关联 OAuth Token
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="sessionBusinessId">业务会话标识</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已退出的用户会话，不存在时返回空</returns>
    Task<SysUserSession?> LogoutAsync(
        long userId,
        string sessionBusinessId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

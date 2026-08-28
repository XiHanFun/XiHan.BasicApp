// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Extensions;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 认证应用服务：模仿登录
/// </summary>
public sealed partial class AuthAppService
{
    /// <summary>
    /// 模仿会话被吊销时写入的原因
    /// </summary>
    private const string ImpersonationRevokeReason = "结束模仿登录";

    /// <summary>
    /// 发起模仿登录：以目标用户身份签发一枚新令牌并新建独立的模仿会话
    /// </summary>
    /// <remarks>
    /// 不是一次目标用户的新登录：不回写其登录痕迹，也不发布登录成功事件。
    /// 令牌里的身份、角色与租户全部来自目标用户在目标租户的实时授权快照，
    /// 发起人仅以 <c>impersonator_*</c> 声明随行。
    /// </remarks>
    /// <param name="input">模仿登录参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模仿态的登录令牌</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Impersonation.Start)]
    public async Task<LoginTokenDto> StartImpersonationAsync(StartImpersonationRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var operatorUserId = _currentUser.UserId ?? throw new UserFriendlyException("当前用户未登录。");
        var operatorUserName = _currentUser.UserName;
        var operatorTenantId = _currentUser.TenantId;
        var operatorTenantName = _currentTenant.Name;
        var now = DateTimeOffset.UtcNow;

        var plan = await AuthorizeImpersonationOrAuditAsync(operatorUserId, operatorUserName, operatorTenantId, input, now, cancellationToken);

        // 退出模仿要靠发起人的当前会话找回原身份，它必须此刻仍然有效
        var originSession = await GetCurrentSessionOrThrowAsync(cancellationToken);
        if (originSession.Status != SessionStatus.Active)
        {
            throw new UserFriendlyException("会话已失效，请重新登录。");
        }

        if (originSession.ImpersonatorUserId.HasValue)
        {
            throw new UserFriendlyException("当前已处于模仿状态，不能再次发起模仿。");
        }

        // 一条原会话同时只挂一条模仿会话：多挂的那些在「结束模仿」时吊销不到，会滞留到过期
        var activeImpersonations = await _userSessionRepository.GetListAsync(
            session => session.ImpersonatorSessionId == originSession.UserSessionId && session.Status == SessionStatus.Active,
            cancellationToken);
        if (activeImpersonations.Count > 0)
        {
            throw new UserFriendlyException("当前已有进行中的模仿会话，请先结束。");
        }

        var lifetime = await ResolveImpersonationLifetimeAsync(cancellationToken);
        var sessionBusinessId = Guid.NewGuid().ToString("N");
        var accessTokenJti = Guid.NewGuid().ToString("N");
        var client = _clientInfoProvider.GetCurrent();

        using var tenantScope = _currentTenant.Change(plan.TargetTenantId, plan.TargetTenantName);

        var targetSnapshot = await _authorizationSnapshotQueryService.BuildAsync(plan.Target.BasicId, now, cancellationToken);
        IReadOnlyCollection<string> tokenPermissions = targetSnapshot.Permissions.Contains("*") ? ["*"] : [];
        var tokenIssue = _authTokenIssueService.IssueAccessToken(
            new AuthAccessTokenIssueCommand(
                plan.Target,
                plan.TargetTenantId,
                sessionBusinessId,
                accessTokenJti,
                targetSnapshot.Roles,
                tokenPermissions,
                originSession.DeviceId,
                operatorUserId,
                operatorUserName,
                operatorTenantId,
                operatorTenantName));

        _ = await _loginSessionDomainService.IssueImpersonationAsync(
            plan.Target,
            originSession,
            operatorUserId,
            operatorUserName,
            operatorTenantId,
            sessionBusinessId,
            accessTokenJti,
            tokenIssue.TokenResult,
            input.Reason,
            lifetime,
            client,
            now,
            cancellationToken);

        await _cacheInvalidator.InvalidateSessionStateAsync(sessionBusinessId, cancellationToken);

        await PublishSecurityAuditAsync(
            plan.TargetTenantId,
            plan.Target.BasicId,
            plan.Target.UserName,
            LoginResult.ImpersonationStarted,
            $"管理员 {operatorUserName}({operatorUserId}) 以本账号身份登录，有效期 {(int)lifetime.TotalMinutes} 分钟");

        await NotifyImpersonationTargetAsync(plan.Target, operatorUserName, operatorUserId, lifetime, cancellationToken);

        return tokenIssue.Token;
    }

    /// <summary>
    /// 结束模仿登录：吊销模仿会话，并在发起人的原会话上重新签发其本人身份的令牌
    /// </summary>
    /// <remarks>
    /// 本端点不挂权限码：模仿态下的当前主体是被模仿者，不持有模仿类权限。
    /// 准入靠令牌里的 <c>impersonator_userid</c> 声明与模仿会话行上的 <c>ImpersonatorUserId</c> 两者相等。
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发起人本人身份的登录令牌</returns>
    [UnitOfWork(true)]
    public async Task<LoginTokenDto> StopImpersonationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var impersonatorUserId = _currentUser.FindImpersonatorUserId()
            ?? throw new UserFriendlyException("当前不处于模仿状态。");
        var impersonatedUserId = _currentUser.UserId ?? throw new UserFriendlyException("当前用户未登录。");
        var impersonatedUserName = _currentUser.UserName;
        var impersonatedTenantId = _currentUser.TenantId;
        var now = DateTimeOffset.UtcNow;

        using var impersonationScope = _currentTenant.Change(impersonatedTenantId, impersonatedTenantId?.ToString());

        var impersonationSession = await GetCurrentSessionOrThrowAsync(cancellationToken);
        if (impersonationSession.ImpersonatorUserId != impersonatorUserId ||
            string.IsNullOrWhiteSpace(impersonationSession.ImpersonatorSessionId))
        {
            LogImpersonationDenied(
                "结束模仿被拒绝：令牌声明与会话记录的模仿者不一致",
                impersonatorUserId,
                impersonatedUserId);
            throw new UserFriendlyException("模仿状态校验失败，请重新登录。");
        }

        // 全部前置校验先做完再落写：方法带 [UnitOfWork(true)]，中途抛异常会把已写的吊销一并回滚，
        // 先写后校验会读起来像「模仿态一定结束」而实际什么都没发生。
        // 校验不过时模仿会话保持原状，由其短过期或一次登出收场。
        var originSession = await _userSessionRepository.GetByUserSessionIdAsync(
            impersonationSession.ImpersonatorSessionId,
            cancellationToken);
        if (originSession is null ||
            originSession.UserId != impersonatorUserId ||
            originSession.Status != SessionStatus.Active ||
            (originSession.ExpirationTime.HasValue && originSession.ExpirationTime.Value <= now))
        {
            throw new UserFriendlyException("原会话已失效，请重新登录。");
        }

        var operatorUser = await _userRepository.GetByIdIgnoreTenantAsync(impersonatorUserId, cancellationToken)
            ?? throw new UserFriendlyException("原账号不存在，请重新登录。");
        if (operatorUser.Status != EnableStatus.Enabled)
        {
            throw new UserFriendlyException("原账号已被禁用，请重新登录。");
        }

        var originTenantId = impersonationSession.ImpersonatorTenantId is > 0
            ? impersonationSession.ImpersonatorTenantId
            : null;
        string? originTenantName = null;
        if (originTenantId.HasValue)
        {
            var originTenant = await _authContextQueryService.FindAvailableLoginTenantAsync(originTenantId.Value, now, cancellationToken)
                ?? throw new UserFriendlyException("原租户当前不可用，请重新登录。");
            originTenantName = originTenant.TenantName;
        }

        _ = await _loginSessionDomainService.RevokeImpersonationAsync(
            impersonationSession,
            ImpersonationRevokeReason,
            now,
            cancellationToken);
        await _cacheInvalidator.InvalidateSessionStateAsync(impersonationSession.UserSessionId, cancellationToken);

        await PublishSecurityAuditAsync(
            impersonatedTenantId,
            impersonatedUserId,
            impersonatedUserName,
            LoginResult.ImpersonationEnded,
            $"管理员 {impersonationSession.ImpersonatorUserName ?? impersonatorUserId.ToString()}({impersonatorUserId}) 结束了对本账号的模仿");

        using var originScope = _currentTenant.Change(originTenantId, originTenantName);

        var operatorSnapshot = await _authorizationSnapshotQueryService.BuildAsync(operatorUser.BasicId, now, cancellationToken);
        IReadOnlyCollection<string> operatorTokenPermissions = operatorSnapshot.Permissions.Contains("*") ? ["*"] : [];
        var accessTokenJti = Guid.NewGuid().ToString("N");

        // 新令牌不带任何 impersonator_* 声明：声明集合是重建的，不是在旧主体上做删除
        var tokenIssue = _authTokenIssueService.IssueAccessToken(
            new AuthAccessTokenIssueCommand(
                operatorUser,
                originTenantId,
                originSession.UserSessionId,
                accessTokenJti,
                operatorSnapshot.Roles,
                operatorTokenPermissions,
                originSession.DeviceId));

        _ = await _loginSessionDomainService.SwitchTenantAsync(
            originSession,
            originTenantId,
            accessTokenJti,
            tokenIssue.TokenResult,
            now,
            cancellationToken);
        await _cacheInvalidator.InvalidateSessionStateAsync(originSession.UserSessionId, cancellationToken);

        return tokenIssue.Token;
    }

    /// <summary>
    /// 走准入判定，被拒时先落一条失败审计再抛出
    /// </summary>
    private async Task<ImpersonationPlan> AuthorizeImpersonationOrAuditAsync(
        long operatorUserId,
        string? operatorUserName,
        long? operatorTenantId,
        StartImpersonationRequestDto input,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _impersonationPolicyService.AuthorizeStartAsync(
                operatorUserId,
                operatorTenantId,
                _currentUser.IsImpersonating(),
                input.TargetUserId,
                input.TenantId,
                now,
                cancellationToken);
        }
        catch (UserFriendlyException exception)
        {
            LogImpersonationDenied(
                $"发起模仿登录被拒绝：{exception.Message}",
                operatorUserId,
                input.TargetUserId);
            throw;
        }
    }

    /// <summary>
    /// 记录一次模仿相关的拒绝
    /// </summary>
    /// <remarks>
    /// 走应用日志而不是审计事件：拒绝路径以抛异常收尾，而两个端点都带 [UnitOfWork(true)]，
    /// 排在工作单元里的领域事件在回滚时整个丢弃，落不到登录日志。
    /// </remarks>
    /// <param name="message">拒绝原因</param>
    /// <param name="operatorUserId">发起人用户标识</param>
    /// <param name="targetUserId">目标用户标识</param>
    private void LogImpersonationDenied(string message, long operatorUserId, long targetUserId)
    {
        _logger.LogWarning(
            "模仿登录拒绝：{Message}（发起人 {OperatorUserId}，目标 {TargetUserId}，链路 {TraceId}）",
            message,
            operatorUserId,
            targetUserId,
            _traceIdProvider.GetCurrentTraceId());
    }

    /// <summary>
    /// 读取模仿会话存活时长配置，越界回落到上下限
    /// </summary>
    private async Task<TimeSpan> ResolveImpersonationLifetimeAsync(CancellationToken cancellationToken)
    {
        var minutes = await _saasConfigurationService.GetInt32Async(
            SaasConfigKeys.Auth.ImpersonationSessionMinutes,
            ImpersonationDefaults.DefaultSessionMinutes,
            cancellationToken);
        return ImpersonationDefaults.NormalizeSessionLifetime(minutes);
    }

    /// <summary>
    /// 按配置向被模仿者投递安全通知，失败不阻断模仿流程
    /// </summary>
    private async Task NotifyImpersonationTargetAsync(
        SysUser target,
        string? operatorUserName,
        long operatorUserId,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        var notifyEnabled = await _saasConfigurationService.GetBooleanAsync(
            SaasConfigKeys.Auth.ImpersonationNotifyTarget,
            true,
            cancellationToken);
        if (!notifyEnabled)
        {
            return;
        }

        try
        {
            _ = await _userNotificationDispatchService.DispatchToUserAsync(
                target.BasicId,
                "管理员以你的身份登录",
                $"管理员 {operatorUserName ?? operatorUserId.ToString()} 已以你的账号身份登录，用于排查问题，有效期 {(int)lifetime.TotalMinutes} 分钟。期间的操作会记录在登录日志里。",
                NotificationType.Security,
                businessType: "auth.impersonation",
                businessId: operatorUserId,
                cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "模仿登录通知投递失败：目标用户 {TargetUserId}", target.BasicId);
        }
    }
}

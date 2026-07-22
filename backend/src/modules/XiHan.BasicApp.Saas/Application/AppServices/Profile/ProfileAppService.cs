// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 当前用户个人中心应用服务（账号资料关注点）。
/// 其余关注点见 <c>ProfileAppService.Security.cs</c> / <c>ProfileAppService.Verification.cs</c> / <c>ProfileAppService.Session.cs</c>。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "个人中心")]
public sealed partial class ProfileAppService
    : SaasApplicationService, IProfileAppService
{
    private readonly ICurrentUser _currentUser;

    private readonly ILocalEventBus _localEventBus;

    private readonly IUserNotificationDispatchService _notificationDispatchService;

    private readonly IProfileDomainService _profileDomainService;

    private readonly IProfileQueryService _profileQueryService;

    private readonly IProfileVerificationService _profileVerificationService;

    private readonly IUserNotificationPreferenceRepository _notificationPreferenceRepository;

    private readonly IUserApiCredentialRepository _userApiCredentialRepository;

    private readonly IUserApiCredentialSecretProtector _apiCredentialSecretProtector;

    private readonly IClientInfoProvider _clientInfoProvider;

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileAppService(
        IProfileDomainService profileDomainService,
        IProfileQueryService profileQueryService,
        IProfileVerificationService profileVerificationService,
        ILocalEventBus localEventBus,
        IUserNotificationDispatchService notificationDispatchService,
        IUserNotificationPreferenceRepository notificationPreferenceRepository,
        IUserApiCredentialRepository userApiCredentialRepository,
        IUserApiCredentialSecretProtector apiCredentialSecretProtector,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _profileDomainService = profileDomainService;
        _profileQueryService = profileQueryService;
        _profileVerificationService = profileVerificationService;
        _localEventBus = localEventBus;
        _notificationDispatchService = notificationDispatchService;
        _notificationPreferenceRepository = notificationPreferenceRepository;
        _userApiCredentialRepository = userApiCredentialRepository;
        _apiCredentialSecretProtector = apiCredentialSecretProtector;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async Task<ProfileNotificationPreferenceDto> GetNotificationPreferenceAsync(CancellationToken cancellationToken = default)
    {
        return await _profileQueryService.GetNotificationPreferenceAsync(GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task<ProfileNotificationPreferenceDto> UpdateNotificationPreferenceAsync(ProfileNotificationPreferenceDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var preference = await _notificationPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        if (preference is null)
        {
            // 惰性创建
            preference = new SysUserNotificationPreference { UserId = userId };
            ApplyPreference(preference, input);
            await _notificationPreferenceRepository.AddAsync(preference, cancellationToken);
        }
        else
        {
            ApplyPreference(preference, input);
            await _notificationPreferenceRepository.UpdateAsync(preference, cancellationToken);
        }

        return ProfileQueryService.ToPreferenceDto(preference);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task ChangeUserNameAsync(ProfileChangeUserNameDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.ChangeUserNameAsync(
            ProfileApplicationMapper.ToChangeUserNameCommand(input, currentUserId),
            cancellationToken);

        await _notificationDispatchService.DispatchToUserAsync(
            result.User.BasicId,
            "用户名已修改",
            $"您的账号用户名已修改为 {result.User.UserName}。",
            NotificationType.Security,
            "profile.username.changed",
            result.User.BasicId,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task DeactivateAccountAsync(ProfilePasswordConfirmDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.DeactivateAccountAsync(
            ProfileApplicationMapper.ToPasswordConfirmCommand(input, currentUserId, _currentUser.UserId),
            cancellationToken);
        await PublishSessionRevokedEventsAsync(result.DomainEvents, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// 显式锁定 POST /Profile/DeleteAccount：按动词剥离约定本方法会推断成 DELETE /Profile/Account，
    /// 带密码确认 body 的 DELETE 不符合习惯，前端也按 POST 完整方法名调用。
    /// </remarks>
    [HttpPost]
    [DynamicApi(Name = "DeleteAccount")]
    [UnitOfWork(true)]
    public async Task DeleteAccountAsync(ProfilePasswordConfirmDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.DeleteAccountAsync(
            ProfileApplicationMapper.ToPasswordConfirmCommand(input, currentUserId, _currentUser.UserId),
            cancellationToken);
        await PublishSessionRevokedEventsAsync(result.DomainEvents, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileActivityDto> GetActivityAsync(CancellationToken cancellationToken = default)
    {
        return await _profileQueryService.GetActivityAsync(GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<ProfileExternalLoginDto>> GetLinkedAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await _profileQueryService.GetLinkedAccountsAsync(GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        return await _profileQueryService.GetProfileAsync(GetCurrentUserIdOrThrow(), _currentUser.TenantId, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task UnlinkAccountAsync(ProfileUnlinkAccountDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _profileDomainService.UnlinkAccountAsync(
            ProfileApplicationMapper.ToUnlinkAccountCommand(input, GetCurrentUserIdOrThrow()),
            cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task<UserProfileDto> UpdateProfileAsync(ProfileUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        await _profileDomainService.UpdateProfileAsync(
            ProfileApplicationMapper.ToUpdateCommand(input, userId),
            cancellationToken);

        return await _profileQueryService.GetProfileAsync(userId, _currentUser.TenantId, cancellationToken);
    }

    /// <summary>
    /// 将 DTO 写入偏好实体
    /// </summary>
    private static void ApplyPreference(SysUserNotificationPreference preference, ProfileNotificationPreferenceDto input)
    {
        preference.ChannelInApp = input.ChannelInApp;
        preference.ChannelEmail = input.ChannelEmail;
        preference.ChannelSms = input.ChannelSms;
        preference.ChannelPush = input.ChannelPush;
        preference.ChannelBot = input.ChannelBot;
        preference.TypeAnnouncement = input.TypeAnnouncement;
        preference.TypeTask = input.TypeTask;
        preference.TypeApproval = input.TypeApproval;
        preference.TypeSecurity = input.TypeSecurity;
        preference.TypeMarketing = input.TypeMarketing;
    }

    private static TwoFactorMethod ToTwoFactorMethod(int method)
    {
        return method switch
        {
            (int)TwoFactorMethod.Totp => TwoFactorMethod.Totp,
            (int)TwoFactorMethod.Email => TwoFactorMethod.Email,
            (int)TwoFactorMethod.Phone => TwoFactorMethod.Phone,
            _ => throw new ArgumentOutOfRangeException(nameof(method), "双因素方式无效。")
        };
    }

    /// <summary>
    /// 发布认证安全审计事件（密码修改/绑定解绑MFA 等），统一落登录日志
    /// </summary>
    private async Task PublishSecurityAuditAsync(LoginResult auditResult, string message)
    {
        var client = _clientInfoProvider.GetCurrent();
        await _localEventBus.PublishAsync(
            new AuthSecurityAuditDomainEvent(
                _currentUser.TenantId,
                _currentUser.UserId,
                _currentUser.UserName,
                auditResult,
                message,
                DateTimeOffset.UtcNow,
                _httpContextAccessor.HttpContext?.TraceIdentifier,
                client.IpAddress,
                client.UserAgent));
    }

    private string? GetCurrentSessionId()
    {
        return _currentUser.FindClaim(XiHanClaimTypes.SessionId)?.Value;
    }

    private long GetCurrentUserIdOrThrow()
    {
        return _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
    }

    private async Task PublishSessionRevokedEventsAsync(
        IReadOnlyList<UserSessionRevokedDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _localEventBus.PublishAsync(domainEvent);
        }
    }
}

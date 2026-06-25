#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileAppService.Developer
// Guid:e1c6a4d8-7b25-4f93-a0e7-5d2f8b3c9a61
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Uow.Attributes;
using XiHan.Framework.Utils.Security;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 当前用户个人中心应用服务（开发者关注点：个人 API 凭证）。
/// </summary>
/// <remarks>
/// AppKey/AppSecret 以加密安全随机数生成；Secret 仅存哈希（与账号密码同栈 IPasswordHasher），
/// 明文仅创建/滚动时返回一次。凭证数量按用户限额，全部操作自限定当前用户并发安全通知。
/// </remarks>
public sealed partial class ProfileAppService
{
    /// <summary>
    /// 每用户 API 凭证数量上限
    /// </summary>
    private const int ApiCredentialLimit = 5;

    private const string ApiCredentialBusinessType = "profile.api-credential";

    /// <inheritdoc />
    public async Task<List<ProfileApiCredentialDto>> GetApiCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var credentials = await _userApiCredentialRepository.GetListByUserIdAsync(GetCurrentUserIdOrThrow(), cancellationToken);
        return [.. credentials.Select(ToApiCredentialDto)];
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task<ProfileApiCredentialSecretDto> CreateApiCredentialAsync(ProfileApiCredentialCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var existing = await _userApiCredentialRepository.GetListByUserIdAsync(userId, cancellationToken);
        if (existing.Count >= ApiCredentialLimit)
        {
            throw new UserFriendlyException($"API 凭证最多 {ApiCredentialLimit} 个，请删除不再使用的凭证后重试。");
        }

        var name = string.IsNullOrWhiteSpace(input.CredentialName) ? "默认凭证" : input.CredentialName.Trim();
        if (name.Length > 100)
        {
            throw new UserFriendlyException("凭证名称不能超过 100 个字符。");
        }

        var appKey = await GenerateUniqueAppKeyAsync(cancellationToken);
        var secret = GenerateApiSecret();

        var credential = new SysUserApiCredential
        {
            UserId = userId,
            CredentialName = name,
            AppKey = appKey,
            SecretHash = _passwordHasher.HashPassword(secret),
            Status = EnableStatus.Enabled
        };
        credential = await _userApiCredentialRepository.AddAsync(credential, cancellationToken);

        await NotifyApiCredentialChangeAsync(userId, "API 凭证已创建", $"凭证「{name}」（{appKey}）已创建。如非本人操作，请立即删除该凭证并修改密码。", credential.BasicId, cancellationToken);

        return new ProfileApiCredentialSecretDto
        {
            BasicId = credential.BasicId,
            AppKey = credential.AppKey,
            AppSecret = secret
        };
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task<ProfileApiCredentialSecretDto> RotateApiCredentialSecretAsync(ProfileApiCredentialIdDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var credential = await GetOwnedApiCredentialOrThrowAsync(userId, input.BasicId, cancellationToken);

        var secret = GenerateApiSecret();
        credential.SecretHash = _passwordHasher.HashPassword(secret);
        _ = await _userApiCredentialRepository.UpdateAsync(credential, cancellationToken);

        await NotifyApiCredentialChangeAsync(userId, "API 凭证密钥已滚动", $"凭证「{credential.CredentialName}」（{credential.AppKey}）的密钥已重置，旧密钥立即失效。", credential.BasicId, cancellationToken);

        return new ProfileApiCredentialSecretDto
        {
            BasicId = credential.BasicId,
            AppKey = credential.AppKey,
            AppSecret = secret
        };
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task<ProfileApiCredentialDto> UpdateApiCredentialStatusAsync(ProfileApiCredentialStatusDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Enum.IsDefined(input.Status))
        {
            throw new UserFriendlyException("凭证状态无效。");
        }

        var userId = GetCurrentUserIdOrThrow();
        var credential = await GetOwnedApiCredentialOrThrowAsync(userId, input.BasicId, cancellationToken);

        credential.Status = input.Status;
        _ = await _userApiCredentialRepository.UpdateAsync(credential, cancellationToken);

        return ToApiCredentialDto(credential);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task DeleteApiCredentialAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var credential = await GetOwnedApiCredentialOrThrowAsync(userId, id, cancellationToken);

        _ = await _userApiCredentialRepository.DeleteAsync(credential, cancellationToken);

        await NotifyApiCredentialChangeAsync(userId, "API 凭证已删除", $"凭证「{credential.CredentialName}」（{credential.AppKey}）已删除，该 AppKey 立即不可用。", credential.BasicId, cancellationToken);
    }

    /// <summary>
    /// 生成应用密钥明文（加密安全随机数，前缀 sk_）
    /// </summary>
    private static string GenerateApiSecret()
    {
        return $"sk_{RandomCoder.GetNumberOrLetter(43)}";
    }

    private static ProfileApiCredentialDto ToApiCredentialDto(SysUserApiCredential credential)
    {
        return new ProfileApiCredentialDto
        {
            BasicId = credential.BasicId,
            CredentialName = credential.CredentialName,
            AppKey = credential.AppKey,
            Status = credential.Status,
            LastUsedTime = credential.LastUsedTime,
            ExpirationTime = credential.ExpirationTime,
            CreatedTime = credential.CreatedTime
        };
    }

    /// <summary>
    /// 校验凭证归属并返回（仅允许操作本人凭证）
    /// </summary>
    private async Task<SysUserApiCredential> GetOwnedApiCredentialOrThrowAsync(long userId, long credentialId, CancellationToken cancellationToken)
    {
        if (credentialId <= 0)
        {
            throw new UserFriendlyException(new ResourceLocalizableString("Errors", "Profile.Credential.InvalidId"), "凭证主键无效。");
        }

        var credential = await _userApiCredentialRepository.GetByIdAsync(credentialId, cancellationToken);
        if (credential is null || credential.UserId != userId)
        {
            throw new UserFriendlyException(new ResourceLocalizableString("Errors", "Profile.Credential.NotFoundOrNoPermission"), "凭证不存在或无权操作。");
        }

        return credential;
    }

    /// <summary>
    /// 生成全局唯一应用键（加密安全随机数，前缀 ak_）
    /// </summary>
    private async Task<string> GenerateUniqueAppKeyAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var appKey = $"ak_{RandomCoder.GetNumberOrLetter(24)}";
            if (await _userApiCredentialRepository.GetByAppKeyAsync(appKey, cancellationToken) is null)
            {
                return appKey;
            }
        }

        throw new InvalidOperationException("应用键生成冲突，请重试。");
    }

    /// <summary>
    /// 凭证变更安全通知（创建/滚动/删除）
    /// </summary>
    private async Task NotifyApiCredentialChangeAsync(long userId, string title, string content, long credentialId, CancellationToken cancellationToken)
    {
        await _notificationDispatchService.DispatchToUserAsync(
            userId,
            title,
            content,
            NotificationType.Business,
            ApiCredentialBusinessType,
            credentialId,
            link: "/workbench/profile",
            icon: "lucide:key-round",
            cancellationToken: cancellationToken);
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 个人中心应用服务（会话与登录日志关注点）。
/// </summary>
public sealed partial class ProfileAppService
{
    /// <inheritdoc />
    public async Task<ProfileLoginLogPageDto> GetLoginLogsAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return await _profileQueryService.GetLoginLogsAsync(GetCurrentUserIdOrThrow(), page, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<ProfileSessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _profileQueryService.GetSessionsAsync(GetCurrentUserIdOrThrow(), GetCurrentSessionId(), cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task RevokeOtherSessionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.RevokeOtherSessionsAsync(
            ProfileApplicationMapper.ToOtherSessionsRevokeCommand(currentUserId, GetCurrentSessionId(), _currentUser.UserId),
            cancellationToken);
        await PublishSessionRevokedEventsAsync(result.DomainEvents, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task RevokeSessionAsync(ProfileSessionRevokeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.RevokeSessionAsync(
            ProfileApplicationMapper.ToSessionRevokeCommand(input, currentUserId, GetCurrentSessionId(), _currentUser.UserId),
            cancellationToken);
        await PublishSessionRevokedEventsAsync(result.DomainEvents, cancellationToken);
    }
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PasswordHistoryDomainService
// Guid:8a3d6b29-7c5e-4f0b-ad44-2e3f4a5b6c7d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 密码历史领域服务实现
/// </summary>
public sealed class PasswordHistoryDomainService : IPasswordHistoryDomainService
{
    private readonly IPasswordHistoryRepository _passwordHistoryRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly PasswordPolicyOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PasswordHistoryDomainService(
        IPasswordHistoryRepository passwordHistoryRepository,
        IPasswordHasher passwordHasher,
        IOptions<PasswordPolicyOptions> options)
    {
        _passwordHistoryRepository = passwordHistoryRepository ?? throw new ArgumentNullException(nameof(passwordHistoryRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task EnsureNotReusedAsync(long userId, string newPlainPassword, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var count = _options.PasswordHistoryCount;
        if (userId <= 0 || count <= 0 || string.IsNullOrEmpty(newPlainPassword))
        {
            return;
        }

        var history = await _passwordHistoryRepository.GetRecentByUserIdAsync(userId, count, cancellationToken);
        foreach (var entry in history)
        {
            // PBKDF2 加盐哈希：同一明文每次哈希不同，必须用 VerifyPassword(历史哈希, 新明文) 逐条比对
            if (_passwordHasher.VerifyPassword(entry.PasswordHash, newPlainPassword))
            {
                throw new InvalidOperationException($"新密码不能与最近 {count} 次使用过的密码相同。");
            }
        }
    }

    /// <inheritdoc />
    public async Task RecordAsync(long userId, string newPasswordHash, DateTimeOffset changedTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId <= 0 || string.IsNullOrEmpty(newPasswordHash))
        {
            return;
        }

        var entry = new SysPasswordHistory
        {
            UserId = userId,
            PasswordHash = newPasswordHash,
            ChangedTime = changedTime
        };

        _ = await _passwordHistoryRepository.AddAsync(entry, cancellationToken);
    }
}

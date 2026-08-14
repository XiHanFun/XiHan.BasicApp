// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

    /// <summary>
    /// 校验新密码未与最近 N 次历史密码重复，重复则抛出 <see cref="InvalidOperationException"/>
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="newPlainPassword">新密码明文</param>
    /// <param name="cancellationToken">取消令牌</param>
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

    /// <summary>
    /// 记录一条密码历史（密码变更成功后写入新密码哈希）
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="newPasswordHash">新密码哈希</param>
    /// <param name="changedTime">变更时间</param>
    /// <param name="cancellationToken">取消令牌</param>
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

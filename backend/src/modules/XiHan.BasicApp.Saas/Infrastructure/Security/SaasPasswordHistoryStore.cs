// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Security.Services;

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// SaaS 密码历史存储实现，桥接框架 <see cref="IPasswordHistoryStore"/> 与领域仓储 IPasswordHistoryRepository
/// </summary>
public sealed class SaasPasswordHistoryStore : IPasswordHistoryStore
{
    private readonly IPasswordHistoryRepository _passwordHistoryRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasPasswordHistoryStore(IPasswordHistoryRepository passwordHistoryRepository)
    {
        _passwordHistoryRepository = passwordHistoryRepository ?? throw new ArgumentNullException(nameof(passwordHistoryRepository));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetRecentPasswordHashesAsync(long userId, int count, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (userId <= 0 || count <= 0)
        {
            return [];
        }

        var history = await _passwordHistoryRepository.GetRecentByUserIdAsync(userId, count, ct);

        return history
            .Select(h => h.PasswordHash)
            .ToList()
            .AsReadOnly();
    }
}

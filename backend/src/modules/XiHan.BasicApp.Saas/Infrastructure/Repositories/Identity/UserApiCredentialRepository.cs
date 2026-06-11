#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserApiCredentialRepository
// Guid:c3a8e5f1-9d27-4b64-8f0a-2c6b9e4d7a38
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户 API 凭证仓储实现
/// </summary>
public sealed class UserApiCredentialRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserApiCredential>(clientResolver), IUserApiCredentialRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysUserApiCredential>> GetListByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(credential => credential.UserId == userId)
            .OrderByDescending(credential => credential.CreatedTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysUserApiCredential?> GetByAppKeyAsync(string appKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appKey);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(credential => credential.AppKey == appKey)
            .FirstAsync(cancellationToken);
    }
}

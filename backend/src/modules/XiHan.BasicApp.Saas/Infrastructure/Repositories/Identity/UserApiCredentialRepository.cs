// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

        // AppKey 全局唯一，跨租户查询：开放接口网关鉴权在请求管道早期执行、无租户上下文；
        // 创建时的唯一性检查也需跨租户，避免不同租户生成相同 AppKey
        return await CreateNoTenantQueryable()
            .Where(credential => credential.AppKey == appKey)
            .FirstAsync(cancellationToken);
    }
}

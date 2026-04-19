#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacExternalLoginStore
// Guid:a1b2c3d4-5e6f-7890-abcd-ef1234567813
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.OAuth;

namespace XiHan.BasicApp.Saas.Infrastructure.Authentication;

/// <summary>
/// 第三方登录存储的数据库实现
/// </summary>
public class RbacExternalLoginStore : IExternalLoginStore
{
    private readonly IExternalLoginRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacExternalLoginStore(IExternalLoginRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<long?> FindUserIdAsync(string provider, string providerKey, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var record = await _repository.FindByProviderAsync(provider, providerKey, tenantId, cancellationToken);
        return record?.UserId;
    }

    /// <inheritdoc/>
    public async Task CreateAsync(long userId, ExternalLoginInfo info, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var entity = new SysExternalLogin
        {
            TenantId = tenantId ?? 0,
            UserId = userId,
            Provider = info.Provider,
            ProviderKey = info.ProviderKey,
            ProviderDisplayName = info.DisplayName,
            Email = info.Email,
            AvatarUrl = info.AvatarUrl,
            LastLoginTime = DateTimeOffset.UtcNow
        };
        await _repository.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(long userId, string provider, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(userId, provider, cancellationToken);
    }
}

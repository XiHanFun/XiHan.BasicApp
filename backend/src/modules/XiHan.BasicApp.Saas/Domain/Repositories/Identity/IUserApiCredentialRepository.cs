// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户 API 凭证仓储接口
/// </summary>
public interface IUserApiCredentialRepository : ISaasRepository<SysUserApiCredential>
{
    /// <summary>
    /// 获取用户全部凭证（创建时间倒序）
    /// </summary>
    Task<IReadOnlyList<SysUserApiCredential>> GetListByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按应用键查询凭证（跨租户；开放接口鉴权入口，AppKey 全局唯一）
    /// </summary>
    Task<SysUserApiCredential?> GetByAppKeyAsync(string appKey, CancellationToken cancellationToken = default);
}

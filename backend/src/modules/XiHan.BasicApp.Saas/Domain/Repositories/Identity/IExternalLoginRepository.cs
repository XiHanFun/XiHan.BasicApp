// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 外部登录仓储接口
/// </summary>
public interface IExternalLoginRepository : ISaasRepository<SysExternalLogin>
{
    /// <summary>
    /// 根据提供商和提供商用户标识获取外部登录
    /// </summary>
    Task<SysExternalLogin?> GetByProviderAndKeyAsync(string provider, string providerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 跨租户获取用户全部第三方账号绑定（绑定行带绑定时所在租户的戳，个人中心自助场景按用户取全）
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>绑定列表</returns>
    Task<IReadOnlyList<SysExternalLogin>> GetListByUserIdIgnoreTenantAsync(long userId, CancellationToken cancellationToken = default);
}

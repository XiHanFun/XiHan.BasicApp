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
}

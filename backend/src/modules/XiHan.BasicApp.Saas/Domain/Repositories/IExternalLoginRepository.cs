#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IExternalLoginRepository
// Guid:a1b2c3d4-5e6f-7890-abcd-ef1234567811
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 第三方登录仓储接口
/// </summary>
public interface IExternalLoginRepository
{
    /// <summary>
    /// 根据提供商和提供商Key查找绑定记录
    /// </summary>
    Task<SysExternalLogin?> FindByProviderAsync(string provider, string providerKey, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有第三方绑定
    /// </summary>
    Task<IReadOnlyList<SysExternalLogin>> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增绑定记录
    /// </summary>
    Task<SysExternalLogin> AddAsync(SysExternalLogin entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新绑定记录
    /// </summary>
    Task UpdateAsync(SysExternalLogin entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除绑定记录
    /// </summary>
    Task DeleteAsync(long userId, string provider, CancellationToken cancellationToken = default);
}

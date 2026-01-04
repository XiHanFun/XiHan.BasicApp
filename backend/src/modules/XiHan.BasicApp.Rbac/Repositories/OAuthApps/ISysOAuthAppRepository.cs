#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOAuthAppRepository
// Guid:adb2c3d4-e5f6-7890-abcd-ef123456789c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.OAuthApps;

/// <summary>
/// 系统OAuth应用仓储接口
/// </summary>
public interface ISysOAuthAppRepository : IRepositoryBase<SysOAuthApp, long>
{
    /// <summary>
    /// 根据客户端ID获取应用
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <returns></returns>
    Task<SysOAuthApp?> GetByClientIdAsync(string clientId);

    /// <summary>
    /// 根据应用名称获取应用
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <returns></returns>
    Task<SysOAuthApp?> GetByAppNameAsync(string appName);

    /// <summary>
    /// 检查客户端ID是否存在
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="excludeId">排除的应用ID</param>
    /// <returns></returns>
    Task<bool> ExistsByClientIdAsync(string clientId, long? excludeId = null);
}

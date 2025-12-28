#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOAuthAppService
// Guid:d1e2f3g4-h5i6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 18:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Services.OAuthApps.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.OAuthApps;

/// <summary>
/// 系统OAuth应用服务接口
/// </summary>
public interface ISysOAuthAppService : ICrudApplicationService<OAuthAppDto, XiHanBasicAppIdType, CreateOAuthAppDto, UpdateOAuthAppDto>
{
    /// <summary>
    /// 根据客户端ID获取应用
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <returns></returns>
    Task<OAuthAppDto?> GetByClientIdAsync(string clientId);

    /// <summary>
    /// 根据应用名称获取应用
    /// </summary>
    /// <param name="appName">应用名称</param>
    /// <returns></returns>
    Task<OAuthAppDto?> GetByAppNameAsync(string appName);

    /// <summary>
    /// 检查客户端ID是否存在
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="excludeId">排除的应用ID</param>
    /// <returns></returns>
    Task<bool> ExistsByClientIdAsync(string clientId, XiHanBasicAppIdType? excludeId = null);
}


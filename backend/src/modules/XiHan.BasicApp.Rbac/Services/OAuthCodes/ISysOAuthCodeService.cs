#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOAuthCodeService
// Guid:g1h2i3j4-k5l6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 18:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Services.OAuthCodes.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.OAuthCodes;

/// <summary>
/// 系统OAuth授权码服务接口
/// </summary>
public interface ISysOAuthCodeService : ICrudApplicationService<OAuthCodeDto, XiHanBasicAppIdType, CreateOAuthCodeDto, UpdateOAuthCodeDto>
{
    /// <summary>
    /// 根据授权码获取
    /// </summary>
    /// <param name="code">授权码</param>
    /// <returns></returns>
    Task<OAuthCodeDto?> GetByCodeAsync(string code);

    /// <summary>
    /// 根据客户端ID和用户ID获取授权码列表
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<OAuthCodeDto>> GetByClientAndUserAsync(string clientId, XiHanBasicAppIdType userId);

    /// <summary>
    /// 删除过期的授权码
    /// </summary>
    /// <returns></returns>
    Task<int> DeleteExpiredCodesAsync();
}


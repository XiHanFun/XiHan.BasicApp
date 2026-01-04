#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOAuthCodeRepository
// Guid:aeb2c3d4-e5f6-7890-abcd-ef123456789d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.OAuthCodes;

/// <summary>
/// 系统OAuth授权码仓储接口
/// </summary>
public interface ISysOAuthCodeRepository : IRepositoryBase<SysOAuthCode, long>
{
    /// <summary>
    /// 根据授权码获取
    /// </summary>
    /// <param name="code">授权码</param>
    /// <returns></returns>
    Task<SysOAuthCode?> GetByCodeAsync(string code);

    /// <summary>
    /// 根据客户端ID和用户ID获取授权码列表
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysOAuthCode>> GetByClientAndUserAsync(string clientId, long userId);

    /// <summary>
    /// 删除过期的授权码
    /// </summary>
    /// <returns></returns>
    Task<int> DeleteExpiredCodesAsync();
}

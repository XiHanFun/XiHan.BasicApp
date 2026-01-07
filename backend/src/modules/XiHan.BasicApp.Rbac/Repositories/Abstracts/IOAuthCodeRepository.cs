#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthCodeRepository
// Guid:f2a3b4c5-d6e7-4f5a-8b9c-1d2e3f4a5b6c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// OAuth授权码仓储接口
/// </summary>
public interface IOAuthCodeRepository : IRepositoryBase<SysOAuthCode, long>
{
    /// <summary>
    /// 根据授权码查询
    /// </summary>
    /// <param name="code">授权码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>授权码实体</returns>
    Task<SysOAuthCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证授权码并删除（授权码一次性使用）
    /// </summary>
    /// <param name="code">授权码</param>
    /// <param name="clientId">客户端ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>授权码实体</returns>
    Task<SysOAuthCode?> ValidateAndRemoveAsync(string code, string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期授权码
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理数量</returns>
    Task<int> CleanExpiredCodesAsync(DateTimeOffset currentTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的授权码列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>授权码列表</returns>
    Task<List<SysOAuthCode>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}

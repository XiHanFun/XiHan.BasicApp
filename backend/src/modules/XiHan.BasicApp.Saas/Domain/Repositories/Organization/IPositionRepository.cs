#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPositionRepository
// Guid:4b7da3ec-9f56-4a2b-be83-3c4d5e6f7082
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 系统岗位仓储接口
/// </summary>
public interface IPositionRepository : ISaasRepository<SysPosition>
{
    /// <summary>
    /// 根据岗位编码获取
    /// </summary>
    Task<SysPosition?> GetByCodeAsync(string positionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查岗位编码是否存在
    /// </summary>
    Task<bool> ExistsCodeAsync(string positionCode, long? excludeId = null, CancellationToken cancellationToken = default);
}

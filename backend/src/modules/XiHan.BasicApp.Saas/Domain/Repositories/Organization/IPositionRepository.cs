// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

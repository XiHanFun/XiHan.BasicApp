// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 岗位领域服务
/// </summary>
public interface IPositionDomainService
{
    /// <summary>
    /// 创建岗位
    /// </summary>
    Task<PositionCommandResult> CreatePositionAsync(PositionCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除岗位
    /// </summary>
    Task DeletePositionAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新岗位
    /// </summary>
    Task<PositionCommandResult> UpdatePositionAsync(PositionUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新岗位状态
    /// </summary>
    Task<PositionCommandResult> UpdatePositionStatusAsync(PositionStatusChangeCommand command, CancellationToken cancellationToken = default);
}

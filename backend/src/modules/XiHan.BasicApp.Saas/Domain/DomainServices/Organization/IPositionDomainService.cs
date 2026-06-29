#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPositionDomainService
// Guid:7e0fd61f-c289-4d5e-e1b6-6f7081920315
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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

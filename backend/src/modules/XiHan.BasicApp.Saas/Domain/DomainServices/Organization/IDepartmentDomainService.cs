#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentDomainService
// Guid:c4055471-4f1b-45df-a377-4ca5583bdf4d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 部门领域服务
/// </summary>
public interface IDepartmentDomainService
{
    /// <summary>
    /// 创建部门
    /// </summary>
    Task<DepartmentCommandResult> CreateAsync(DepartmentCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新部门
    /// </summary>
    Task<DepartmentCommandResult> UpdateAsync(DepartmentUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新部门状态
    /// </summary>
    Task<DepartmentCommandResult> UpdateStatusAsync(DepartmentStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除部门
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

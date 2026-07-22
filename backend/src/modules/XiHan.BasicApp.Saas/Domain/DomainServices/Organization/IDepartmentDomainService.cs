// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

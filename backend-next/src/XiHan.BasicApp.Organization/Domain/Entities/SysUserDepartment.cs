// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 用户部门归属（租户库实体）。
/// </summary>
/// <remarks>
/// 一个用户可在多个部门，恰有一个主部门（<see cref="IsPrimary"/>）；用户是平台库的 <c>SysUser</c>（注册地必须是当前租户），这里只存主键。
/// </remarks>
public sealed class SysUserDepartment : TenantScopedAuditedEntity
{
    /// <summary>
    /// 用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 部门。
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// 在该部门的岗位。
    /// </summary>
    /// <remarks>
    /// 为空即未指定。
    /// </remarks>
    public Guid? PositionId { get; set; }

    /// <summary>
    /// 是否主部门：设为主时同一用户的其他归属自动取消主标记。
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// 工号。
    /// </summary>
    public string? EmployeeNumber { get; set; }

    /// <summary>
    /// 职级。
    /// </summary>
    public string? JobGrade { get; set; }

    /// <summary>
    /// 入职时间（UTC）。
    /// </summary>
    public DateTimeOffset? HiredAt { get; set; }

    /// <summary>
    /// 部门内排序号。
    /// </summary>
    public int Order { get; set; }
}

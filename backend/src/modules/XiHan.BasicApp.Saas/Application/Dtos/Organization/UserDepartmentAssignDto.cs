// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户部门归属分配 DTO
/// </summary>
public sealed class UserDepartmentAssignDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 部门主键
    /// </summary>
    public long DepartmentId { get; set; }

    /// <summary>
    /// 岗位主键（用户在该部门担任的岗位，可空）
    /// </summary>
    public long? PositionId { get; set; }

    /// <summary>
    /// 工号
    /// </summary>
    public string? JobNumber { get; set; }

    /// <summary>
    /// 职级
    /// </summary>
    public string? JobLevel { get; set; }

    /// <summary>
    /// 入职日期
    /// </summary>
    public DateTimeOffset? JoinTime { get; set; }

    /// <summary>
    /// 是否主部门
    /// </summary>
    public bool IsMain { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户部门归属批量变更中的单条分配项
/// </summary>
public sealed class UserDepartmentBatchAssignItemDto
{
    /// <summary>
    /// 部门主键
    /// </summary>
    public long DepartmentId { get; set; }

    /// <summary>
    /// 是否设为主部门（一次至多一项）
    /// </summary>
    public bool IsMain { get; set; }

    /// <summary>
    /// 岗位主键
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
    /// 入职时间
    /// </summary>
    public DateTimeOffset? JoinTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 用户部门归属批量变更 DTO（一次性提交本次归属改动）
/// </summary>
public sealed class UserDepartmentBatchUpdateDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 待分配的部门；已有效的部门再次下发只参与主部门调整
    /// </summary>
    public List<UserDepartmentBatchAssignItemDto> Assigns { get; set; } = [];

    /// <summary>
    /// 待撤销的用户部门归属记录主键集合
    /// </summary>
    public List<long> RevokeUserDepartmentIds { get; set; } = [];
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.BasicApp.Contracts.Organization;

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 用户部门归属（租户库实体）
/// </summary>
/// <remarks>
/// 一个用户可在多个部门，恰有一个主部门（<see cref="IsPrimary"/>）；用户是平台库的 <c>SysUser</c>（注册地必须是当前租户），这里只存主键
/// 加入与移出部门时记领域事件 <see cref="MembershipChanged"/>（主题 <c>basicapp.organization.membership-changed</c>），随写入进发件箱
/// </remarks>
[SugarTable(TableName = "sys_user_department", TableDescription = "用户部门归属（租户库实体）")]
[ActiveUniqueIndex(nameof(TenantId), nameof(UserId), nameof(DepartmentId), Name = "ux_sys_user_department_tenant_id_user_id_department_id")]
[SugarIndex("ix_sys_user_department_tenant_id_department_id", nameof(TenantId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc)]
[SugarIndex("ix_sys_user_department_tenant_id_position_id", nameof(TenantId), OrderByType.Asc, nameof(PositionId), OrderByType.Asc)]
public sealed class SysUserDepartment : TenantScopedAggregateRoot
{
    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 部门
    /// </summary>
    [SugarColumn(ColumnName = "department_id", IsNullable = false, ColumnDescription = "部门")]
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// 在该部门的岗位
    /// </summary>
    /// <remarks>
    /// 为空即未指定
    /// </remarks>
    [SugarColumn(ColumnName = "position_id", IsNullable = true, ColumnDescription = "在该部门的岗位")]
    public Guid? PositionId { get; set; }

    /// <summary>
    /// 是否主部门：设为主时同一用户的其他归属自动取消主标记
    /// </summary>
    [SugarColumn(ColumnName = "is_primary", IsNullable = false, ColumnDescription = "是否主部门：设为主时同一用户的其他归属自动取消主标记")]
    public bool IsPrimary { get; set; }

    /// <summary>
    /// 工号
    /// </summary>
    [SugarColumn(ColumnName = "employee_number", Length = 512, IsNullable = true, ColumnDescription = "工号")]
    public string? EmployeeNumber { get; set; }

    /// <summary>
    /// 职级
    /// </summary>
    [SugarColumn(ColumnName = "job_grade", Length = 512, IsNullable = true, ColumnDescription = "职级")]
    public string? JobGrade { get; set; }

    /// <summary>
    /// 入职时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "hired_at", IsNullable = true, ColumnDescription = "入职时间（UTC）")]
    public DateTimeOffset? HiredAt { get; set; }

    /// <summary>
    /// 部门内排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "部门内排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }

    /// <summary>
    /// 记下用户加入了部门
    /// </summary>
    /// <param name="at">发生时间（UTC）</param>
    public void RecordJoined(DateTimeOffset at) => Raise(new MembershipChanged(UserId, DepartmentId, Joined: true, at));

    /// <summary>
    /// 记下用户移出了部门
    /// </summary>
    /// <param name="at">发生时间（UTC）</param>
    public void RecordLeft(DateTimeOffset at) => Raise(new MembershipChanged(UserId, DepartmentId, Joined: false, at));
}

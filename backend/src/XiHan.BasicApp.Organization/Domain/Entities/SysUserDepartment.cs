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
/// 用户与部门的关联表，带任职属性（岗位、工号、职级、入职时间）：一个人可在多个部门任职，这些属性按任职存而不是按人存
/// 恰有一个主部门（<see cref="IsPrimary"/>）：用户列表的主部门、流程从主部门往上找主管、权限条件的主部门属性都要一个确定的答案；设为主部门时取消同一用户的其他主标记，移出主部门时剩下的第一条自动接上
/// 用户只存平台库 <c>SysUser</c> 的主键：用户在平台库、任职在租户库，分库时不能联表也不建外键，名称按主键批量补；写入时核对用户在当前租户（注册在这里或外部成员）
/// （租户, 用户, 部门）用部分唯一索引：同一部门只能任职一次，移出（软删除）后可以重新加入
/// 是聚合根：加入与移出记领域事件 <see cref="MembershipChanged"/>，随写入进发件箱、与归属同一事务提交，回滚即不发；Chat 据此同步部门群，本包不必知道下游
/// 任职是数据范围从部门展开成用户的依据，也是部门成员数的来源，所以加入与移出让部门缓存失效；任职列表本身也按调用方的数据范围收窄
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

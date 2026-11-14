// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 部门（租户库实体，严格按租户过滤）
/// </summary>
/// <remarks>
/// 组织是租户自己的数据：部门落租户库、只在租户侧管理，平台一侧查不到任何部门，操作没有租户上下文直接拒绝
/// 树用 <see cref="ParentId"/> 存直接上级，祖先 / 子孙关系另由闭包表 <see cref="SysDepartmentHierarchy"/> 维护：改上级与组树只看 <see cref="ParentId"/>，数据范围「本部门及子部门」、按部门找人含下级要一次取出全部子孙，靠闭包表免去递归；两者都经 <see cref="Services.DepartmentService"/> 写，保持一致
/// 编码租户内唯一、建后不改，用部分唯一索引：软删除后同编码可以重建；导入用户、组织目录与流程办理人都按编码找部门，编码要稳定
/// 负责人只存平台库 <c>SysUser</c> 的主键：用户在平台库、部门在租户库，分库时不能联表也不建外键，写入时核对负责人在本租户（注册在这里或外部成员）
/// 整租户的部门连同各部门成员数作为一份读穿缓存（<c>DepartmentSnapshots</c>）：部门树与选择器每次都要全量，一个租户的部门以百计，整份缓存比逐级查便宜；部门与归属写入即在工作单元结束后失效，只在只读操作里回填（写事务里读会把未提交的行写进缓存），60 秒过期兜住从库的延迟
/// 软删除，还有子部门或成员的不能删，免得留下孤儿节点与悬空的任职；它的闭包行随之物理删除
/// 不用的部门改停用而不是删除：数据与任职都留着，但按编码找成员与负责人、字段安全、权限条件的部门属性与 Chat 的部门树只认启用的部门
/// Access 与 Chat 直接引用本包读仓储；Identity 在本包之下，Workflow 与 Notification 不引用本包，都经 Contracts 的组织目录与接收人来源读部门
/// </remarks>
[SugarTable(TableName = "sys_department", TableDescription = "部门（租户库实体，严格按租户过滤）")]
[ActiveUniqueIndex(nameof(TenantId), nameof(Code), Name = "ux_sys_department_tenant_id_code")]
public sealed class SysDepartment : TenantScopedAuditedEntity
{
    /// <summary>
    /// 部门编码：租户内唯一，字母或数字开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 64
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 512, IsNullable = false, ColumnDescription = "部门编码：租户内唯一，字母或数字开头，只含字母、数字、.、_、-，最长 64")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 512, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 部门类型
    /// </summary>
    [SugarColumn(ColumnName = "department_type", IsNullable = false, ColumnDescription = "部门类型")]
    public DepartmentType DepartmentType { get; set; } = DepartmentType.Department;

    /// <summary>
    /// 直接上级部门
    /// </summary>
    /// <remarks>
    /// 为空即顶层
    /// </remarks>
    [SugarColumn(ColumnName = "parent_id", IsNullable = true, ColumnDescription = "直接上级部门")]
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 负责人（本租户的用户）
    /// </summary>
    /// <remarks>
    /// 为空即未指定
    /// </remarks>
    [SugarColumn(ColumnName = "leader_user_id", IsNullable = true, ColumnDescription = "负责人（本租户的用户）")]
    public Guid? LeaderUserId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

    /// <summary>
    /// 同级排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "同级排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [SugarColumn(ColumnName = "phone", Length = 20, IsNullable = true, ColumnDescription = "联系电话")]
    public string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [SugarColumn(ColumnName = "email", Length = 100, IsNullable = true, ColumnDescription = "邮箱")]
    public string? Email { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [SugarColumn(ColumnName = "address", Length = 500, IsNullable = true, ColumnDescription = "地址")]
    public string? Address { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true, ColumnDescription = "说明")]
    public string? Description { get; set; }
}

/// <summary>
/// 部门类型
/// </summary>
/// <remarks>
/// 部门是缺省值、排在 0，加列时已有的行读出来就是它（报文按名称，数值只在库里）
/// </remarks>
public enum DepartmentType
{
    /// <summary>
    /// 部门
    /// </summary>
    Department = 0,

    /// <summary>
    /// 集团
    /// </summary>
    Corporation = 1,

    /// <summary>
    /// 总部
    /// </summary>
    Headquarters = 2,

    /// <summary>
    /// 公司
    /// </summary>
    Company = 3,

    /// <summary>
    /// 分公司
    /// </summary>
    Branch = 4,

    /// <summary>
    /// 事业部
    /// </summary>
    Division = 5,

    /// <summary>
    /// 中心
    /// </summary>
    Center = 6,

    /// <summary>
    /// 科室
    /// </summary>
    Section = 7,

    /// <summary>
    /// 团队
    /// </summary>
    Team = 8,

    /// <summary>
    /// 小组
    /// </summary>
    Group = 9,

    /// <summary>
    /// 项目组
    /// </summary>
    Project = 10,

    /// <summary>
    /// 工作组
    /// </summary>
    Workgroup = 11,

    /// <summary>
    /// 虚拟组织
    /// </summary>
    Virtual = 12,

    /// <summary>
    /// 办事处
    /// </summary>
    Office = 13,

    /// <summary>
    /// 子公司
    /// </summary>
    Subsidiary = 14,

    /// <summary>
    /// 其他
    /// </summary>
    Other = 99,
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统字典实体
/// 静态枚举数据集的元信息（字典本身），具体选项由 SysDictItem 承载
/// </summary>
/// <remarks>
/// 关联：
/// - 反向：SysDictItem.DictId
///
/// 写入：
/// - TenantId + DictCode 租户内唯一（UX_TeId_DiCo）
/// - DictType 区分系统内置 / 业务自定义（内置字典禁止普通租户修改）
///
/// 查询：
/// - 前端下拉框缓存加载：按 TenantId + Status=Yes 全量读取
/// - 按类型过滤：IX_DiTy
///
/// 删除：
/// - 仅软删；删除前必须级联处理 SysDictItem
///
/// 状态：
/// - Status: Yes/No（停用字典后所有子项不再对外暴露）
///
/// 场景：
/// - 订单状态、客户等级、审批类型等业务枚举
/// - 多语言字典：可通过扩展字段承载 i18n
/// </remarks>
[SugarTable(TableName = "Sys_Dict", TableDescription = "系统字典表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_DiCo", nameof(TenantId), OrderByType.Asc, nameof(DictCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_DiTy", nameof(DictType), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysDict : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 字典编码
    /// </summary>
    [SugarColumn(ColumnName = "Dict_Code", ColumnDescription = "字典编码", Length = 100, IsNullable = false)]
    public virtual string DictCode { get; set; } = string.Empty;

    /// <summary>
    /// 字典名称
    /// </summary>
    [SugarColumn(ColumnName = "Dict_Name", ColumnDescription = "字典名称", Length = 100, IsNullable = false)]
    public virtual string DictName { get; set; } = string.Empty;

    /// <summary>
    /// 字典类型
    /// </summary>
    [SugarColumn(ColumnName = "Dict_Type", ColumnDescription = "字典类型", Length = 50, IsNullable = false)]
    public virtual string DictType { get; set; } = string.Empty;

    /// <summary>
    /// 字典描述
    /// </summary>
    [SugarColumn(ColumnName = "Dict_Description", ColumnDescription = "字典描述", Length = 500, IsNullable = true)]
    public virtual string? DictDescription { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    [SugarColumn(ColumnName = "Is_Built_In", ColumnDescription = "是否内置")]
    public virtual bool IsBuiltIn { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Routing;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统字段级安全实体（Field Level Security, FLS）
/// 限制某个角色/用户/部门对某个实体某个字段的读取方式与修改能力
/// </summary>
/// <remarks>
/// 规则只收紧、不放宽：没有规则就是明文可改；一条规则要么让字段不再明文可读（<see cref="MaskStrategy"/> 非 None），
/// 要么让字段只读（<see cref="IsEditable"/> 为 false），两者都不收紧的规则没有意义、不允许保存。
///
/// 命中与合并：
/// - 命中：目标是当前用户、当前用户生效的角色、当前用户所在部门或其上级部门，且规则已启用
/// - 合并：同一字段取最严的读取方式（隐藏 &gt; 固定文本 &gt; 全部星号 &gt; 哈希 &gt; 部分脱敏 &gt; 明文），
///   任一命中规则只读即只读
///
/// 脱敏且可编辑即「只写」：看不到原值，但可以填新值覆盖（如更换密钥）；表单把脱敏值原样交回视为没改。
///
/// 生效范围：平台（TenantId = 0）的规则对所有租户生效，只能在平台维护；租户规则只在本租户生效。
///
/// 示例：
/// - 角色「客服」对 SysUser.Phone：部分脱敏，保留前 3 后 4 → 138****1234
/// - 部门「外包组」对 SysUser.Email：隐藏
/// - 角色「运营」对 SysTenant.ContactPhone：明文只读
/// - 角色「运维」对 SysAiProvider.ApiKey：隐藏但可编辑（只能换新密钥，看不到旧的）
/// </remarks>
[SugarTable(TableName = "Sys_Field_Level_Security", TableDescription = "系统字段级安全表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_TaTy_TaId_EnNa_FiNa", nameof(TenantId), OrderByType.Asc, nameof(TargetType), OrderByType.Asc, nameof(TargetId), OrderByType.Asc, nameof(EntityName), OrderByType.Asc, nameof(FieldName), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TaTy_TaId", nameof(TargetType), OrderByType.Asc, nameof(TargetId), OrderByType.Asc)]
[SugarIndex("IX_{table}_EnNa_St", nameof(EntityName), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[PlatformDataSource]
public partial class SysFieldLevelSecurity : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 目标类型（规则绑定到角色/用户/部门）
    /// </summary>
    [SugarColumn(ColumnName = "Target_Type", ColumnDescription = "目标类型")]
    public virtual FieldSecurityTargetType TargetType { get; set; } = FieldSecurityTargetType.Role;

    /// <summary>
    /// 目标ID（角色ID/用户ID/部门ID，具体对应 TargetType）
    /// </summary>
    [SugarColumn(ColumnName = "Target_Id", ColumnDescription = "目标ID", IsNullable = false)]
    public virtual long TargetId { get; set; }

    /// <summary>
    /// 实体名（实体类名，如 SysUser；须在字段安全实体目录里登记过）
    /// </summary>
    [SugarColumn(ColumnName = "Entity_Name", ColumnDescription = "实体名", Length = 100, IsNullable = false)]
    public virtual string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// 字段名（实体属性名，区分大小写；接口返回按同名属性处理）
    /// </summary>
    [SugarColumn(ColumnName = "Field_Name", ColumnDescription = "字段名", Length = 100, IsNullable = false)]
    public virtual string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 读取方式（None 为明文；其余为脱敏或隐藏）
    /// </summary>
    [SugarColumn(ColumnName = "Mask_Strategy", ColumnDescription = "读取方式")]
    public virtual FieldMaskStrategy MaskStrategy { get; set; } = FieldMaskStrategy.None;

    /// <summary>
    /// 部分脱敏保留的前几位（仅 PartialMask）
    /// </summary>
    [SugarColumn(ColumnName = "Mask_Keep_Head", ColumnDescription = "保留前几位", IsNullable = true)]
    public virtual int? MaskKeepHead { get; set; }

    /// <summary>
    /// 部分脱敏保留的后几位（仅 PartialMask）
    /// </summary>
    [SugarColumn(ColumnName = "Mask_Keep_Tail", ColumnDescription = "保留后几位", IsNullable = true)]
    public virtual int? MaskKeepTail { get; set; }

    /// <summary>
    /// 固定文本（仅 Redact：有值时一律显示这段文字）
    /// </summary>
    [SugarColumn(ColumnName = "Mask_Replacement", ColumnDescription = "固定文本", Length = 100, IsNullable = true)]
    public virtual string? MaskReplacement { get; set; }

    /// <summary>
    /// 是否可编辑（false 时新建不能填、修改不能改；脱敏且可编辑为只写，明文规则必须只读）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Editable", ColumnDescription = "是否可编辑")]
    public virtual bool IsEditable { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}

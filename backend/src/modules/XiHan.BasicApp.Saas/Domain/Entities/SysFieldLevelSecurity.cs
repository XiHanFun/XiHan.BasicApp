// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统字段级安全实体（Field Level Security, FLS）
/// 控制角色/用户/权限对某个资源特定字段的读写能力，以及只读场景下的脱敏策略
/// </summary>
/// <remarks>
/// 决策流程（与 RBAC/ABAC 决策之后生效）：
/// 1. 加载命中的所有 FLS 记录：匹配 (TargetType, TargetId) ∈ 当前用户的 角色集/用户自身/拥有的权限
/// 2. 冲突合并规则（deny-overrides + 优先级覆盖）：
///    - 默认：任一命中规则 IsReadable=false → 最终不可读；任一 IsEditable=false → 最终不可编辑
///    - 当 Priority 不同时，高优先级规则覆盖低优先级（便于"用户级放行某字段"的白名单场景）
/// 3. 字段可见性三段语义：
///    - IsReadable=true, MaskStrategy=None → 可读原文
///    - IsReadable=true, MaskStrategy!=None → 可读脱敏值（如客服看手机号 138****1234）
///    - IsReadable=false, MaskStrategy!=None → 不可读，按策略返回脱敏/隐藏结果
///    不可编辑时字段只读
///
/// 使用建议：
/// - 绝大多数场景推荐绑定到 Role（TargetType=Role），便于批量管理
/// - 临时例外绑定到 User（TargetType=User，Priority 较高）
/// - 绑定到 Permission（TargetType=Permission）适合"拥有导出权限时可见全部字段"等随权限门控的场景
///
/// 示例：
/// - HR 场景：角色"普通员工"对 SysUser.Salary 字段 → IsReadable=false, MaskStrategy=Hidden
/// - 客服场景：角色"客服"对 SysUser.Phone 字段 → IsReadable=true, MaskStrategy=PartialMask, MaskPattern="keep:3,4"（可读脱敏值 138****1234）
/// - 财务场景：用户 U100 对 SysOrder.Amount → Priority=100 覆盖角色级限制允许可编辑
/// </remarks>
[SugarTable(TableName = "Sys_Field_Level_Security", TableDescription = "系统字段级安全表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_TaTy_TaId_ReId_FiNa", nameof(TenantId), OrderByType.Asc, nameof(TargetType), OrderByType.Asc, nameof(TargetId), OrderByType.Asc, nameof(ResourceId), OrderByType.Asc, nameof(FieldName), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TaTy_TaId", nameof(TargetType), OrderByType.Asc, nameof(TargetId), OrderByType.Asc)]
[SugarIndex("IX_{table}_ReId_FiNa", nameof(ResourceId), OrderByType.Asc, nameof(FieldName), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysFieldLevelSecurity : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 目标类型（策略绑定到角色/用户/权限）
    /// </summary>
    [SugarColumn(ColumnName = "Target_Type", ColumnDescription = "目标类型")]
    public virtual FieldSecurityTargetType TargetType { get; set; } = FieldSecurityTargetType.Role;

    /// <summary>
    /// 目标ID（角色ID/用户ID/权限ID，具体对应 TargetType）
    /// </summary>
    [SugarColumn(ColumnName = "Target_Id", ColumnDescription = "目标ID", IsNullable = false)]
    public virtual long TargetId { get; set; }

    /// <summary>
    /// 资源ID（关联 SysResource，标识受控对象）
    /// </summary>
    [SugarColumn(ColumnName = "Resource_Id", ColumnDescription = "资源ID", IsNullable = false)]
    public virtual long ResourceId { get; set; }

    /// <summary>
    /// 字段名（资源对应数据结构的列/属性名，区分大小写）
    /// </summary>
    [SugarColumn(ColumnName = "Field_Name", ColumnDescription = "字段名", Length = 100, IsNullable = false)]
    public virtual string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可读（false 时返回值按 MaskStrategy 处理）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Readable", ColumnDescription = "是否可读")]
    public virtual bool IsReadable { get; set; } = true;

    /// <summary>
    /// 是否可编辑（false 时前端只读，后端写操作拒绝）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Editable", ColumnDescription = "是否可编辑")]
    public virtual bool IsEditable { get; set; } = true;

    /// <summary>
    /// 脱敏策略（IsReadable=true 时对可读值脱敏；IsReadable=false 时对不可读值脱敏/隐藏）
    /// </summary>
    [SugarColumn(ColumnName = "Mask_Strategy", ColumnDescription = "脱敏策略")]
    public virtual FieldMaskStrategy MaskStrategy { get; set; } = FieldMaskStrategy.None;

    /// <summary>
    /// 脱敏模式（JSON 或表达式，配合 MaskStrategy 使用）
    /// </summary>
    /// <remarks>
    /// 示例：
    /// - PartialMask: {"keepLeft":3,"keepRight":4,"maskChar":"*"} → 138****1234
    /// - Redact: "[已脱敏]" → 固定替换串
    /// - Custom: 自定义规则字符串，由应用层解析
    /// </remarks>
    [SugarColumn(ColumnName = "Mask_Pattern", ColumnDescription = "脱敏模式", Length = 500, IsNullable = true)]
    public virtual string? MaskPattern { get; set; }

    /// <summary>
    /// 优先级（数字越大优先级越高，与 SysPermission.Priority 方向一致）
    /// 用于冲突合并时覆盖低优先级规则（如用户级白名单覆盖角色级限制）
    /// </summary>
    [SugarColumn(ColumnName = "Priority", ColumnDescription = "优先级")]
    public virtual int Priority { get; set; } = 0;

    /// <summary>
    /// 策略描述
    /// </summary>
    [SugarColumn(ColumnName = "Description", ColumnDescription = "策略描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

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

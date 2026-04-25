#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperation
// Guid:3b4c5d6e-7f89-0123-cdef-123456789012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统操作实体
/// 权限体系中的"动作"（Action）：定义可对资源执行的操作，与 SysResource 组合形成 SysPermission
/// </summary>
/// <remarks>
/// 关联：
/// - 反向：SysPermission.OperationId
///
/// 写入：
/// - TenantId + OperationCode 租户内唯一（UX_TeId_OpCo）
/// - OperationCode 规范：小写英文（create/read/update/delete/approve/export）
/// - IsGlobal=true 时作为平台动作模板（CRUD/Approve/Export 等通用操作）
/// - IsDangerous=true 的操作前端应弹二次确认；IsRequireAudit=true 的操作必须写 SysAuditLog
///
/// 查询：
/// - 按 Category/OperationTypeCode 过滤用于前端操作面板分组
/// - 全局+私有合并查询优先使用：WHERE TenantId IN (0, ?)
///   IsGlobal 仅作为语义标记，不应替代 TenantId=0 的平台记录约束
///
/// 删除：
/// - 仅软删；删除前必须校验：无权限引用（SysPermission.OperationId）
///
/// 状态：
/// - Status: Yes/No 启停
///
/// 场景：
/// - 与 SysResource 笛卡尔积生成权限点
/// - 按 Category 分组渲染前端权限矩阵
/// - HttpMethod 字段用于 API 类资源的接口 Method 匹配
/// </remarks>
[SugarTable("SysOperation", "系统操作表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_OpCo", nameof(TenantId), OrderByType.Asc, nameof(OperationCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_Ca", nameof(Category), OrderByType.Asc)]
[SugarIndex("IX_{table}_OpTyCo", nameof(OperationTypeCode), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsGl", nameof(IsGlobal), OrderByType.Asc)]
public partial class SysOperation : BasicAppAggregateRoot
{
    /// <summary>
    /// 操作编码（唯一标识，如：create, read, update, delete, approve）
    /// </summary>
    [SugarColumn(ColumnDescription = "操作编码", Length = 50, IsNullable = false)]
    public virtual string OperationCode { get; set; } = string.Empty;

    /// <summary>
    /// 操作名称
    /// </summary>
    [SugarColumn(ColumnDescription = "操作名称", Length = 100, IsNullable = false)]
    public virtual string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型代码
    /// </summary>
    [SugarColumn(ColumnDescription = "操作类型代码")]
    public virtual OperationTypeCode OperationTypeCode { get; set; } = OperationTypeCode.Read;

    /// <summary>
    /// 操作分类
    /// </summary>
    [SugarColumn(ColumnDescription = "操作分类")]
    public virtual OperationCategory Category { get; set; } = OperationCategory.Crud;

    /// <summary>
    /// HTTP方法（针对API资源）
    /// </summary>
    [SugarColumn(ColumnDescription = "HTTP方法", IsNullable = true)]
    public virtual HttpMethodType? HttpMethod { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    [SugarColumn(ColumnDescription = "操作描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 操作图标
    /// </summary>
    [SugarColumn(ColumnDescription = "操作图标", Length = 100, IsNullable = true)]
    public virtual string? Icon { get; set; }

    /// <summary>
    /// 操作颜色（前端按钮样式）
    /// </summary>
    [SugarColumn(ColumnDescription = "操作颜色", Length = 20, IsNullable = true)]
    public virtual string? Color { get; set; }

    /// <summary>
    /// 是否危险操作（需要二次确认）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否危险操作")]
    public virtual bool IsDangerous { get; set; } = false;

    /// <summary>
    /// 是否需要审计日志
    /// </summary>
    [SugarColumn(ColumnDescription = "是否需要审计")]
    public virtual bool IsRequireAudit { get; set; } = false;

    /// <summary>
    /// 是否平台级全局操作（全局操作所有租户共享，TenantId = 0）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否全局操作")]
    public virtual bool IsGlobal { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}

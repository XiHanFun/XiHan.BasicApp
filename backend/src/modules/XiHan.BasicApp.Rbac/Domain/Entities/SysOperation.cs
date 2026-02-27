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
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统操作实体
/// 定义可对资源执行的操作类型（增删改查、审批、导入导出等）
/// </summary>
[SugarTable("Sys_Operation", "系统操作表")]
[SugarIndex("IX_SysOperation_OpCo", nameof(OperationCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysOperation_Ca", nameof(Category), OrderByType.Asc)]
[SugarIndex("IX_SysOperation_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysOperation_OpTyCo", nameof(OperationTypeCode), OrderByType.Asc)]
[SugarIndex("IX_SysOperation_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysOperation : BasicAppCreationEntity
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
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

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

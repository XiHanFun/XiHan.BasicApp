#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysResource
// Guid:2a3b4c5d-6e7f-8901-bcde-f12345678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统资源实体
/// 统一抽象所有可被授权的资源（菜单、API、按钮、文件等）
/// </summary>
[SugarTable("Sys_Resource", "系统资源表")]
[SugarIndex("IX_SysResource_ReCo", nameof(ResourceCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysResource_PaId", nameof(ParentId), OrderByType.Asc)]
[SugarIndex("IX_SysResource_ReTy", nameof(ResourceType), OrderByType.Asc)]
[SugarIndex("IX_SysResource_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysResource_ReTy_St", nameof(ResourceType), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysResource_TeId_ReTy_St", nameof(TenantId), OrderByType.Asc, nameof(ResourceType), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysResource : RbacCreationEntity<long>
{
    /// <summary>
    /// 父资源ID（支持资源树结构）
    /// </summary>
    [SugarColumn(ColumnDescription = "父资源ID", IsNullable = true)]
    public virtual long? ParentId { get; set; }

    /// <summary>
    /// 资源编码（唯一标识，如：user:list, order:create）
    /// </summary>
    [SugarColumn(ColumnDescription = "资源编码", Length = 100, IsNullable = false)]
    public virtual string ResourceCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    [SugarColumn(ColumnDescription = "资源名称", Length = 100, IsNullable = false)]
    public virtual string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型
    /// </summary>
    [SugarColumn(ColumnDescription = "资源类型")]
    public virtual ResourceType ResourceType { get; set; } = ResourceType.Menu;

    /// <summary>
    /// 资源路径（URL/文件路径/API路径等）
    /// </summary>
    [SugarColumn(ColumnDescription = "资源路径", Length = 500, IsNullable = true)]
    public virtual string? ResourcePath { get; set; }

    /// <summary>
    /// 资源图标
    /// </summary>
    [SugarColumn(ColumnDescription = "资源图标", Length = 100, IsNullable = true)]
    public virtual string? Icon { get; set; }

    /// <summary>
    /// 资源描述
    /// </summary>
    [SugarColumn(ColumnDescription = "资源描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 资源元数据（JSON格式，存储扩展信息）
    /// 例如：{"method": "GET", "controller": "UserController", "action": "GetList"}
    /// </summary>
    [SugarColumn(ColumnDescription = "资源元数据", ColumnDataType = "text", IsNullable = true)]
    public virtual string? Metadata { get; set; }

    /// <summary>
    /// 是否需要认证
    /// </summary>
    [SugarColumn(ColumnDescription = "是否需要认证")]
    public virtual bool IsRequireAuth { get; set; } = true;

    /// <summary>
    /// 是否公开资源（无需授权即可访问）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否公开")]
    public virtual bool IsPublic { get; set; } = false;

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

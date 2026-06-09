#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionConditionListItemDto
// Guid:7cf6e35c-10e3-49eb-8dd9-f55709877d47
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限 ABAC 条件列表项 DTO
/// </summary>
public class PermissionConditionListItemDto : BasicAppDto
{
    /// <summary>
    /// 角色权限绑定主键
    /// </summary>
    public long? RolePermissionId { get; set; }

    /// <summary>
    /// 用户直授权限绑定主键
    /// </summary>
    public long? UserPermissionId { get; set; }

    /// <summary>
    /// 角色主键
    /// </summary>
    public long? RoleId { get; set; }

    /// <summary>
    /// 角色编码
    /// </summary>
    public string? RoleCode { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户租户内显示名
    /// </summary>
    public string? UserDisplayName { get; set; }

    /// <summary>
    /// 权限主键
    /// </summary>
    public long? PermissionId { get; set; }

    /// <summary>
    /// 权限编码
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 权限名称
    /// </summary>
    public string? PermissionName { get; set; }

    /// <summary>
    /// 条件分组
    /// </summary>
    public int ConditionGroup { get; set; }

    /// <summary>
    /// 属性名称
    /// </summary>
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// 操作符
    /// </summary>
    public ConditionOperator Operator { get; set; }

    /// <summary>
    /// 是否取反
    /// </summary>
    public bool IsNegated { get; set; }

    /// <summary>
    /// 条件值类型
    /// </summary>
    public ConfigDataType ValueType { get; set; }

    /// <summary>
    /// 条件值
    /// </summary>
    public string ConditionValue { get; set; } = string.Empty;

    /// <summary>
    /// 条件说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ValidityStatus Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}

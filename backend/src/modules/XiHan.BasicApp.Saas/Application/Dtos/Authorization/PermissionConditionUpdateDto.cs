#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionConditionUpdateDto
// Guid:5d6112a0-ae55-4c18-92ac-d877da5379e6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限 ABAC 条件更新 DTO
/// </summary>
public sealed class PermissionConditionUpdateDto : BasicAppUDto
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
    public ConditionOperator Operator { get; set; } = ConditionOperator.Equals;

    /// <summary>
    /// 是否取反
    /// </summary>
    public bool IsNegated { get; set; }

    /// <summary>
    /// 条件值类型
    /// </summary>
    public ConfigDataType ValueType { get; set; } = ConfigDataType.String;

    /// <summary>
    /// 条件值
    /// </summary>
    public string ConditionValue { get; set; } = string.Empty;

    /// <summary>
    /// 条件说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

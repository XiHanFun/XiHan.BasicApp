#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleHierarchyDetailDto
// Guid:674d5901-6ada-45fd-949d-2c22aa5f8a3e
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
/// 角色继承详情 DTO
/// </summary>
public sealed class RoleHierarchyDetailDto : BasicAppDto
{
    /// <summary>
    /// 祖先角色主键
    /// </summary>
    public long AncestorId { get; set; }

    /// <summary>
    /// 祖先角色编码
    /// </summary>
    public string? AncestorRoleCode { get; set; }

    /// <summary>
    /// 祖先角色名称
    /// </summary>
    public string? AncestorRoleName { get; set; }

    /// <summary>
    /// 祖先角色类型
    /// </summary>
    public RoleType? AncestorRoleType { get; set; }

    /// <summary>
    /// 祖先角色状态
    /// </summary>
    public EnableStatus? AncestorStatus { get; set; }

    /// <summary>
    /// 祖先角色是否全局
    /// </summary>
    public bool? IsAncestorGlobal { get; set; }

    /// <summary>
    /// 后代角色主键
    /// </summary>
    public long DescendantId { get; set; }

    /// <summary>
    /// 后代角色编码
    /// </summary>
    public string? DescendantRoleCode { get; set; }

    /// <summary>
    /// 后代角色名称
    /// </summary>
    public string? DescendantRoleName { get; set; }

    /// <summary>
    /// 后代角色类型
    /// </summary>
    public RoleType? DescendantRoleType { get; set; }

    /// <summary>
    /// 后代角色状态
    /// </summary>
    public EnableStatus? DescendantStatus { get; set; }

    /// <summary>
    /// 后代角色是否全局
    /// </summary>
    public bool? IsDescendantGlobal { get; set; }

    /// <summary>
    /// 继承深度
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// 继承路径
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 创建人主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreatedBy { get; set; }
}

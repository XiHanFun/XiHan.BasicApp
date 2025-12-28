#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPermissionDto
// Guid:cc2b3c4d-5e6f-7890-abcd-ef123456789c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 19:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.Base.Dtos;

namespace XiHan.BasicApp.Rbac.Services.UserPermissions.Dtos;

/// <summary>
/// 用户权限 DTO
/// </summary>
public class UserPermissionDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public XiHanBasicAppIdType UserId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public XiHanBasicAppIdType PermissionId { get; set; }

    /// <summary>
    /// 权限操作（授予/禁用）
    /// </summary>
    public PermissionAction PermissionAction { get; set; } = PermissionAction.Grant;

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 创建用户权限 DTO
/// </summary>
public class CreateUserPermissionDto : RbacCreationDtoBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public XiHanBasicAppIdType UserId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public XiHanBasicAppIdType PermissionId { get; set; }

    /// <summary>
    /// 权限操作（授予/禁用）
    /// </summary>
    public PermissionAction PermissionAction { get; set; } = PermissionAction.Grant;

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新用户权限 DTO
/// </summary>
public class UpdateUserPermissionDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 权限操作（授予/禁用）
    /// </summary>
    public PermissionAction? PermissionAction { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 批量授予用户权限 DTO
/// </summary>
public class BatchGrantUserPermissionsDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public XiHanBasicAppIdType UserId { get; set; }

    /// <summary>
    /// 权限ID列表
    /// </summary>
    public List<XiHanBasicAppIdType> PermissionIds { get; set; } = [];

    /// <summary>
    /// 权限操作（授予/禁用）
    /// </summary>
    public PermissionAction PermissionAction { get; set; } = PermissionAction.Grant;

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}


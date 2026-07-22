// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色管理详情聚合 DTO
/// </summary>
public sealed class RoleManagementDetailDto
{
    /// <summary>
    /// 角色基础详情
    /// </summary>
    public RoleDetailDto Role { get; set; } = new();

    /// <summary>
    /// 角色祖先链
    /// </summary>
    public List<RoleHierarchyListItemDto> Ancestors { get; set; } = [];

    /// <summary>
    /// 角色后代链
    /// </summary>
    public List<RoleHierarchyListItemDto> Descendants { get; set; } = [];

    /// <summary>
    /// 角色权限授权
    /// </summary>
    public List<RolePermissionListItemDto> Permissions { get; set; } = [];

    /// <summary>
    /// 角色数据范围
    /// </summary>
    public List<RoleDataScopeListItemDto> DataScopes { get; set; } = [];

    /// <summary>
    /// 授权用户
    /// </summary>
    public List<RoleManagementGrantedUserDto> GrantedUsers { get; set; } = [];

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTimeOffset GeneratedTime { get; set; }
}

/// <summary>
/// 角色管理授权用户 DTO
/// </summary>
public sealed class RoleManagementGrantedUserDto
{
    /// <summary>
    /// 用户角色绑定主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 授权原因
    /// </summary>
    public string? GrantReason { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ValidityStatus Status { get; set; }

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}

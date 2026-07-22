// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户列表项 DTO
/// </summary>
public sealed class UserListItemDto : BasicAppDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

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
    /// 性别
    /// </summary>
    public UserGender Gender { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 时区
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// 国家/地区
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// 是否系统内置账号
    /// </summary>
    public bool IsSystemAccount { get; set; }

    /// <summary>
    /// 角色名称集合（来自 SysUserRole→SysRole，仅有效授权 + 启用角色）
    /// </summary>
    public IReadOnlyList<string> RoleNames { get; set; } = [];

    /// <summary>
    /// 主部门名称（优先主部门，否则取首个有效归属；来自 SysUserDepartment→SysDepartment）
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 是否已锁定（来自 SysUserSecurity）
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// 是否启用双因素认证（来自 SysUserSecurity）
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTimeOffset? LastLoginTime { get; set; }

    /// <summary>
    /// 最后登录 IP
    /// </summary>
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

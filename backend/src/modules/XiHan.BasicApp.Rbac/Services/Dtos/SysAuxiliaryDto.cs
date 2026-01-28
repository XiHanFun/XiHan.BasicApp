#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuxiliaryDto
// Guid:d2e3f4a5-b6c7-8901-2345-678d90123456
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

#region 用户会话

/// <summary>
/// 系统用户会话创建 DTO
/// </summary>
public class SysUserSessionCreateDto : RbacCreationDtoBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 访问令牌
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 会话标识
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    /// <summary>
    /// 设备名称
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// 设备ID
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 登录位置
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Token过期时间
    /// </summary>
    public DateTimeOffset TokenExpiresAt { get; set; }

    /// <summary>
    /// RefreshToken过期时间
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统用户会话更新 DTO
/// </summary>
public class SysUserSessionUpdateDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTimeOffset LastActivityTime { get; set; }

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// 撤销原因
    /// </summary>
    public string? RevokedReason { get; set; }

    /// <summary>
    /// 登出时间
    /// </summary>
    public DateTimeOffset? LogoutTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统用户会话查询 DTO
/// </summary>
public class SysUserSessionGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 访问令牌
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 会话标识
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    /// <summary>
    /// 设备名称
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// 设备ID
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 登录位置
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTimeOffset LastActivityTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Token过期时间
    /// </summary>
    public DateTimeOffset TokenExpiresAt { get; set; }

    /// <summary>
    /// RefreshToken过期时间
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline { get; set; } = true;

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// 撤销原因
    /// </summary>
    public string? RevokedReason { get; set; }

    /// <summary>
    /// 登出时间
    /// </summary>
    public DateTimeOffset? LogoutTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

#endregion

#region 角色继承关系

/// <summary>
/// 系统角色层级关系创建 DTO
/// </summary>
public class SysRoleHierarchyCreateDto : RbacCreationDtoBase
{
    /// <summary>
    /// 祖先角色ID（被继承的角色）
    /// </summary>
    public long AncestorId { get; set; }

    /// <summary>
    /// 后代角色ID（继承者角色）
    /// </summary>
    public long DescendantId { get; set; }

    /// <summary>
    /// 继承深度
    /// </summary>
    public int Depth { get; set; } = 0;

    /// <summary>
    /// 是否直接继承
    /// </summary>
    public bool IsDirect { get; set; } = true;

    /// <summary>
    /// 继承路径
    /// </summary>
    public string? Path { get; set; }

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
/// 系统角色层级关系查询 DTO
/// </summary>
public class SysRoleHierarchyGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 祖先角色ID（被继承的角色）
    /// </summary>
    public long AncestorId { get; set; }

    /// <summary>
    /// 后代角色ID（继承者角色）
    /// </summary>
    public long DescendantId { get; set; }

    /// <summary>
    /// 继承深度
    /// </summary>
    public int Depth { get; set; } = 0;

    /// <summary>
    /// 是否直接继承
    /// </summary>
    public bool IsDirect { get; set; } = true;

    /// <summary>
    /// 继承路径
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

#endregion

#region 会话角色映射

/// <summary>
/// 系统会话角色映射创建 DTO
/// </summary>
public class SysSessionRoleCreateDto : RbacCreationDtoBase
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public long SessionId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 激活时间
    /// </summary>
    public DateTimeOffset ActivatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 激活原因
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// 系统会话角色映射查询 DTO
/// </summary>
public class SysSessionRoleGetDto : RbacDtoBase
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public long SessionId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 激活时间
    /// </summary>
    public DateTimeOffset ActivatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 停用时间
    /// </summary>
    public DateTimeOffset? DeactivatedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 会话角色状态
    /// </summary>
    public SessionRoleStatus Status { get; set; } = SessionRoleStatus.Active;

    /// <summary>
    /// 激活原因
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; } = DateTimeOffset.Now;
}

#endregion

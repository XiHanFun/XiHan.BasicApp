using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 角色 DTO
/// </summary>
public class RoleDto : BasicAppDto
{
    /// <summary>
    /// 角色编码
    /// </summary>
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// 角色名称
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? RoleDescription { get; set; }

    /// <summary>
    /// 角色类型
    /// </summary>
    public RoleType RoleType { get; set; } = RoleType.System;

    /// <summary>
    /// 数据权限范围
    /// </summary>
    public DataPermissionScope DataScope { get; set; } = DataPermissionScope.SelfOnly;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 创建角色 DTO
/// </summary>
public class RoleCreateDto : BasicAppCDto
{
    /// <summary>
    /// 角色编码
    /// </summary>
    [Required(ErrorMessage = "角色编码不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "角色编码长度必须在 1～50 之间")]
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// 角色名称
    /// </summary>
    [Required(ErrorMessage = "角色名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "角色名称长度必须在 1～100 之间")]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? RoleDescription { get; set; }

    /// <summary>
    /// 角色类型
    /// </summary>
    public RoleType RoleType { get; set; } = RoleType.System;

    /// <summary>
    /// 数据权限范围
    /// </summary>
    public DataPermissionScope DataScope { get; set; } = DataPermissionScope.SelfOnly;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>

    public long? TenantId { get; set; }
}

/// <summary>
/// 更新角色 DTO
/// </summary>
public class RoleUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 角色名称
    /// </summary>
    [Required(ErrorMessage = "角色名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "角色名称长度必须在 1～100 之间")]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? RoleDescription { get; set; }

    /// <summary>
    /// 数据权限范围
    /// </summary>
    public DataPermissionScope DataScope { get; set; } = DataPermissionScope.SelfOnly;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

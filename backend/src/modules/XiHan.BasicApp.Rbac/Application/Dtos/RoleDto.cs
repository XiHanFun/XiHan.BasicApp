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

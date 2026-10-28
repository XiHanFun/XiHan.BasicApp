// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 用户级数据范围覆盖（平台库实体，只在租户侧）：有这一行时完全取代该用户所有角色的数据范围（不取并集），没有即按角色。
/// <see cref="DataScope.Custom"/> 的部门在 <see cref="SysUserDataScopeDepartment"/>。
/// </summary>
public sealed class SysUserDataScope : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 覆盖后的数据范围。
    /// </summary>
    public DataScope DataScope { get; set; }
}

/// <summary>
/// 用户级自定义数据范围的一个部门（平台库实体，随覆盖整体替换）。
/// </summary>
public sealed class SysUserDataScopeDepartment : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 部门（租户库 <c>SysDepartment</c> 主键）。
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// 是否连同子部门。
    /// </summary>
    public bool IncludeChildren { get; set; }
}

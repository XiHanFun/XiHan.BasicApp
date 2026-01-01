#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DataPermissionAttribute
// Guid:ac2b3c4d-5e6f-7890-abcd-ef12345678bf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 7:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.DataPermissions.Attributes;

/// <summary>
/// 数据权限特性
/// 用于标记需要应用数据权限过滤的实体或查询
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class DataPermissionAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public DataPermissionAttribute()
    {
        Scope = DataPermissionScope.All;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scope">数据权限范围</param>
    public DataPermissionAttribute(DataPermissionScope scope)
    {
        Scope = scope;
    }

    /// <summary>
    /// 数据权限范围
    /// </summary>
    public DataPermissionScope Scope { get; set; }

    /// <summary>
    /// 部门字段名称（默认为 "DepartmentId"）
    /// </summary>
    public string DepartmentField { get; set; } = "DepartmentId";

    /// <summary>
    /// 创建者字段名称（默认为 "CreatedBy"）
    /// </summary>
    public string CreatorField { get; set; } = "CreatedBy";

    /// <summary>
    /// 租户字段名称（默认为 "TenantId"）
    /// </summary>
    public string TenantField { get; set; } = "TenantId";

    /// <summary>
    /// 是否启用多租户隔离
    /// </summary>
    public bool EnableTenantFilter { get; set; } = true;

    /// <summary>
    /// 自定义过滤器类型
    /// </summary>
    public Type? CustomFilterType { get; set; }
}

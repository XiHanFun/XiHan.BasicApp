#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacAuthorizationChangedEvent
// Guid:fa6f4df4-c1f2-4861-b171-f9d2fbb5bc3f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 18:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.Caching.Events;

/// <summary>
/// 授权变更类型
/// </summary>
[Flags]
public enum AuthorizationChangeType
{
    /// <summary>
    /// 无变更
    /// </summary>
    None = 0,

    /// <summary>
    /// 权限变更
    /// </summary>
    Permission = 1,

    /// <summary>
    /// 数据范围变更
    /// </summary>
    DataScope = 2,

    /// <summary>
    /// 权限与数据范围均变更
    /// </summary>
    All = Permission | DataScope
}

/// <summary>
/// RBAC 授权变更事件
/// </summary>
public sealed class RbacAuthorizationChangedEvent
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="changeType"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public RbacAuthorizationChangedEvent(long? tenantId, AuthorizationChangeType changeType)
    {
        if (changeType == AuthorizationChangeType.None)
        {
            throw new ArgumentOutOfRangeException(nameof(changeType), "授权变更类型不能为 None");
        }

        TenantId = tenantId;
        ChangeType = changeType;
    }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; }

    /// <summary>
    /// 变更类型
    /// </summary>
    public AuthorizationChangeType ChangeType { get; }

    /// <summary>
    /// 是否包含权限变更
    /// </summary>
    public bool HasPermissionChange => ChangeType.HasFlag(AuthorizationChangeType.Permission);

    /// <summary>
    /// 是否包含数据范围变更
    /// </summary>
    public bool HasDataScopeChange => ChangeType.HasFlag(AuthorizationChangeType.DataScope);
}

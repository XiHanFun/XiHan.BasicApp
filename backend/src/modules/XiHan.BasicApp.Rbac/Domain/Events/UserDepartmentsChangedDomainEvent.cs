#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDepartmentsChangedDomainEvent
// Guid:b98f74fa-cf31-4998-95b2-b2d0506c5ec1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 19:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Events;

/// <summary>
/// 用户部门关系变更事件
/// </summary>
public sealed class UserDepartmentsChangedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="departmentIds">新部门集合</param>
    /// <param name="mainDepartmentId">主部门 ID</param>
    /// <param name="tenantId">租户 ID</param>
    public UserDepartmentsChangedDomainEvent(
        long userId,
        IReadOnlyCollection<long> departmentIds,
        long? mainDepartmentId = null,
        long? tenantId = null)
    {
        UserId = userId;
        DepartmentIds = departmentIds;
        MainDepartmentId = mainDepartmentId;
        TenantId = tenantId;
    }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// 新部门集合
    /// </summary>
    public IReadOnlyCollection<long> DepartmentIds { get; }

    /// <summary>
    /// 主部门 ID
    /// </summary>
    public long? MainDepartmentId { get; }

    /// <summary>
    /// 租户 ID
    /// </summary>
    public long? TenantId { get; }
}

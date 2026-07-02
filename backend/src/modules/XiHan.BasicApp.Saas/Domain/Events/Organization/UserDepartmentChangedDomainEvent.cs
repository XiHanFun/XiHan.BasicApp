#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDepartmentChangedDomainEvent
// Guid:3f8b7c0d-2c9e-4dc6-ab75-6b7c8d9e0f1a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 用户部门归属变更事件
/// </summary>
/// <remarks>
/// 由用户部门归属的分配/撤销/改属路径发布；订阅方：
/// 聊天部门群成员同步（入部门自动进群、移出部门即踢群）。
/// </remarks>
public sealed class UserDepartmentChangedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="departmentId">部门ID</param>
    /// <param name="isAssigned">true=分配进部门；false=移出部门</param>
    public UserDepartmentChangedDomainEvent(long userId, long departmentId, bool isAssigned)
    {
        UserId = userId;
        DepartmentId = departmentId;
        IsAssigned = isAssigned;
    }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public long DepartmentId { get; }

    /// <summary>
    /// true=分配进部门；false=移出部门
    /// </summary>
    public bool IsAssigned { get; }
}

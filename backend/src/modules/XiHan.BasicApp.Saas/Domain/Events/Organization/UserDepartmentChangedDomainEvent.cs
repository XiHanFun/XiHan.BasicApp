// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserCreatedDomainEvent
// Guid:483c1c07-b3da-444a-b5c0-4d5b1b06f06a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:37:07
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Events;

/// <summary>
/// 用户创建事件
/// </summary>
public sealed class UserCreatedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="userName">用户名</param>
    public UserCreatedDomainEvent(long userId, string userName)
    {
        UserId = userId;
        UserName = userName;
    }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; }
}

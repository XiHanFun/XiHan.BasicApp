#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPasswordChangedDomainEvent
// Guid:3ce768d7-e2be-440c-8994-107210c5bc61
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:37:15
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Events;

/// <summary>
/// 用户密码变更事件
/// </summary>
public sealed class UserPasswordChangedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userId">用户 ID</param>
    public UserPasswordChangedDomainEvent(long userId)
    {
        UserId = userId;
    }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; }
}

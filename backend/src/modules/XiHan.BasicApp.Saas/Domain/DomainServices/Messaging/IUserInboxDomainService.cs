#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserInboxDomainService
// Guid:433a85fa-ce28-42dc-99e9-af607258a019
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 用户站内信领域服务
/// </summary>
public interface IUserInboxDomainService
{
    /// <summary>
    /// 投递用户站内信
    /// </summary>
    Task<UserInboxDispatchResult> DispatchToUserAsync(UserInboxDispatchCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 确认通知
    /// </summary>
    Task ConfirmAsync(long userNotificationId, long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记全部已读
    /// </summary>
    Task MarkAllReadAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记已读
    /// </summary>
    Task MarkReadAsync(long userNotificationId, long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记登录后弹窗已展示（仅弹一次，幂等：已弹过不重复写）
    /// </summary>
    Task MarkPopupShownAsync(long userNotificationId, long userId, CancellationToken cancellationToken = default);
}

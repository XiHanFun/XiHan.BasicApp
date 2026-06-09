#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserInboxAppService
// Guid:3178e321-975a-4c1f-a8a3-c8c25a6845f5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 当前用户站内信应用服务接口
/// </summary>
public interface IUserInboxAppService : IApplicationService
{
    /// <summary>
    /// 获取当前用户站内信列表
    /// </summary>
    Task<List<UserInboxItemDto>> GetListAsync(bool unreadOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记已读
    /// </summary>
    Task MarkReadAsync(UserInboxUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记全部已读
    /// </summary>
    Task MarkAllReadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 确认通知
    /// </summary>
    Task ConfirmAsync(UserInboxUpdateDto input, CancellationToken cancellationToken = default);
}

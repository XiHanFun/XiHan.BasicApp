#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:INotificationDomainService
// Guid:80912031-2435-4567-0123-456789abcd03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 通知领域服务接口
/// </summary>
public interface INotificationDomainService
{
    /// <summary>
    /// 创建通知
    /// </summary>
    Task<SysNotification> CreateAsync(SysNotification notification);

    /// <summary>
    /// 更新通知
    /// </summary>
    Task<SysNotification> UpdateAsync(SysNotification notification);

    /// <summary>
    /// 删除通知
    /// </summary>
    Task<bool> DeleteAsync(long id);
}

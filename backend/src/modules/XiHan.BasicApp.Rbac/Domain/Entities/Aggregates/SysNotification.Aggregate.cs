#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotification.Aggregate
// Guid:1e7bceec-d95d-4ce3-97f8-1814ed8607e6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 10:47:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Rbac.Domain.Entities;

/// <summary>
/// 通知聚合领域行为
/// </summary>
public partial class SysNotification
{
    /// <summary>
    /// 判断通知是否过期
    /// </summary>
    /// <param name="now"></param>
    /// <returns></returns>
    public bool IsExpired(DateTimeOffset? now = null)
    {
        if (!ExpireTime.HasValue)
        {
            return false;
        }

        return ExpireTime.Value <= (now ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 判断用户是否可访问当前通知
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public bool CanAccess(long userId)
    {
        if (userId <= 0)
        {
            return false;
        }

        return IsGlobal || RecipientUserId == userId;
    }

    /// <summary>
    /// 标记为已读
    /// </summary>
    /// <param name="readTime"></param>
    public void MarkRead(DateTimeOffset? readTime = null)
    {
        if (NotificationStatus == NotificationStatus.Deleted)
        {
            throw new BusinessException(message: "已删除通知不允许标记已读");
        }

        NotificationStatus = NotificationStatus.Read;
        ReadTime = readTime ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 标记为已删除
    /// </summary>
    public void MarkDeleted()
    {
        NotificationStatus = NotificationStatus.Deleted;
    }

    /// <summary>
    /// 确认通知
    /// </summary>
    /// <param name="confirmTime"></param>
    public void Confirm(DateTimeOffset? confirmTime = null)
    {
        MarkRead(confirmTime);
        NeedConfirm = false;
    }
}

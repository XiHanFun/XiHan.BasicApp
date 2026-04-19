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

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 通知聚合领域行为
/// </summary>
public partial class SysNotification
{
    /// <summary>
    /// 判断通知是否过期
    /// </summary>
    public bool IsExpired(DateTimeOffset? now = null)
    {
        if (!ExpireTime.HasValue)
        {
            return false;
        }

        return ExpireTime.Value <= (now ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 判断通知是否有效（未删除、未过期）
    /// </summary>
    public bool IsActive(DateTimeOffset? now = null)
    {
        return IsPublished && !IsExpired(now);
    }
}

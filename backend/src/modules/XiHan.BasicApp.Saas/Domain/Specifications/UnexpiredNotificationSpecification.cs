#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UnexpiredNotificationSpecification.cs
// Guid:f4b8d2e5-9a3c-4f7b-e012-c6b9a8f3d5e7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Domain.Specifications;

/// <summary>
/// 通知未过期规约
/// </summary>
public class UnexpiredNotificationSpecification : Specification<SysNotification>
{
    public override Expression<Func<SysNotification, bool>> ToExpression()
    {
        var now = DateTimeOffset.UtcNow;
        return notification => notification.ExpireTime == null || notification.ExpireTime > now;
    }
}

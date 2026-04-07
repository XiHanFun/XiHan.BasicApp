#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserChangedDomainEvent
// Guid:3a4b5c6d-7e8f-4012-cdef-330000000003
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 用户变更领域事件
/// </summary>
public sealed class UserChangedDomainEvent : EntityChangedDomainEvent<long>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public UserChangedDomainEvent(long entityId, long? tenantId = null)
        : base(entityId, tenantId)
    {
    }
}

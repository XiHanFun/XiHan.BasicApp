#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskChangedDomainEvent
// Guid:4a5b6c7d-8e9f-4123-def0-450000000005
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 任务变更领域事件
/// </summary>
public sealed class TaskChangedDomainEvent : EntityChangedDomainEvent<long>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskChangedDomainEvent(long entityId, long? tenantId = null)
        : base(entityId, tenantId)
    {
    }
}

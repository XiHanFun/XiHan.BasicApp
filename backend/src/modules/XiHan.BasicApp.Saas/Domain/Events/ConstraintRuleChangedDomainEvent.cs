#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRuleChangedDomainEvent
// Guid:a1b2c3d4-5e6f-7890-abcd-ef12345678c1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 约束规则变更领域事件
/// </summary>
public sealed class ConstraintRuleChangedDomainEvent : EntityChangedDomainEvent<long>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ConstraintRuleChangedDomainEvent(long entityId, long? tenantId = null)
        : base(entityId, tenantId)
    {
    }
}

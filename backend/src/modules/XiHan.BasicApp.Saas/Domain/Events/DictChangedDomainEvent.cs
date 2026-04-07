#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictChangedDomainEvent
// Guid:2a3b4c5d-6e7f-8901-abcd-ef0123456705
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 字典变更领域事件
/// </summary>
public sealed class DictChangedDomainEvent : EntityChangedDomainEvent<long>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public DictChangedDomainEvent(long entityId, string? dictCode = null)
        : base(entityId)
    {
        DictCode = dictCode;
    }

    /// <summary>
    /// 字典编码
    /// </summary>
    public string? DictCode { get; }
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileChangedDomainEvent
// Guid:6e7f8091-0213-2345-ef01-23456789ab05
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 文件变更领域事件
/// </summary>
public sealed class FileChangedDomainEvent : EntityChangedDomainEvent<long>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public FileChangedDomainEvent(long entityId)
        : base(entityId)
    {
    }
}

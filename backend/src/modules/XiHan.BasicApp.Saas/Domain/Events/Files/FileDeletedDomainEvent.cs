#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileDeletedDomainEvent
// Guid:b081a8b9-938f-4e26-9d51-4ae9f2fd3f5a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 文件删除事件
/// </summary>
public sealed class FileDeletedDomainEvent(
    long tenantId,
    long fileId,
    string fileName,
    bool physicalDeleted,
    long? operatorUserId,
    string? reason = null)
    : SaasDomainEventBase(tenantId, operatorUserId, reason)
{
    /// <summary>
    /// 文件主键
    /// </summary>
    public long FileId { get; } = fileId;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; } = fileName;

    /// <summary>
    /// 是否已执行物理删除
    /// </summary>
    public bool PhysicalDeleted { get; } = physicalDeleted;
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

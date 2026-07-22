// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 文件主存储切换事件
/// </summary>
public sealed class FilePrimaryStorageChangedDomainEvent(
    long tenantId,
    long fileId,
    long storageId,
    long? previousStorageId,
    long? operatorUserId,
    string? reason = null)
    : SaasDomainEventBase(tenantId, operatorUserId, reason)
{
    /// <summary>
    /// 文件主键
    /// </summary>
    public long FileId { get; } = fileId;

    /// <summary>
    /// 新主存储主键
    /// </summary>
    public long StorageId { get; } = storageId;

    /// <summary>
    /// 原主存储主键
    /// </summary>
    public long? PreviousStorageId { get; } = previousStorageId;
}

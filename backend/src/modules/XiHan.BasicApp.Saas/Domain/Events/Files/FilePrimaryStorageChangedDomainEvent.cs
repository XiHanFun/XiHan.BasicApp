#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FilePrimaryStorageChangedDomainEvent
// Guid:1cc4d119-03dc-4eaa-b58c-f25657cb9e74
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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

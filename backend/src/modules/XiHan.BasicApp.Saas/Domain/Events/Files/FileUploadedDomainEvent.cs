// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 文件上传完成事件
/// </summary>
public sealed class FileUploadedDomainEvent(
    long tenantId,
    long fileId,
    long storageId,
    string fileName,
    long fileSize,
    long? operatorUserId,
    string? reason = null)
    : SaasDomainEventBase(tenantId, operatorUserId, reason)
{
    /// <summary>
    /// 文件主键
    /// </summary>
    public long FileId { get; } = fileId;

    /// <summary>
    /// 存储主键
    /// </summary>
    public long StorageId { get; } = storageId;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; } = fileName;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; } = fileSize;
}

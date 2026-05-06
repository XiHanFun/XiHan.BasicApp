#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFileStorageDomainService
// Guid:e9e9c7e2-a2f5-4dd4-9c21-8f25d9c1849d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 文件存储领域服务
/// </summary>
public interface IFileStorageDomainService
{
    /// <summary>
    /// 规范上传文件原始名称
    /// </summary>
    string NormalizeOriginalName(string originalName);

    /// <summary>
    /// 生成租户内唯一的系统文件名
    /// </summary>
    string BuildStoredFileName(string originalName, string fileHash);

    /// <summary>
    /// 生成对象存储路径
    /// </summary>
    string BuildStoragePath(string fileName, string? directory, DateTimeOffset now);

    /// <summary>
    /// 根据扩展名和 MIME 推断文件类型
    /// </summary>
    FileType ResolveFileType(string? extension, string? mimeType);

    /// <summary>
    /// 根据对象存储提供商解析存储类型
    /// </summary>
    FileStorageType ResolveStorageType(string providerName);

    /// <summary>
    /// 解析对象存储访问控制
    /// </summary>
    string ResolveAccessControl(ResourceAccessLevel accessLevel, string? accessControl);

    /// <summary>
    /// 校验上传元数据
    /// </summary>
    void EnsureUploadMetadata(long fileSize, bool isTemporary, DateTimeOffset? expiresAt, int retentionDays);

    /// <summary>
    /// 校验存储副本可作为主存储
    /// </summary>
    void EnsureCanBecomePrimary(SysFileStorage storage);
}

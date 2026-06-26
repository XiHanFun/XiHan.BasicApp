#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFileDomainService
// Guid:97d4a3dd-e19c-46a3-a064-d769a744c4cf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 文件领域服务
/// </summary>
public interface IFileDomainService
{
    /// <summary>
    /// 删除文件
    /// </summary>
    Task<FileDeleteCommandResult> DeleteFileAsync(FileDeleteCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加文件下载次数（自增 DownloadCount 并记录最后下载时间）
    /// </summary>
    Task IncrementDownloadCountAsync(long fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加文件访问次数（自增 ViewCount 并记录最后访问时间）
    /// </summary>
    Task IncrementViewCountAsync(long fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 秒传文件
    /// </summary>
    Task<FileCommandResult> FastUploadFileAsync(FileFastUploadCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 切换主存储
    /// </summary>
    Task<FilePrimaryStorageSwitchCommandResult> SwitchPrimaryStorageAsync(FilePrimaryStorageSwitchCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新文件业务元数据
    /// </summary>
    Task<FileCommandResult> UpdateFileMetadataAsync(FileMetadataUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新文件状态
    /// </summary>
    Task<FileCommandResult> UpdateFileStatusAsync(FileStatusUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新文件存储状态
    /// </summary>
    Task<FileStorageCommandResult> UpdateFileStorageStatusAsync(FileStorageStatusUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建上传中文件
    /// </summary>
    Task<FileCommandResult> CreateUploadingFileAsync(FileCreateUploadingCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记上传失败
    /// </summary>
    Task<FileCommandResult> MarkUploadFailedAsync(FileUploadFailedCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 完成文件上传
    /// </summary>
    Task<FileUploadCommandResult> CompleteUploadAsync(FileUploadCompleteCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验文件存储副本
    /// </summary>
    Task<FileStorageCommandResult> VerifyFileStorageAsync(FileStorageVerifyCommand command, CancellationToken cancellationToken = default);
}

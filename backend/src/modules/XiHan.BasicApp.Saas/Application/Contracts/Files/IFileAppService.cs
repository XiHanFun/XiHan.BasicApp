#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFileAppService
// Guid:5c50c7c9-aee7-4b8c-b8d2-7af0783e1be5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

public interface IFileAppService : IApplicationService
{
    Task<FileDetailDto> UploadFileAsync(FileUploadDto input, CancellationToken cancellationToken = default);

    Task<FileDetailDto> FastUploadFileAsync(FileFastUploadDto input, CancellationToken cancellationToken = default);

    Task<FileDetailDto> UpdateFileMetadataAsync(FileMetadataUpdateDto input, CancellationToken cancellationToken = default);

    Task<FileDetailDto> UpdateFileStatusAsync(FileStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task<FileStorageDetailDto> SwitchPrimaryStorageAsync(FilePrimaryStorageSwitchDto input, CancellationToken cancellationToken = default);

    Task<FileStorageDetailDto> VerifyFileStorageAsync(FileStorageVerifyDto input, CancellationToken cancellationToken = default);

    Task<FileStorageDetailDto> UpdateFileStorageStatusAsync(FileStorageStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(FileDeleteDto input, CancellationToken cancellationToken = default);

    Task<Stream> DownloadFileAsync(long fileId, CancellationToken cancellationToken = default);

    Task<string> GenerateFilePresignedUrlAsync(long fileId, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default);
}

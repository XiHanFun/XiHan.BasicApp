// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

public interface IFileAppService : IApplicationService
{
    Task<FileDetailDto> UploadFileAsync(FileUploadDto input, CancellationToken cancellationToken = default);

    Task<FileDetailDto?> FastUploadFileAsync(FileFastUploadDto input, CancellationToken cancellationToken = default);

    Task<FileDetailDto> UpdateFileMetadataAsync(FileMetadataUpdateDto input, CancellationToken cancellationToken = default);

    Task<FileDetailDto> UpdateFileStatusAsync(FileStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task<FileStorageDetailDto> SwitchPrimaryStorageAsync(FilePrimaryStorageSwitchDto input, CancellationToken cancellationToken = default);

    Task<FileStorageDetailDto> VerifyFileStorageAsync(FileStorageVerifyDto input, CancellationToken cancellationToken = default);

    Task<FileStorageDetailDto> UpdateFileStorageStatusAsync(FileStorageStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(FileDeleteDto input, CancellationToken cancellationToken = default);

    Task<Stream> DownloadFileAsync(long fileId, CancellationToken cancellationToken = default);

    Task<string> GenerateFilePresignedUrlAsync(long fileId, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default);
}

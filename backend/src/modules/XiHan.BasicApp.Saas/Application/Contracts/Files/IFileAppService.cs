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
    Task<FileDetailDto> CreateFileAsync(FileCreateDto input, CancellationToken cancellationToken = default);

    Task<FileDetailDto> UpdateFileAsync(FileUpdateDto input, CancellationToken cancellationToken = default);

    Task<FileDetailDto> UpdateFileStatusAsync(FileStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(long id, CancellationToken cancellationToken = default);

    Task<FileStorageDetailDto> CreateFileStorageAsync(FileStorageCreateDto input, CancellationToken cancellationToken = default);

    Task<FileStorageDetailDto> UpdateFileStorageAsync(FileStorageUpdateDto input, CancellationToken cancellationToken = default);

    Task<FileStorageDetailDto> UpdateFileStorageStatusAsync(FileStorageStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteFileStorageAsync(long id, CancellationToken cancellationToken = default);
}

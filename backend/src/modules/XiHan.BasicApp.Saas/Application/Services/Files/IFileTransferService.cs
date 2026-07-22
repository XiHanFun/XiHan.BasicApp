// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 文件传输服务
/// </summary>
public interface IFileTransferService
{
    /// <summary>
    /// 删除物理文件
    /// </summary>
    Task DeletePhysicalAsync(IReadOnlyList<SysFileStorage> storages, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载文件
    /// </summary>
    Task<Stream> DownloadAsync(SysFileStorage storage, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成预签名访问地址
    /// </summary>
    Task<string> GeneratePresignedUrlAsync(SysFileStorage storage, TimeSpan? expiresIn, CancellationToken cancellationToken = default);

    /// <summary>
    /// 探测文件存储副本
    /// </summary>
    Task<FileStorageProbeResult> ProbeStorageAsync(SysFileStorage storage, CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传文件
    /// </summary>
    Task<FileTransferUploadResult> UploadAsync(FileUploadDto input, CancellationToken cancellationToken = default);
}

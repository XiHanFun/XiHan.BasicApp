// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 文件存储探测结果
/// </summary>
public sealed record FileStorageProbeResult(bool Exists, long MetadataSize, string? ExternalUrl);

/// <summary>
/// 文件传输上传结果
/// </summary>
public sealed record FileTransferUploadResult(SysFile File, SysFileStorage? Storage, bool Uploaded, string? Reason);

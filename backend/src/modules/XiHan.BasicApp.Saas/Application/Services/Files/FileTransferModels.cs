#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileTransferModels
// Guid:81d51099-9418-4bb0-a52f-2e21a38ccdf4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileUploadService
// Guid:d6e7f8a9-b0c1-4f2d-e3f4-a5b6c7d8e9f0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/10 10:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Services.Application.Files.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Files;

/// <summary>
/// 文件上传服务接口
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// 上传文件
    /// </summary>
    Task<SysFile> UploadAsync(FileUploadDto fileUploadDto, CancellationToken cancellationToken = default);
}

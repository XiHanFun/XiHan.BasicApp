#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFileAppService
// Guid:7088f63f-093d-4f3e-a47b-a0f89e0e5f60
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:36:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 文件应用服务
/// </summary>
public interface IFileAppService
    : ICrudApplicationService<SysFile, FileDto, long, FileCreateDto, FileUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据哈希获取文件
    /// </summary>
    Task<FileDto?> GetByFileHashAsync(string fileHash, long? tenantId = null);
}

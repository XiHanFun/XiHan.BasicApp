#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUpgradeAppService
// Guid:3a0c2a0b-3b2c-4c48-9c6f-4f1f9b0e9d2c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 18:21:30
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统升级应用服务
/// </summary>
public interface IUpgradeAppService : IApplicationService
{
    /// <summary>
    /// 获取系统版本信息
    /// </summary>
    Task<SystemVersionDto> GetVersionAsync(string? clientVersion = null);

    /// <summary>
    /// 启动升级
    /// </summary>
    Task<UpgradeStartResultDto> StartUpgradeAsync();

    /// <summary>
    /// 获取升级状态
    /// </summary>
    Task<UpgradeStatusDto> GetUpgradeStatusAsync();
}

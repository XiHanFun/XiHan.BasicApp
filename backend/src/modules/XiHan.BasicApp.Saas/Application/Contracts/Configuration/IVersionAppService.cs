#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IVersionAppService
// Guid:0a9bfe54-e955-4800-b770-1050635e07b4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 系统版本命令应用服务接口
/// </summary>
public interface IVersionAppService : IApplicationService
{
    Task<VersionDetailDto> CreateVersionAsync(VersionCreateDto input, CancellationToken cancellationToken = default);

    Task<VersionDetailDto> UpdateVersionAsync(VersionUpdateDto input, CancellationToken cancellationToken = default);

    Task<VersionDetailDto> StartVersionUpgradeAsync(VersionUpgradeStartDto input, CancellationToken cancellationToken = default);

    Task<VersionDetailDto> FinishVersionUpgradeAsync(VersionUpgradeFinishDto input, CancellationToken cancellationToken = default);

    Task DeleteVersionAsync(long id, CancellationToken cancellationToken = default);
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConfigAppService
// Guid:5a8a40b5-0457-44e2-9837-a7fd18b56130
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
/// 系统配置命令应用服务接口
/// </summary>
public interface IConfigAppService : IApplicationService
{
    Task<ConfigDetailDto> CreateConfigAsync(ConfigCreateDto input, CancellationToken cancellationToken = default);

    Task<ConfigDetailDto> UpdateConfigAsync(ConfigUpdateDto input, CancellationToken cancellationToken = default);

    Task<ConfigDetailDto> UpdateConfigStatusAsync(ConfigStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteConfigAsync(long id, CancellationToken cancellationToken = default);
}

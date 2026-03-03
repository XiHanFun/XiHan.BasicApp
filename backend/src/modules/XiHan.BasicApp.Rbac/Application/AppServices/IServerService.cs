#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IServerService
// Guid:a6dc056d-7099-421f-a76a-8c4b50903e4c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 14:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Utils.Diagnostics.HardwareInfos;
using XiHan.Framework.Utils.Runtime;

namespace XiHan.BasicApp.Rbac.Application.AppServices;

/// <summary>
/// 服务器监控服务
/// </summary>
public interface IServerService : IApplicationService
{
    /// <summary>
    /// 获取服务器综合信息
    /// </summary>
    Task<ServerInfoDto> GetServerInfoAsync(
        bool includeDisk = true,
        bool includeNetwork = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取运行时信息
    /// </summary>
    Task<RuntimeInfo> GetRuntimeInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 CPU 信息
    /// </summary>
    Task<CpuInfo> GetCpuInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取内存信息
    /// </summary>
    Task<RamInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default);
}

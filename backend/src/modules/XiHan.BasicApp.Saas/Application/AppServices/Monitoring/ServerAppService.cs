#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ServerAppService
// Guid:ce53f470-6d98-4e89-8ff7-f3ef24fc9bb8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 14:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Dtos.Monitoring;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Utils.Diagnostics.HardwareInfos;
using XiHan.Framework.Utils.Reflections;
using XiHan.Framework.Utils.Runtime;

namespace XiHan.BasicApp.Saas.Application.AppServices.Monitoring;

/// <summary>
/// 系统服务器监控服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class ServerAppService : ApplicationServiceBase
{
    private readonly IServerInfoQueryService _serverInfoQueryService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ServerAppService(IServerInfoQueryService serverInfoQueryService)
    {
        _serverInfoQueryService = serverInfoQueryService;
    }

    /// <summary>
    /// 获取主板信息
    /// </summary>
    public async Task<BoardInfo> GetBoardInfoAsync()
    {
        return await _serverInfoQueryService.GetBoardInfoAsync();
    }

    /// <summary>
    /// 获取 CPU 信息
    /// </summary>
    public async Task<CpuInfo> GetCpuInfoAsync()
    {
        return await _serverInfoQueryService.GetCpuInfoAsync();
    }

    /// <summary>
    /// 获取磁盘信息
    /// </summary>
    public async Task<List<DiskInfo>> GetDiskInfoAsync()
    {
        return await _serverInfoQueryService.GetDiskInfoAsync();
    }

    /// <summary>
    /// 获取 GPU 信息
    /// </summary>
    public async Task<List<GpuInfo>> GetGpuInfoAsync()
    {
        return await _serverInfoQueryService.GetGpuInfoAsync();
    }

    /// <summary>
    /// 获取内存信息
    /// </summary>
    public async Task<RamInfo> GetMemoryInfoAsync()
    {
        return await _serverInfoQueryService.GetMemoryInfoAsync();
    }

    /// <summary>
    /// 获取网卡信息
    /// </summary>
    public async Task<List<NetworkInfo>> GetNetworkInfoAsync()
    {
        return await _serverInfoQueryService.GetNetworkInfoAsync();
    }

    /// <summary>
    /// 获取 NuGet 包信息
    /// </summary>
    public async Task<List<NuGetPackage>> GetNuGetPackagesAsync()
    {
        return await _serverInfoQueryService.GetNuGetPackagesAsync();
    }

    /// <summary>
    /// 获取运行时信息
    /// </summary>
    public async Task<RuntimeInfo> GetRuntimeInfoAsync()
    {
        return await _serverInfoQueryService.GetRuntimeInfoAsync();
    }

    /// <summary>
    /// 获取服务器综合信息
    /// </summary>
    public async Task<ServerInfoDto> GetServerInfoAsync(bool includeDisk = true, bool includeNetwork = true)
    {
        return await _serverInfoQueryService.GetServerInfoAsync(includeDisk, includeNetwork);
    }
}

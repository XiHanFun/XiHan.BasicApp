// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos.Monitoring;
using XiHan.Framework.Utils.Diagnostics.HardwareInfos;
using XiHan.Framework.Utils.Reflections;
using XiHan.Framework.Utils.Runtime;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 服务器信息查询服务
/// </summary>
public interface IServerInfoQueryService
{
    /// <summary>
    /// 获取主板信息
    /// </summary>
    Task<BoardInfo> GetBoardInfoAsync();

    /// <summary>
    /// 获取 CPU 信息
    /// </summary>
    Task<CpuInfo> GetCpuInfoAsync();

    /// <summary>
    /// 获取磁盘信息
    /// </summary>
    Task<List<DiskInfo>> GetDiskInfoAsync();

    /// <summary>
    /// 获取 GPU 信息
    /// </summary>
    Task<List<GpuInfo>> GetGpuInfoAsync();

    /// <summary>
    /// 获取内存信息
    /// </summary>
    Task<RamInfo> GetMemoryInfoAsync();

    /// <summary>
    /// 获取网卡信息
    /// </summary>
    Task<List<NetworkInfo>> GetNetworkInfoAsync();

    /// <summary>
    /// 获取 NuGet 包信息
    /// </summary>
    Task<List<NuGetPackage>> GetNuGetPackagesAsync();

    /// <summary>
    /// 获取运行时信息
    /// </summary>
    Task<RuntimeInfo> GetRuntimeInfoAsync();

    /// <summary>
    /// 获取服务器综合信息
    /// </summary>
    Task<ServerInfoDto> GetServerInfoAsync(bool includeDisk = true, bool includeNetwork = true);
}

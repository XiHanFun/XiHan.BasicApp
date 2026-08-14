// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos.Monitoring;
using XiHan.Framework.Utils.Diagnostics.HardwareInfos;
using XiHan.Framework.Utils.Reflections;
using XiHan.Framework.Utils.Runtime;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 服务器信息查询服务实现
/// </summary>
public sealed class ServerInfoQueryService
    : IServerInfoQueryService
{
    /// <summary>
    /// 获取主板信息
    /// </summary>
    public Task<BoardInfo> GetBoardInfoAsync()
    {
        return Task.FromResult(BoardHelper.BoardInfos);
    }

    /// <summary>
    /// 获取 CPU 信息
    /// </summary>
    public Task<CpuInfo> GetCpuInfoAsync()
    {
        return Task.FromResult(CpuHelper.CpuInfos);
    }

    /// <summary>
    /// 获取磁盘信息
    /// </summary>
    public Task<List<DiskInfo>> GetDiskInfoAsync()
    {
        return Task.FromResult(DiskHelper.DiskInfos);
    }

    /// <summary>
    /// 获取 GPU 信息
    /// </summary>
    public Task<List<GpuInfo>> GetGpuInfoAsync()
    {
        return Task.FromResult(GpuHelper.GpuInfos);
    }

    /// <summary>
    /// 获取内存信息
    /// </summary>
    public Task<RamInfo> GetMemoryInfoAsync()
    {
        return Task.FromResult(RamHelper.RamInfos);
    }

    /// <summary>
    /// 获取网卡信息
    /// </summary>
    public Task<List<NetworkInfo>> GetNetworkInfoAsync()
    {
        return Task.FromResult(NetworkHelper.NetworkInfos);
    }

    /// <summary>
    /// 获取 NuGet 包信息
    /// </summary>
    public Task<List<NuGetPackage>> GetNuGetPackagesAsync()
    {
        return Task.FromResult(ReflectionHelper.GetNuGetPackages("XiHan"));
    }

    /// <summary>
    /// 获取运行时信息
    /// </summary>
    public Task<RuntimeInfo> GetRuntimeInfoAsync()
    {
        return Task.FromResult(OsPlatformHelper.RuntimeInfos);
    }

    /// <summary>
    /// 获取服务器综合信息
    /// </summary>
    public Task<ServerInfoDto> GetServerInfoAsync(bool includeDisk = true, bool includeNetwork = true)
    {
        var result = new ServerInfoDto
        {
            RuntimeInfo = OsPlatformHelper.RuntimeInfos,
            CpuInfo = CpuHelper.CpuInfos,
            MemoryInfo = RamHelper.RamInfos,
            DiskInfos = includeDisk ? [.. DiskHelper.DiskInfos] : [],
            NetworkInfos = includeNetwork ? [.. NetworkHelper.NetworkInfos] : [],
            BoardInfo = BoardHelper.BoardInfos,
            GpuInfos = [.. GpuHelper.GpuInfos],
            CollectedAt = DateTimeOffset.UtcNow
        };

        return Task.FromResult(result);
    }
}

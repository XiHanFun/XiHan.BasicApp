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
    /// <inheritdoc />
    public Task<BoardInfo> GetBoardInfoAsync()
    {
        return Task.FromResult(BoardHelper.BoardInfos);
    }

    /// <inheritdoc />
    public Task<CpuInfo> GetCpuInfoAsync()
    {
        return Task.FromResult(CpuHelper.CpuInfos);
    }

    /// <inheritdoc />
    public Task<List<DiskInfo>> GetDiskInfoAsync()
    {
        return Task.FromResult(DiskHelper.DiskInfos);
    }

    /// <inheritdoc />
    public Task<List<GpuInfo>> GetGpuInfoAsync()
    {
        return Task.FromResult(GpuHelper.GpuInfos);
    }

    /// <inheritdoc />
    public Task<RamInfo> GetMemoryInfoAsync()
    {
        return Task.FromResult(RamHelper.RamInfos);
    }

    /// <inheritdoc />
    public Task<List<NetworkInfo>> GetNetworkInfoAsync()
    {
        return Task.FromResult(NetworkHelper.NetworkInfos);
    }

    /// <inheritdoc />
    public Task<List<NuGetPackage>> GetNuGetPackagesAsync()
    {
        return Task.FromResult(ReflectionHelper.GetNuGetPackages("XiHan"));
    }

    /// <inheritdoc />
    public Task<RuntimeInfo> GetRuntimeInfoAsync()
    {
        return Task.FromResult(OsPlatformHelper.RuntimeInfos);
    }

    /// <inheritdoc />
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

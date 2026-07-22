// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Utils.Diagnostics.HardwareInfos;
using XiHan.Framework.Utils.Runtime;

namespace XiHan.BasicApp.Saas.Application.Dtos.Monitoring;

/// <summary>
/// 服务器监控信息 DTO
/// </summary>
public class ServerInfoDto
{
    /// <summary>
    /// 运行时信息
    /// </summary>
    public RuntimeInfo RuntimeInfo { get; set; } = new();

    /// <summary>
    /// CPU 信息
    /// </summary>
    public CpuInfo CpuInfo { get; set; } = new();

    /// <summary>
    /// 内存信息
    /// </summary>
    public RamInfo MemoryInfo { get; set; } = new();

    /// <summary>
    /// 磁盘信息
    /// </summary>
    public IReadOnlyList<DiskInfo> DiskInfos { get; set; } = [];

    /// <summary>
    /// 网卡信息
    /// </summary>
    public IReadOnlyList<NetworkInfo> NetworkInfos { get; set; } = [];

    /// <summary>
    /// 主板信息
    /// </summary>
    public BoardInfo BoardInfo { get; set; } = new();

    /// <summary>
    /// GPU 信息
    /// </summary>
    public IReadOnlyList<GpuInfo> GpuInfos { get; set; } = [];

    /// <summary>
    /// 采集时间
    /// </summary>
    public DateTimeOffset CollectedAt { get; set; } = DateTimeOffset.UtcNow;
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ServerDto
// Guid:4667cb4f-ddcf-400c-8693-6e94c6df8680
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 14:11:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Utils.Diagnostics.HardwareInfos;
using XiHan.Framework.Utils.Runtime;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

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

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using XiHan.Framework.Web.Core.Clients;
using XiHan.Framework.Web.Core.Extensions.DependencyInjection;
using XiHan.Framework.Web.Core.Options;

namespace XiHan.BasicApp.Web.Core.Tests;

/// <summary>
/// 宿主随附的 ip2region 离线库：IPv4 与 IPv6 两个库都能按宿主配置的路径查到归属地。
/// </summary>
/// <remarks>
/// 登录地址与访问策略的地区条件都靠它；只配 IPv4 库时 IPv6 地址的归属地一直为空，这里钉住两个库都在用。
/// </remarks>
public sealed class IpRegionDatabaseTests
{
    /// <summary>
    /// 国内的 IPv4 与 IPv6 地址解析出「中国」，国外地址解析出英文国名。
    /// </summary>
    [Theory]
    [InlineData("114.114.114.114", "中国")]
    [InlineData("2400:3200::1", "中国")]
    [InlineData("8.8.8.8", "United States")]
    public void Resolve_WithBundledDatabases(string address, string expectedCountry)
    {
        var resolver = new Ip2RegionResolver(
            new EmptyHostingEnvironment { ContentRootPath = FindWebHostDirectory(), WebRootPath = FindWebHostDirectory() },
            new OptionsWrapper<XiHanClientInfoOptions>(new XiHanClientInfoOptions
            {
                Ip2RegionDbPath = "IpDatabases/ip2region_v4.xdb",
                Ip2RegionV6DbPath = "IpDatabases/ip2region_v6.xdb"
            }),
            NullLogger<Ip2RegionResolver>.Instance);

        var region = resolver.Resolve(IPAddress.Parse(address));

        Assert.NotNull(region);
        Assert.Equal(expectedCountry, region.Country);
    }

    private static string FindWebHostDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "main", "XiHan.BasicApp.WebHost");
            if (File.Exists(Path.Combine(candidate, "IpDatabases", "ip2region_v6.xdb")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("找不到 WebHost 目录下的 ip2region 离线库");
    }
}

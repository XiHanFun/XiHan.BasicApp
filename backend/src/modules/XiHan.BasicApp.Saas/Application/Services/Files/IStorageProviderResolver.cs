// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.ObjectStorage;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 上传用的存储路由：提供程序，以及驱动它的存储配置（appsettings 路由时为 null）
/// </summary>
/// <param name="Provider">存储提供程序</param>
/// <param name="StorageConfigId">存储配置主键；文件落库时记下，之后按它读写</param>
public sealed record StorageRoute(IFileStorageProvider Provider, long? StorageConfigId);

/// <summary>
/// 存储提供程序解析器：让 <c>SysStorageConfig</c>（DB 行）在运行时驱动对象存储凭证，
/// 无可用的 DB 配置时回退框架 appsettings 路由
/// </summary>
public interface IStorageProviderResolver
{
    /// <summary>
    /// 解析提供程序名称（沿用框架路由：指定优先，否则 routeKey/默认）
    /// </summary>
    string ResolveProviderName(string? routeKey = null, string? providerName = null);

    /// <summary>
    /// 为上传解析提供程序：当前上下文默认且启用的对象存储配置优先，租户未自配时回退平台默认，都没有时回退 appsettings
    /// </summary>
    Task<StorageRoute> RouteForUploadAsync(string? routeKey, string? providerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为既有文件（下载/删除/探测/预签名）解析提供程序：按文件记下的存储配置取凭证，没有记录的走 appsettings 路由
    /// </summary>
    Task<IFileStorageProvider> RouteForStorageAsync(SysFileStorage storage, CancellationToken cancellationToken = default);
}

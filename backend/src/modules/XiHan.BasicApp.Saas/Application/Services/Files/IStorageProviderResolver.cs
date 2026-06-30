#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IStorageProviderResolver
// Guid:e5f6a7b8-c9d0-4a42-be5f-6a7b8c9d0e1f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.ObjectStorage;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 存储提供程序解析器：让 <c>SysStorageConfig</c>（DB 行）在运行时驱动对象存储凭证，
/// 无匹配的 DB 配置时回退框架 appsettings 路由
/// </summary>
public interface IStorageProviderResolver
{
    /// <summary>
    /// 解析提供程序名称（沿用框架路由：指定优先，否则 routeKey/默认）
    /// </summary>
    string ResolveProviderName(string? routeKey = null, string? providerName = null);

    /// <summary>
    /// 为上传解析提供程序：优先用 DB 默认且启用的对象存储配置（运行时构建凭证），否则回退 appsettings
    /// </summary>
    Task<IFileStorageProvider> RouteForUploadAsync(string? routeKey, string? providerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为既有文件（下载/删除/探测/预签名）解析提供程序：当其提供程序名与 DB 默认配置类型匹配时用 DB 凭证，否则回退 appsettings
    /// </summary>
    Task<IFileStorageProvider> RouteForProviderAsync(string? providerName, CancellationToken cancellationToken = default);
}

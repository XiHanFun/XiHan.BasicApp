// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.Framework.Protocols.Http.OpenApi;

namespace XiHan.BasicApp.Host;

/// <summary>
/// 接口文档的标题、版本与分组。
/// </summary>
/// <remarks>
/// 宿主服务的 <c>/openapi.json</c> 与导出的契约快照用同一份。每个包一组（组名即包名小写），显示名与说明在各包的
/// <c>openapi.groups.&lt;组名&gt;.name</c> / <c>.description</c>；没有操作的普通端点按标签入组（文件上传下载进 files，头像上传进 identity）。
/// 全量文档在 <c>/openapi.json</c>（带 <c>x-tagGroups</c>），各组在 <c>/openapi/&lt;组名&gt;.json</c>（ADR-0052）。
/// </remarks>
public static class BasicAppOpenApi
{
    /// <summary>
    /// 接口文档的选项。
    /// </summary>
    /// <returns>新的一份选项。</returns>
    public static OpenApiOptions Options() => new()
    {
        Title = AppInfo.Name,
        Version = AppInfo.Version,
        Groups =
        [
            Group("identity", 10, ["profile"]),
            Group("access", 20),
            Group("tenancy", 30),
            Group("organization", 40),
            Group("settings", 50),
            Group("files", 60, ["files"]),
            Group("notification", 70),
            Group("auditing", 80),
            Group("maintenance", 85),
            Group("openplatform", 90),
        ],
    };

    private static OpenApiGroup Group(string name, int order, IReadOnlyList<string>? tags = null)
        => new(name, order, [AppInfo.ManifestIdPrefix + name], tags);
}

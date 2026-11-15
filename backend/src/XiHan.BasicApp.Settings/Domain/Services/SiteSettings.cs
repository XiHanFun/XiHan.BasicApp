// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Settings;

namespace XiHan.BasicApp.Settings.Domain.Services;

/// <summary>
/// 站点设置
/// </summary>
/// <remarks>
/// 系统参数 <see cref="ConfigKey"/> 的 JSON 值：平台的是站点设置（没建即 <see cref="Default"/>），租户的同键参数是它的品牌，只写要改的字段，没写的沿用平台的
/// 图片存文件 ID（宿主的 <c>/api/site/assets</c> 上传，平台侧、公开级别，匿名可读）；文字为空即前端用界面文案
/// </remarks>
/// <param name="Name">系统名：侧栏、浏览器标题、登录页与水印</param>
/// <param name="Subtitle">登录页的标语</param>
/// <param name="Description">登录页的说明</param>
/// <param name="Logo">Logo 的文件 ID</param>
/// <param name="Favicon">浏览器图标的文件 ID；为空即用 Logo</param>
/// <param name="LoginBackground">登录页背景图的文件 ID</param>
/// <param name="CopyrightOwner">页脚的版权方</param>
/// <param name="CopyrightUrl">版权方的链接</param>
/// <param name="CopyrightSince">版权起始年份</param>
/// <param name="Icp">备案号</param>
/// <param name="IcpUrl">备案号的链接</param>
public sealed record SiteSettings(
    string? Name = null,
    string? Subtitle = null,
    string? Description = null,
    Guid? Logo = null,
    Guid? Favicon = null,
    Guid? LoginBackground = null,
    string? CopyrightOwner = null,
    string? CopyrightUrl = null,
    string? CopyrightSince = null,
    string? Icp = null,
    string? IcpUrl = null)
{
    /// <summary>
    /// 系统参数键
    /// </summary>
    public const string ConfigKey = "settings.site";

    /// <summary>
    /// 文字字段的长度上限
    /// </summary>
    public const int TextMaxLength = 128;

    /// <summary>
    /// 说明与链接的长度上限
    /// </summary>
    public const int LongTextMaxLength = 512;

    /// <summary>
    /// 平台的缺省值
    /// </summary>
    public static SiteSettings Default { get; } = new(Name: AppInfo.Name, CopyrightOwner: "XiHanFun", CopyrightUrl: "https://www.xihanfun.com", CopyrightSince: "2016");

    /// <summary>
    /// 内建参数的元数据
    /// </summary>
    public static ConfigDescriptor Config { get; } = new(
        ConfigKey,
        "站点设置",
        "系统设置",
        ConfigDataType.Json,
        """{"name":"XiHan.BasicApp","copyrightOwner":"XiHanFun","copyrightUrl":"https://www.xihanfun.com","copyrightSince":"2016"}""",
        "name：系统名；subtitle / description：登录页的标语与说明；logo / favicon / loginBackground：图片的文件 ID（在站点设置页上传）；copyrightOwner / copyrightUrl / copyrightSince / icp / icpUrl：页脚的版权与备案。租户的同键参数是它的品牌，只写要改的字段。文字最长 128（说明与链接 512），链接须是 http(s)，写错的值保存时即拒绝",
        AllowTenantOverride: true,
        Order: 10,
        Accepts: static json => Parse(json) is not null);

    /// <summary>
    /// 取值是否可用：长度不超限，链接是 http(s) 的绝对地址
    /// </summary>
    public bool IsWithinRange()
        => Fits(Name, TextMaxLength) && Fits(Subtitle, TextMaxLength) && Fits(Description, LongTextMaxLength)
            && Fits(CopyrightOwner, TextMaxLength) && Fits(CopyrightSince, TextMaxLength) && Fits(Icp, TextMaxLength)
            && IsLink(CopyrightUrl) && IsLink(IcpUrl);

    /// <summary>
    /// 叠在 <paramref name="baseline"/> 上：本份有值的字段覆盖，没值的沿用
    /// </summary>
    /// <param name="baseline">底下的一层（平台的）</param>
    public SiteSettings Over(SiteSettings baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        return new(
            Name ?? baseline.Name,
            Subtitle ?? baseline.Subtitle,
            Description ?? baseline.Description,
            Logo ?? baseline.Logo,
            Favicon ?? baseline.Favicon,
            LoginBackground ?? baseline.LoginBackground,
            CopyrightOwner ?? baseline.CopyrightOwner,
            CopyrightUrl ?? baseline.CopyrightUrl,
            CopyrightSince ?? baseline.CopyrightSince,
            Icp ?? baseline.Icp,
            IcpUrl ?? baseline.IcpUrl);
    }

    /// <summary>
    /// 去掉首尾空白，空串当作没写
    /// </summary>
    public SiteSettings Trimmed() => this with
    {
        Name = Clean(Name),
        Subtitle = Clean(Subtitle),
        Description = Clean(Description),
        CopyrightOwner = Clean(CopyrightOwner),
        CopyrightUrl = Clean(CopyrightUrl),
        CopyrightSince = Clean(CopyrightSince),
        Icp = Clean(Icp),
        IcpUrl = Clean(IcpUrl),
    };

    /// <summary>
    /// 用到的图片文件
    /// </summary>
    public IEnumerable<Guid> Assets()
    {
        foreach (var id in new[] { Logo, Favicon, LoginBackground })
        {
            if (id is { } value)
            {
                yield return value;
            }
        }
    }

    /// <summary>
    /// 解析参数值
    /// </summary>
    /// <returns>不是 JSON 对象、字段类型不对或取值越界为 <see langword="null"/></returns>
    public static SiteSettings? Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.SiteSettings) is { } parsed && parsed.Trimmed() is var trimmed && trimmed.IsWithinRange() ? trimmed : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 写成参数值（没写的字段不出现）
    /// </summary>
    public string ToJson()
    {
        var node = JsonSerializer.SerializeToNode(this, SettingsJsonContext.Default.SiteSettings)!.AsObject();
        foreach (var name in node.Where(static p => p.Value is null).Select(static p => p.Key).ToList())
        {
            node.Remove(name);
        }

        return node.ToJsonString();
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static bool Fits(string? text, int max) => text is null || text.Length <= max;

    private static bool IsLink(string? text)
        => text is null || (text.Length <= LongTextMaxLength && Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https");
}

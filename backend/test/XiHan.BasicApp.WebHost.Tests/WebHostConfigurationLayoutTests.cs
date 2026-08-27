// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Xml.Linq;

namespace XiHan.BasicApp.WebHost.Tests;

/// <summary>
/// 宿主配置资产与输出拷贝规则测试。
/// </summary>
/// <remarks>
/// WebHost 里真正的 .cs 只有四个，剩下的风险几乎全在非代码资产上：配置写坏了启动即崩、
/// 拷贝规则丢了发布包里就没有升级脚本/本地化/IP 库——而且全都是静默失效，本地跑不到就发现不了。
/// 本类只读文件、不写文件，可并行、可乱序执行。
/// </remarks>
public sealed class WebHostConfigurationLayoutTests
{
    private static readonly JsonDocumentOptions JsoncOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// 两份 appsettings 都是带注释的 JSONC，必须能被配置提供程序解析。
    /// </summary>
    /// <param name="fileName">配置文件名。</param>
    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    public void AppSettings_ShouldBeParsableJsonWithComments(string fileName)
    {
        var path = Path.Combine(WebHostTestHelper.ResolveWebHostProjectRoot(), fileName);
        Assert.True(File.Exists(path), $"配置文件不存在：{path}");

        // File.ReadAllText 会自动剥掉 BOM；一个逗号写错就是启动期崩溃
        using var document = JsonDocument.Parse(File.ReadAllText(path), JsoncOptions);

        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
    }

    /// <summary>
    /// 升级脚本根目录配置必须与 WebHost 下真实存在的目录名对得上。
    /// </summary>
    /// <remarks>
    /// 仓库历史上已经因为目录布局对不上让四个升级脚本从未执行过一次，且全程无任何报错。
    /// </remarks>
    /// <param name="fileName">配置文件名。</param>
    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    public void AppSettings_MigrationsRootPathShouldMatchRealDirectory(string fileName)
    {
        var root = WebHostTestHelper.ResolveWebHostProjectRoot();
        using var document = ReadConfiguration(fileName);

        var configured = ReadStringPath(document.RootElement, "XiHan", "Upgrade", "MigrationsRootPath");
        Assert.False(string.IsNullOrWhiteSpace(configured), $"{fileName} 缺少 XiHan:Upgrade:MigrationsRootPath。");

        var directory = Path.Combine(root, configured!.Replace('\\', '/'));
        Assert.True(
            Directory.Exists(directory),
            $"{fileName} 配置的升级脚本目录在工程里不存在：{configured}（升级会静默跳过）。");
    }

    /// <summary>
    /// 雪花算法 WorkerId 必须存在且为整数：升级逻辑靠它判定主节点，键缺失就失去判定依据。
    /// </summary>
    /// <remarks>
    /// 该键当前只出现在 Development 覆盖文件里，因此这里断言「至少一处配置了」并且
    /// 「凡是配置了的地方都必须是整数」，而不是要求每份文件都写。
    /// </remarks>
    [Fact]
    public void AppSettings_SnowflakeWorkerIdShouldBeConfiguredAsInteger()
    {
        var found = 0;

        foreach (var fileName in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            using var document = ReadConfiguration(fileName);
            var element = ResolvePath(document.RootElement, "XiHan", "DistributedIds", "SnowflakeId", "WorkerId");
            if (element is null)
            {
                continue;
            }

            found++;
            Assert.Equal(JsonValueKind.Number, element.Value.ValueKind);
            Assert.True(element.Value.TryGetInt32(out _), $"{fileName} 的 WorkerId 不是整数。");
        }

        Assert.True(found > 0, "两份配置里都没有 XiHan:DistributedIds:SnowflakeId:WorkerId，升级主节点判定会失去依据。");
    }

    /// <summary>
    /// Hosting:Urls 按分号切分后每一段都必须是合法绝对 URI —— Program.cs 直接把它交给 UseUrls，非法值启动即崩。
    /// </summary>
    [Fact]
    public void AppSettings_HostingUrlsShouldBeAbsoluteUris()
    {
        foreach (var fileName in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            using var document = ReadConfiguration(fileName);
            var configured = ReadStringPath(document.RootElement, "Hosting", "Urls");
            if (configured is null)
            {
                // 未配置时 Program.cs 走 Kestrel 默认端口，属于合法状态
                continue;
            }

            var segments = configured.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.NotEmpty(segments);
            Assert.All(segments, segment => Assert.True(
                Uri.TryCreate(segment, UriKind.Absolute, out _),
                $"{fileName} 的 Hosting:Urls 含非法地址段：{segment}"));
        }
    }

    /// <summary>
    /// 会打进生产镜像的基础 appsettings.json 里不得出现 JWT 签名密钥明文。
    /// </summary>
    /// <remarks>
    /// Development 覆盖文件里的开发占位值不受此约束，它不进生产镜像。
    /// </remarks>
    [Fact]
    public void BaseAppSettings_ShouldNotContainPlainJwtSecret()
    {
        using var document = ReadConfiguration("appsettings.json");

        var secret = ResolvePath(document.RootElement, "XiHan", "Authentication", "Jwt", "SecretKey");

        Assert.True(secret is null, "基础 appsettings.json 出现了 JWT SecretKey 明文，会随生产镜像一起发布。");
    }

    /// <summary>
    /// 升级脚本必须随输出拷贝：迁移执行器从应用基目录读脚本，规则一丢发布包里就没有脚本、升级静默跳过。
    /// </summary>
    /// <param name="itemPath">csproj 中 None Update 的路径。</param>
    [Theory]
    [InlineData("UpdateScripts\\**\\*.sql")]
    [InlineData("Localization\\**\\*.json")]
    [InlineData("IpDatabases\\ip2region_v4.xdb")]
    [InlineData("IpDatabases\\ip2region_v6.xdb")]
    public void Csproj_ShouldKeepCopyToOutputRuleForRuntimeAssets(string itemPath)
    {
        var copyRule = ReadCopyToOutputDirectory(itemPath);

        Assert.False(
            string.IsNullOrWhiteSpace(copyRule),
            $"csproj 丢失了 {itemPath} 的输出拷贝规则，发布环境会缺失该资产且不报任何错。");
    }

    /// <summary>
    /// IP 归属地库必须每次都覆盖拷贝：库文件更新后若不覆盖，登录日志的地域解析会一直用旧库。
    /// </summary>
    /// <param name="itemPath">csproj 中 None Update 的路径。</param>
    [Theory]
    [InlineData("IpDatabases\\ip2region_v4.xdb")]
    [InlineData("IpDatabases\\ip2region_v6.xdb")]
    public void Csproj_IpDatabasesShouldAlwaysCopy(string itemPath)
    {
        Assert.Equal("Always", ReadCopyToOutputDirectory(itemPath), StringComparer.Ordinal);
    }

    /// <summary>
    /// csproj 声明要拷贝的资产必须在工程里真实存在，否则规则只是一句空话。
    /// </summary>
    [Fact]
    public void Csproj_DeclaredRuntimeAssetsShouldExistOnDisk()
    {
        var root = WebHostTestHelper.ResolveWebHostProjectRoot();

        Assert.True(Directory.Exists(Path.Combine(root, "UpdateScripts")), "UpdateScripts 目录缺失。");
        Assert.True(Directory.Exists(Path.Combine(root, "Localization")), "Localization 目录缺失。");
        Assert.True(File.Exists(Path.Combine(root, "IpDatabases", "ip2region_v4.xdb")), "IPv4 归属地库缺失。");
        Assert.True(File.Exists(Path.Combine(root, "IpDatabases", "ip2region_v6.xdb")), "IPv6 归属地库缺失。");
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(root, "UpdateScripts"), "*.sql", SearchOption.AllDirectories));
    }

    /// <summary>
    /// 读取 csproj 中指定 None Update 项的输出拷贝设置。
    /// </summary>
    /// <param name="itemPath">None Update 的路径值。</param>
    /// <returns>拷贝设置值；项不存在时返回 null。</returns>
    private static string? ReadCopyToOutputDirectory(string itemPath)
    {
        var csprojPath = Path.Combine(
            WebHostTestHelper.ResolveWebHostProjectRoot(), "XiHan.BasicApp.WebHost.csproj");
        Assert.True(File.Exists(csprojPath), $"未找到被测工程文件：{csprojPath}");

        // csproj 无 xml 命名空间，XName 直接用元素名
        var document = XDocument.Load(csprojPath);

        return document.Descendants("None")
            .Where(element => string.Equals(element.Attribute("Update")?.Value, itemPath, StringComparison.Ordinal))
            .Select(element => element.Element("CopyToOutputDirectory")?.Value)
            .FirstOrDefault();
    }

    /// <summary>
    /// 读取并解析指定配置文件。
    /// </summary>
    /// <param name="fileName">配置文件名。</param>
    /// <returns>已解析的 JSON 文档。</returns>
    private static JsonDocument ReadConfiguration(string fileName)
    {
        var path = Path.Combine(WebHostTestHelper.ResolveWebHostProjectRoot(), fileName);
        Assert.True(File.Exists(path), $"配置文件不存在：{path}");

        return JsonDocument.Parse(File.ReadAllText(path), JsoncOptions);
    }

    /// <summary>
    /// 按层级路径取出 JSON 节点。
    /// </summary>
    /// <param name="root">根节点。</param>
    /// <param name="path">层级键名。</param>
    /// <returns>命中的节点；任一层缺失时返回 null。</returns>
    private static JsonElement? ResolvePath(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var key in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(key, out var next))
            {
                return null;
            }

            current = next;
        }

        return current;
    }

    /// <summary>
    /// 按层级路径取出字符串配置值。
    /// </summary>
    /// <param name="root">根节点。</param>
    /// <param name="path">层级键名。</param>
    /// <returns>字符串值；缺失或非字符串时返回 null。</returns>
    private static string? ReadStringPath(JsonElement root, params string[] path)
    {
        var element = ResolvePath(root, path);

        return element?.ValueKind == JsonValueKind.String ? element.Value.GetString() : null;
    }
}

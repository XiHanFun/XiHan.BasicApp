// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using XiHan.BasicApp.Web.Core.Upgrade;

namespace XiHan.BasicApp.Web.Core.Tests;

/// <summary>
/// 维护模式放行清单测试：穷举维护期间哪些路径可以绕过 503 闸门。
/// </summary>
/// <remarks>
/// 源码注释显式写明两个坑：健康检查被拦会让编排系统误判实例不健康而反复重启；
/// OIDC 发现文档与公钥集被拦会让维护窗口内所有已签发令牌集体验签失败。
/// 本类用 [Theory] 把放行面钉死，任何扩大或收紧都必须先改红这里并经人工复核。
/// </remarks>
public sealed class MaintenanceModeAllowListTests
{
    /// <summary>
    /// 维护期间放行/拦截的路径判定必须与下表逐条一致。
    /// </summary>
    /// <param name="path">请求路径；null 表示 default(PathString)（Value 为 null 的退化值）</param>
    /// <param name="shouldPass">true 表示应当放行（请求交给下游），false 表示应当被拦为 503</param>
    [Theory]
    // 健康检查：精确命中与子段命中，编排系统探活依赖这些路径
    [InlineData("/health", true)]
    [InlineData("/health/", true)]
    [InlineData("/health/live", true)]
    [InlineData("/health/ready", true)]
    // 大小写不敏感（OrdinalIgnoreCase）：大小写变体同样是探活地址，被拦一样翻车
    [InlineData("/Health", true)]
    [InlineData("/HEALTH", true)]
    [InlineData("/HealthZ", true)]
    // ASP.NET 常见探活约定：只有裸 string.StartsWith 能命中，StartsWithSegments 对它们为 false
    [InlineData("/healthz", true)]
    [InlineData("/healthcheck", true)]
    // 当前行为过宽：裸 StartsWith 没有段边界，任何以 /health 开头的路径都被放行。
    // 属已知风险（见 sourceBugs），此处按现状锁定；若日后收紧放行面，这三条应改红。
    [InlineData("/healthy", true)]
    [InlineData("/health-admin/reset", true)]
    [InlineData("/healthzsecret", true)]
    // OIDC 发现文档与公钥集：清单项 "/.well-known/" 带尾斜杠，实际靠裸 string.StartsWith 命中
    [InlineData("/.well-known/", true)]
    [InlineData("/.well-known/openid-configuration", true)]
    [InlineData("/.well-known/jwks.json", true)]
    [InlineData("/.WELL-KNOWN/openid-configuration", true)]
    // 裸 /.well-known（无尾斜杠）当前不放行：清单项带尾斜杠，两条判断都不命中（现状锁定，见 sourceBugs）
    [InlineData("/.well-known", false)]
    // 段边界必须成立：伪造相邻前缀不得绕过闸门
    [InlineData("/.well-knownx", false)]
    [InlineData("/.well-knownx/foo", false)]
    [InlineData("/.well-known-x/foo", false)]
    [InlineData("/.well", false)]
    // health 不在首段时不得放行，否则任意业务路径带上 health 即可绕闸
    [InlineData("/api/health", false)]
    [InlineData("/api/health/live", false)]
    // 业务面与文档面一律拦截
    [InlineData("/", false)]
    [InlineData("/api/x", false)]
    [InlineData("/swagger", false)]
    [InlineData("/scalar", false)]
    [InlineData("/heal", false)]
    [InlineData("/HEA", false)]
    // 空路径与 Value 为 null 的退化路径：既不放行，也不得抛 NullReferenceException
    [InlineData("", false)]
    [InlineData(null, false)]
    public async Task InvokeAsync_ShouldMatchAllowList(string? path, bool shouldPass)
    {
        var allowed = await MaintenanceModeTestHelper.IsPathAllowedAsync(path);

        Assert.Equal(shouldPass, allowed);
    }

    /// <summary>
    /// 空路径与 default(PathString) 必须落到 503 分支，而不是抛异常返回 500：
    /// 源码靠 <c>path.Value?.StartsWith(...) == true</c> 的空条件运算符兜底。
    /// </summary>
    /// <param name="path">请求路径；null 表示 default(PathString)</param>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task InvokeAsync_EmptyOrNullPathShouldReturn503WithoutThrowing(string? path)
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var context = MaintenanceModeTestHelper.CreateContext(path);

        var nextCalls = await MaintenanceModeTestHelper.InvokeMiddlewareAsync(state, context);

        Assert.Equal(0, nextCalls);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    /// <summary>
    /// 实测锁定：StartsWithSegments 分支被裸 string.StartsWith 完全包含，对放行结果没有任何独立贡献。
    /// PathString.StartsWithSegments 并不裁掉「清单前缀」一侧的尾斜杠，它等价于
    /// 「裸前缀匹配 + 段边界」，因此永远是裸前缀匹配的子集，源码注释想要的段边界约束被 OR 完全抵消。
    /// 这条把「放行判定实际等价于裸前缀匹配」这一现状钉死（见 sourceBugs BUG-1）。
    /// </summary>
    /// <param name="rawPath">用于对照两条判断分支的路径</param>
    /// <param name="prefix">放行清单前缀</param>
    [Theory]
    [InlineData("/.well-known", "/.well-known/")]
    [InlineData("/.well-known/", "/.well-known/")]
    [InlineData("/.well-known/openid-configuration", "/.well-known/")]
    [InlineData("/health", "/health")]
    [InlineData("/health/", "/health")]
    [InlineData("/health/live", "/health")]
    [InlineData("/healthz", "/health")]
    [InlineData("/healthy", "/health")]
    [InlineData("/api/health", "/health")]
    public void IsAllowed_SegmentsBranchShouldBeSubsumedByRawStartsWith(string rawPath, string prefix)
    {
        var path = new PathString(rawPath);

        var bySegments = path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase);
        var byRawPrefix = path.Value!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

        // 段边界命中必然蕴含裸前缀命中；反之不成立（/healthz、/healthy 即是缺口）
        Assert.True(!bySegments || byRawPrefix);
    }

    /// <summary>
    /// 裸 /.well-known（不带尾斜杠）当前会被拦成 503：清单项写作 "/.well-known/"，
    /// 而 StartsWithSegments 不裁掉前缀侧尾斜杠，两条判断都落空。现状锁定，收紧或放开都必须显式改这条。
    /// </summary>
    [Fact]
    public async Task IsAllowed_BareWellKnownShouldCurrentlyBeBlocked()
    {
        var path = new PathString("/.well-known");
        const string Prefix = "/.well-known/";

        Assert.False(path.StartsWithSegments(Prefix, StringComparison.OrdinalIgnoreCase));
        Assert.False(path.Value!.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase));
        Assert.False(await MaintenanceModeTestHelper.IsPathAllowedAsync("/.well-known"));
    }

    /// <summary>
    /// 裸 string.StartsWith 分支是唯一载荷：/healthz、/healthcheck 只有它能命中。
    /// StartsWithSegments("/health") 要求下一字符是 '/' 或串尾，对它们为 false。
    /// 删掉 string.StartsWith 分支这条即红。
    /// </summary>
    /// <param name="rawPath">以 /health 开头但不构成段边界的路径</param>
    [Theory]
    [InlineData("/healthz")]
    [InlineData("/healthcheck")]
    public async Task IsAllowed_HealthzShouldRelyOnRawStartsWith(string rawPath)
    {
        var path = new PathString(rawPath);
        const string Prefix = "/health";

        Assert.False(path.StartsWithSegments(Prefix, StringComparison.OrdinalIgnoreCase));
        Assert.StartsWith(Prefix, path.Value, StringComparison.OrdinalIgnoreCase);
        Assert.True(await MaintenanceModeTestHelper.IsPathAllowedAsync(rawPath));
    }

    /// <summary>
    /// 放行清单内容必须恰为两项：任何人往清单里加一条都必须先改红本测试并经人工复核，
    /// 防止业务面被悄悄放进维护窗口。
    /// </summary>
    [Fact]
    public void AllowedPathPrefixes_ShouldBeExactlyHealthAndWellKnown()
    {
        var prefixes = MaintenanceModeTestHelper.GetAllowedPathPrefixes();

        Assert.Equal(["/health", "/.well-known/"], prefixes);
    }
}

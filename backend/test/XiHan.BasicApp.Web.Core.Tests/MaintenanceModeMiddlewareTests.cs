// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;
using System.Text.Json;
using XiHan.BasicApp.Web.Core.Upgrade;

namespace XiHan.BasicApp.Web.Core.Tests;

/// <summary>
/// 维护模式中间件测试：锁定 503 拦截契约与常态透传行为。
/// </summary>
/// <remarks>
/// 该中间件注册在管线最前端，维护期间它是唯一的闸门。契约面共四项：
/// 状态码 503、Retry-After: 30、application/json; charset=utf-8、以及 code/message 两字段的 JSON 体。
/// </remarks>
public sealed class MaintenanceModeMiddlewareTests
{
    /// <summary>
    /// 未进入维护模式时，任意路径都必须原样透传，且不得改动状态码、响应头与响应体。
    /// </summary>
    /// <param name="path">请求路径</param>
    [Theory]
    [InlineData("/api/x")]
    [InlineData("/")]
    [InlineData("/health")]
    [InlineData("/swagger/index.html")]
    public async Task InvokeAsync_InactiveShouldPassThroughUntouched(string path)
    {
        var state = new MaintenanceModeState();
        var context = MaintenanceModeTestHelper.CreateContext(path);

        var nextCalls = await MaintenanceModeTestHelper.InvokeMiddlewareAsync(state, context);

        Assert.Equal(1, nextCalls);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("Retry-After"));
        Assert.Null(context.Response.ContentType);
        Assert.Equal(string.Empty, await MaintenanceModeTestHelper.ReadResponseBodyAsync(context));
    }

    /// <summary>
    /// 维护期间业务请求绝不能进入后续管线：源码注释「维护期间的请求不必再走后续管线」意在
    /// 避免请求打到正在迁移的数据库上。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ActiveBusinessPathShouldNeverCallNext()
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var next = new Mock<RequestDelegate>();
        _ = next.Setup(value => value(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        var middleware = new MaintenanceModeMiddleware(next.Object, state);
        var context = MaintenanceModeTestHelper.CreateContext("/api/order/create");

        await middleware.InvokeAsync(context);

        next.Verify(value => value(It.IsAny<HttpContext>()), Times.Never);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    /// <summary>
    /// 拦截响应的四项对外契约必须同时成立：503、Retry-After: 30、JSON 内容类型、UTF-8 字符集。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ActiveShouldWriteFullResponseContract()
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var context = MaintenanceModeTestHelper.CreateContext("/api/order/create");

        _ = await MaintenanceModeTestHelper.InvokeMiddlewareAsync(state, context);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal("30", context.Response.Headers["Retry-After"]);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
    }

    /// <summary>
    /// 响应体必须是合法 JSON，业务码与 HTTP 状态码一致，提示文案是前端要直接展示的中文串。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ActiveShouldWriteJsonPayloadWithCodeAndMessage()
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var context = MaintenanceModeTestHelper.CreateContext("/api/order/create");

        _ = await MaintenanceModeTestHelper.InvokeMiddlewareAsync(state, context);
        var body = await MaintenanceModeTestHelper.ReadResponseBodyAsync(context);
        var (code, message) = MaintenanceModeTestHelper.ParseMaintenancePayload(body);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, code);
        Assert.Equal(MaintenanceModeTestHelper.MaintenanceMessage, message);
    }

    /// <summary>
    /// 中文提示必须能完整往返：JsonSerializer 默认编码器会把非 ASCII 转义成 \uXXXX 写在线上，
    /// 解析后必须还原成原文且不含替换字符 U+FFFD。若日后换成 UnsafeRelaxedJsonEscaping，
    /// 线上字节会变成直出中文，本测试的转义断言会红，从而强制确认编码改动是有意为之。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ActiveChineseMessageShouldRoundTripWithoutMojibake()
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var context = MaintenanceModeTestHelper.CreateContext("/api/order/create");

        _ = await MaintenanceModeTestHelper.InvokeMiddlewareAsync(state, context);
        var body = await MaintenanceModeTestHelper.ReadResponseBodyAsync(context);

        Assert.DoesNotContain("�", body, StringComparison.Ordinal);
        Assert.Contains("\\u", body, StringComparison.Ordinal);
        Assert.All(body, character => Assert.True(character < 128));

        var restored = JsonSerializer.Deserialize<JsonElement>(body).GetProperty("message").GetString();
        Assert.Equal(MaintenanceModeTestHelper.MaintenanceMessage, restored);
    }

    /// <summary>
    /// 响应体写出的字节必须是 UTF-8 编码（WriteAsync 默认 UTF-8），按 UTF-8 解码后与序列化结果逐字相同。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ActiveBodyBytesShouldBeUtf8Encoded()
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var context = MaintenanceModeTestHelper.CreateContext("/api/order/create");

        _ = await MaintenanceModeTestHelper.InvokeMiddlewareAsync(state, context);
        var stream = (MemoryStream)context.Response.Body;
        var decoded = Encoding.UTF8.GetString(stream.ToArray());

        Assert.Equal(
            JsonSerializer.Serialize(new
            {
                code = StatusCodes.Status503ServiceUnavailable,
                message = MaintenanceModeTestHelper.MaintenanceMessage
            }),
            decoded);
    }

    /// <summary>
    /// 维护期间放行路径仍必须交给下游，且状态码不得被改成 503，否则探活与验签在维护窗口内被误杀。
    /// </summary>
    /// <param name="path">放行清单覆盖的路径</param>
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/.well-known/openid-configuration")]
    [InlineData("/.well-known/jwks.json")]
    public async Task InvokeAsync_ActiveAllowedPathShouldStillReachNext(string path)
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var context = MaintenanceModeTestHelper.CreateContext(path);

        var nextCalls = await MaintenanceModeTestHelper.InvokeMiddlewareAsync(state, context);

        Assert.Equal(1, nextCalls);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("Retry-After"));
    }

    /// <summary>
    /// 状态必须每请求实时读取，而不是构造期快照：同一个中间件实例在 Enter 后拦截、Exit 后恢复透传，
    /// 否则退出维护模式需要重启进程。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldReadStatePerRequest()
    {
        var state = new MaintenanceModeState();
        var nextCalls = 0;
        var middleware = new MaintenanceModeMiddleware(
            _ =>
            {
                nextCalls++;
                return Task.CompletedTask;
            },
            state);

        var beforeEnter = MaintenanceModeTestHelper.CreateContext("/api/x");
        await middleware.InvokeAsync(beforeEnter);
        Assert.Equal(1, nextCalls);
        Assert.Equal(StatusCodes.Status200OK, beforeEnter.Response.StatusCode);

        state.Enter();
        var duringMaintenance = MaintenanceModeTestHelper.CreateContext("/api/x");
        await middleware.InvokeAsync(duringMaintenance);
        Assert.Equal(1, nextCalls);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, duringMaintenance.Response.StatusCode);

        state.Exit();
        var afterExit = MaintenanceModeTestHelper.CreateContext("/api/x");
        await middleware.InvokeAsync(afterExit);
        Assert.Equal(2, nextCalls);
        Assert.Equal(StatusCodes.Status200OK, afterExit.Response.StatusCode);
    }

    /// <summary>
    /// 中间件没有 try/catch，下游异常必须原样冒泡，否则非维护期的真实错误会被静默掩盖。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NextExceptionShouldBubbleUp()
    {
        var state = new MaintenanceModeState();
        var middleware = new MaintenanceModeMiddleware(
            _ => throw new InvalidOperationException("下游炸了"),
            state);
        var context = MaintenanceModeTestHelper.CreateContext("/api/x");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Equal("下游炸了", exception.Message);
    }

    /// <summary>
    /// 维护期间对全部 HTTP 方法一视同仁返回 503，写操作不得在迁移期漏进管线。
    /// OPTIONS 预检同样被拦：浏览器侧会先报 CORS 失败而不是拿到 503 提示，属已知取舍（见 sourceBugs）。
    /// </summary>
    /// <param name="method">HTTP 方法</param>
    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task InvokeAsync_ActiveShouldBlockEveryHttpMethod(string method)
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var context = MaintenanceModeTestHelper.CreateContext("/api/order", method);

        var nextCalls = await MaintenanceModeTestHelper.InvokeMiddlewareAsync(state, context);

        Assert.Equal(0, nextCalls);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    /// <summary>
    /// 判定只看 PathString，查询串既不能用来绕过闸门，也不能误伤探活。
    /// </summary>
    /// <param name="path">请求路径</param>
    /// <param name="queryString">查询串（含问号）</param>
    /// <param name="shouldPass">true 表示应当放行</param>
    [Theory]
    [InlineData("/api/x", "?a=1", false)]
    [InlineData("/api/x", "?path=/health", false)]
    [InlineData("/health", "?full=true", true)]
    [InlineData("/.well-known/jwks.json", "?v=2", true)]
    public async Task InvokeAsync_QueryStringShouldNotAffectDecision(string path, string queryString, bool shouldPass)
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var context = MaintenanceModeTestHelper.CreateContext(path, queryString: queryString);

        var nextCalls = await MaintenanceModeTestHelper.InvokeMiddlewareAsync(state, context);

        Assert.Equal(shouldPass ? 1 : 0, nextCalls);
        Assert.Equal(
            shouldPass ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable,
            context.Response.StatusCode);
    }

    /// <summary>
    /// 上游预置的状态码必须被覆盖成 503，避免污染维护响应。
    /// </summary>
    /// <param name="presetStatusCode">进入中间件前已写在响应上的状态码</param>
    [Theory]
    [InlineData(StatusCodes.Status200OK)]
    [InlineData(StatusCodes.Status204NoContent)]
    [InlineData(StatusCodes.Status404NotFound)]
    public async Task InvokeAsync_ActiveShouldOverwritePresetStatusCode(int presetStatusCode)
    {
        var state = new MaintenanceModeState();
        state.Enter();
        var context = MaintenanceModeTestHelper.CreateContext("/api/x");
        context.Response.StatusCode = presetStatusCode;

        _ = await MaintenanceModeTestHelper.InvokeMiddlewareAsync(state, context);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }
}

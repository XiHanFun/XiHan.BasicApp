// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 集成测试用的 S3 兼容服务
/// </summary>
/// <remarks>
/// 端点来自环境变量 <see cref="Variable"/>（如 <c>http://localhost:9000</c>，要认条件写入，如 RustFS），密钥来自 <c>XIHAN_TEST_S3_ACCESS_KEY</c> / <c>XIHAN_TEST_S3_SECRET_KEY</c>
/// （缺省 <c>rustfsadmin</c>，即 RustFS 镜像不设密钥时的根凭据），与框架 S3 测试同一组变量。端点没设即跳过
/// </remarks>
internal static class S3TestService
{
    /// <summary>
    /// 端点的环境变量
    /// </summary>
    public const string Variable = "XIHAN_TEST_S3";

    /// <summary>
    /// 访问密钥 ID
    /// </summary>
    public static string AccessKey => Environment.GetEnvironmentVariable("XIHAN_TEST_S3_ACCESS_KEY") is { Length: > 0 } key ? key : "rustfsadmin";

    /// <summary>
    /// 访问密钥
    /// </summary>
    public static string SecretKey => Environment.GetEnvironmentVariable("XIHAN_TEST_S3_SECRET_KEY") is { Length: > 0 } key ? key : "rustfsadmin";

    /// <summary>
    /// 取端点
    /// </summary>
    /// <remarks>
    /// 没设即跳过用例
    /// </remarks>
    public static Uri RequireEndpoint()
    {
        var endpoint = Environment.GetEnvironmentVariable(Variable);
        Assert.SkipWhen(string.IsNullOrWhiteSpace(endpoint), $"{Variable} is not set; this case only runs against a real S3-compatible service.");
        return new Uri(endpoint!);
    }

    /// <summary>
    /// 直连服务的客户端（路径式访问），用来建删桶、核对对象
    /// </summary>
    public static AmazonS3Client Client(Uri endpoint)
        => new(new BasicAWSCredentials(AccessKey, SecretKey), new AmazonS3Config { ServiceURL = endpoint.ToString(), ForcePathStyle = true, AuthenticationRegion = "us-east-1" });

    /// <summary>
    /// 清空并删除桶
    /// </summary>
    public static async Task DeleteBucketAsync(IAmazonS3 client, string bucket)
    {
        ArgumentNullException.ThrowIfNull(client);
        while (true)
        {
            var listed = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket });
            if (listed.S3Objects is not { Count: > 0 } objects)
            {
                break;
            }

            foreach (var item in objects)
            {
                await client.DeleteObjectAsync(bucket, item.Key);
            }
        }

        await client.DeleteBucketAsync(bucket);
    }
}

/// <summary>
/// 不认条件写入的 S3 兼容服务的替身
/// </summary>
/// <remarks>
/// 在真实服务前转发全部请求（签名依赖的 Host 原样带过去），只把服务对 <c>If-None-Match: *</c> 回的 412 改成 200：
/// 就像服务不看这个条件、照常写入（Garage、Zenko CloudServer 那样）。框架的条件写入探测据此判定服务不认
/// </remarks>
internal sealed class IgnoringConditionsS3Proxy : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly HttpClient _upstream;

    private IgnoringConditionsS3Proxy(WebApplication app, HttpClient upstream, Uri url)
    {
        _app = app;
        _upstream = upstream;
        Url = url;
    }

    /// <summary>
    /// 替身的地址
    /// </summary>
    public Uri Url { get; }

    /// <summary>
    /// 在本机随机端口上起替身
    /// </summary>
    /// <param name="service">真实服务的端点</param>
    public static async Task<IgnoringConditionsS3Proxy> StartAsync(Uri service)
    {
        // 不读测试目录里的 appsettings（宿主的 Kestrel 端点会盖掉这里的地址）
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        var upstream = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false, AutomaticDecompression = DecompressionMethods.None });
        app.Run(context => ForwardAsync(context, upstream, service));
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new IgnoringConditionsS3Proxy(app, upstream, new Uri(address));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        _upstream.Dispose();
    }

    private static async Task ForwardAsync(HttpContext context, HttpClient upstream, Uri service)
    {
        var request = context.Request;
        using var forward = new HttpRequestMessage(new HttpMethod(request.Method), new Uri(service, request.Path.ToUriComponent() + request.QueryString.ToUriComponent()));
        var body = new MemoryStream();
        await request.Body.CopyToAsync(body, context.RequestAborted);
        body.Position = 0;
        if (body.Length > 0 || HttpMethods.IsPut(request.Method) || HttpMethods.IsPost(request.Method))
        {
            forward.Content = new StreamContent(body);
        }

        foreach (var (name, values) in request.Headers)
        {
            if (name is "Host" or "Content-Length" or "Transfer-Encoding" or "Connection" or "Expect")
            {
                continue;
            }

            if (!forward.Headers.TryAddWithoutValidation(name, (IEnumerable<string?>)values))
            {
                forward.Content?.Headers.TryAddWithoutValidation(name, (IEnumerable<string?>)values);
            }
        }

        // SigV4 签了 Host：带上客户端连的那个
        forward.Headers.Host = request.Host.Value;
        using var response = await upstream.SendAsync(forward, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.Headers.ETag = "\"ignored\"";
            return;
        }

        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var (name, values) in response.Headers.Concat(response.Content.Headers))
        {
            if (name is not "Transfer-Encoding")
            {
                context.Response.Headers[name] = values.ToArray();
            }
        }

        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}

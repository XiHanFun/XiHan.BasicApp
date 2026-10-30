// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Files.Jobs;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证分片上传的发起、续传、完成、放弃与过期。
/// </summary>
/// <remarks>
/// 各分片经签名地址直传本地存储（宿主的 <c>/blobs</c> 端点）；完成时按存储列出的分片核对齐全与总大小，缺片或大小不符可补传后再完成，
/// 拼好后回读算 SHA-256，内容与已有的相同即共用已有的对象；只有发起人能看到与操作，已结束的不能再签地址、完成或放弃；
/// 过了时限的按过期回答，过期作业把它记为过期并放弃存储里的分片。
/// </remarks>
public sealed class FileUploadsTests : IDisposable
{
    private const int MiB = 1024 * 1024;

    private readonly SqliteDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-uploads-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private Task<(BasicAppTestServer Server, string Platform, string Tenant)> StartAsync(string lifetime = "01:00:00")
        => SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Configuration.AddInMemoryCollection(
        [
            new("BasicApp:Files:LocalRootPath", _root),
            new("BasicApp:Files:MaxUploadBytes", (32 * MiB).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("BasicApp:Files:UploadPartBytes", (5 * MiB).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("BasicApp:Files:UploadLifetime", lifetime),
        ]));

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, body);
    }

    private static async Task<JsonElement> CreateAsync(BasicAppTestServer server, string token, long size, string name = "big.bin")
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/files:start-upload", JsonSerializer.Serialize(new { originalName = name, size, contentType = "application/octet-stream" }), token);
        Assert.Equal(HttpStatusCode.OK, status);
        return body;
    }

    private static async Task<Dictionary<int, string>> SignAsync(BasicAppTestServer server, string token, string uploadId, params int[] parts)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/files:sign-upload-parts", JsonSerializer.Serialize(new { uploadId, partNumbers = parts }), token);
        Assert.Equal(HttpStatusCode.OK, status);
        return body.GetProperty("parts").EnumerateArray().ToDictionary(static p => p.GetProperty("partNumber").GetInt32(), static p => p.GetProperty("url").GetString()!);
    }

    private static async Task PutAsync(BasicAppTestServer server, string url, ReadOnlyMemory<byte> content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = new ReadOnlyMemoryContent(content) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var response = await server.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
    }

    /// <summary>
    /// 对一次分片上传调动词操作（<c>POST /api/files:&lt;动词&gt;</c>，请求体带 <c>uploadId</c>）。
    /// </summary>
    private static Task<(HttpStatusCode Status, JsonElement Body)> VerbAsync(BasicAppTestServer server, string verb, string uploadId, string token, string partNumbers = "")
        => SendAsync(server, HttpMethod.Post, "/api/files:" + verb, $$"""{"uploadId":"{{uploadId}}"{{(partNumbers.Length == 0 ? string.Empty : ",\"partNumbers\":[" + partNumbers + "]")}}}""", token);

    private static Task<(HttpStatusCode Status, JsonElement Body)> SessionAsync(BasicAppTestServer server, string token, string uploadId)
        => SendAsync(server, HttpMethod.Get, $"/api/files:upload-session?uploadId={uploadId}", null, token);

    private static async Task<List<int>> UploadedAsync(BasicAppTestServer server, string token, string uploadId)
        => [.. (await SessionAsync(server, token, uploadId)).Body.GetProperty("uploadedParts").EnumerateArray().Select(static p => p.GetInt32())];

    private static async Task<string> KeyOfAsync(BasicAppTestServer server, string token, string fileId)
        => Assert.Single((await SendAsync(server, HttpMethod.Get, $"/api/files/{fileId}:storages", null, token)).Body.GetProperty("items").EnumerateArray()).GetProperty("key").GetString()!;

    [Fact]
    public async Task Uploads_GoStraightToStorage_ResumeAndCompleteIntoFiles()
    {
        var (server, platform, tenant) = await StartAsync();
        await using var running = server;
        var content = RandomNumberGenerator.GetBytes((5 * MiB) + 100);

        // 发起：按配置的每片大小切成两片；超过单个文件上限照普通上传拒绝
        var upload = await CreateAsync(server, platform, content.Length);
        var uploadId = upload.GetProperty("id").GetString()!;
        Assert.Equal((5L * MiB, 2, "Uploading"), (upload.GetProperty("partSize").GetInt64(), upload.GetProperty("partCount").GetInt32(), upload.GetProperty("status").GetString()));
        Assert.Equal("FILES_VALIDATION_005", (await SendAsync(server, HttpMethod.Post, "/api/files:start-upload", JsonSerializer.Serialize(new { originalName = "huge.bin", size = 33L * MiB }), platform)).Body.GetProperty("code").GetString());

        // 签地址：编号超出分片数拒绝；签出的是本地存储的签名端点
        var urls = await SignAsync(server, platform, uploadId, 2, 1);
        Assert.All(urls.Values, url => Assert.StartsWith("/blobs/files/platform/", url, StringComparison.Ordinal));
        Assert.Equal("FILES_VALIDATION_015", (await VerbAsync(server, "sign-upload-parts", uploadId, platform, "3")).Body.GetProperty("code").GetString());

        // 续传：先传第二片，完成报缺片，查询看得到已传的；补齐后完成得到文件，内容与 SHA-256 都对
        await PutAsync(server, urls[2], content.AsMemory(5 * MiB));
        Assert.Equal("FILES_CONFLICT_009", (await VerbAsync(server, "complete-upload", uploadId, platform)).Body.GetProperty("code").GetString());
        Assert.Equal([2], await UploadedAsync(server, platform, uploadId));
        await PutAsync(server, urls[1], content.AsMemory(0, 5 * MiB));
        var (completed, file) = await VerbAsync(server, "complete-upload", uploadId, platform);
        Assert.Equal(HttpStatusCode.OK, completed);
        var fileId = file.GetProperty("id").GetString()!;
        Assert.Equal((upload.GetProperty("fileId").GetString(), "big.bin", (long)content.Length, Convert.ToHexStringLower(SHA256.HashData(content)), "upload"), (fileId, file.GetProperty("originalName").GetString(), file.GetProperty("size").GetInt64(), file.GetProperty("hash").GetString(), file.GetProperty("source").GetString()));
        using (var read = new HttpRequestMessage(HttpMethod.Get, $"/api/files/{fileId}/content"))
        {
            read.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platform);
            using var response = await server.Client.SendAsync(read);
            Assert.Equal(content, await response.Content.ReadAsByteArrayAsync());
        }

        // 已完成的：查询报已完成、没有分片，不能再签地址、完成或放弃
        var (_, ended) = await SessionAsync(server, platform, uploadId);
        Assert.Equal(("Completed", 0), (ended.GetProperty("status").GetString(), ended.GetProperty("uploadedParts").GetArrayLength()));
        Assert.Equal("FILES_CONFLICT_008", (await VerbAsync(server, "sign-upload-parts", uploadId, platform, "1")).Body.GetProperty("code").GetString());
        Assert.Equal("FILES_CONFLICT_008", (await VerbAsync(server, "abort-upload", uploadId, platform)).Body.GetProperty("code").GetString());

        // 总大小不符：补传那一片（同一编号覆盖）再完成；内容与已有的相同即共用已有的对象，刚拼好的对象删掉
        var again = (await CreateAsync(server, platform, content.Length, "again.bin")).GetProperty("id").GetString()!;
        var againUrls = await SignAsync(server, platform, again, 1, 2);
        await PutAsync(server, againUrls[1], content.AsMemory(0, 5 * MiB));
        await PutAsync(server, againUrls[2], content.AsMemory(5 * MiB, 50));
        var (mismatch, mismatchBody) = await VerbAsync(server, "complete-upload", again, platform);
        Assert.Equal((HttpStatusCode.Conflict, "FILES_CONFLICT_010"), (mismatch, mismatchBody.GetProperty("code").GetString()));
        await PutAsync(server, againUrls[2], content.AsMemory(5 * MiB));
        var (_, shared) = await VerbAsync(server, "complete-upload", again, platform);
        var sharedId = shared.GetProperty("id").GetString()!;
        Assert.NotEqual(fileId, sharedId);
        Assert.Equal(await KeyOfAsync(server, platform, fileId), await KeyOfAsync(server, platform, sharedId));
        Assert.Single(Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories), f => f.Contains(Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar, StringComparison.Ordinal));

        // 放弃：之后不能再完成；只有发起人看得到（另一侧按 ID 也找不到）
        var dropped = (await CreateAsync(server, platform, content.Length, "dropped.bin")).GetProperty("id").GetString()!;
        await PutAsync(server, (await SignAsync(server, platform, dropped, 1))[1], content.AsMemory(0, 5 * MiB));
        var (aborted, abortedBody) = await VerbAsync(server, "abort-upload", dropped, platform);
        Assert.Equal((HttpStatusCode.OK, "Aborted"), (aborted, abortedBody.GetProperty("status").GetString()));
        Assert.Equal("FILES_CONFLICT_008", (await VerbAsync(server, "complete-upload", dropped, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("FILES_NOTFOUND_006", (await SessionAsync(server, tenant, dropped)).Body.GetProperty("code").GetString());
        await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = "viewer", displayName = "viewer", password = "Pass@123456" }), platform);
        var viewer = (await AuthFlowTests.LoginAsync(server, "viewer", "Pass@123456")).GetProperty("accessToken").GetString()!;
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(server, HttpMethod.Post, "/api/files:start-upload", JsonSerializer.Serialize(new { originalName = "x.bin", size = 10 }), viewer)).Status);
    }

    [Fact]
    public async Task Uploads_PastTheirLifetime_AnswerAsExpired_AndTheJobMarksThem()
    {
        var (server, platform, _) = await StartAsync(lifetime: "00:00:01");
        await using var running = server;
        var uploadId = (await CreateAsync(server, platform, 10)).GetProperty("id").GetString()!;
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // 过了时限：查询按过期回答，不能再签地址
        Assert.Equal("Expired", (await SessionAsync(server, platform, uploadId)).Body.GetProperty("status").GetString());
        Assert.Equal("FILES_CONFLICT_008", (await VerbAsync(server, "sign-upload-parts", uploadId, platform, "1")).Body.GetProperty("code").GetString());

        // 过期作业把它记为过期（有结束时间）；放弃也就不再可能
        var ran = await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var job = ctx.Services.GetRequiredService<FileUploadExpiryJob>();
            return await job.RunAsync(JobRun.Create(FileUploadExpiryJob.Name, DateTimeOffset.UtcNow, JobTrigger.Manual), ctx, ctx.CancellationToken);
        });
        Assert.True(ran.IsSuccess);
        var (_, expired) = await SessionAsync(server, platform, uploadId);
        Assert.Equal(("Expired", JsonValueKind.String), (expired.GetProperty("status").GetString(), expired.GetProperty("endedAt").ValueKind));
        Assert.Equal("FILES_CONFLICT_008", (await VerbAsync(server, "abort-upload", uploadId, platform)).Body.GetProperty("code").GetString());
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 导出中心与导入历史：导出某个列表操作的查询结果（后台以发起人的身份逐页调用，CSV 带 BOM、XLSX 是 OOXML 包），结果存成临时文件经签名地址下载；
/// 发起人没有列表操作的权限即任务失败；任务与导入历史都只看得到自己的；删除任务连同结果文件。
/// </summary>
public sealed class ExportImportTests : IDisposable
{
    private readonly SqliteDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-export-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string? token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, body);
    }

    private static object Submit(string businessType, string format = "Csv", string scope = "SearchResult", string? keyword = null) => new
    {
        businessType,
        format,
        scope,
        taskName = "users-" + format,
        querySnapshot = JsonSerializer.Serialize(new { page = new { index = 1, size = 20 }, filters = Array.Empty<object>(), sorts = Array.Empty<object>(), keyword }),
        columns = new object[] { new { key = "userName", title = "Login" }, new { key = "status", title = "Status", valueMap = new Dictionary<string, string> { ["Active"] = "启用" } } },
    };

    private static async Task<JsonElement> FinishedAsync(BasicAppTestServer server, string token, string taskId)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var (_, task) = await SendAsync(server, HttpMethod.Get, $"/api/export-tasks/{taskId}", null, token);
            if (task.GetProperty("status").GetString() is "Success" or "Failed")
            {
                return task;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Export task {taskId} did not finish.");
    }

    private static async Task<byte[]> DownloadAsync(BasicAppTestServer server, string token, string fileId)
    {
        var link = (await SendAsync(server, HttpMethod.Get, $"/api/files/{fileId}:presign", null, token)).Body.GetProperty("url").GetString()!;
        return await server.Client.GetByteArrayAsync(link);
    }

    [Fact]
    public async Task Exports_RunAsTheRequester_AndImportHistoryStaysPersonal()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Configuration.AddInMemoryCollection([new("BasicApp:Files:LocalRootPath", _root)]));
        await using var running = server;
        foreach (var name in new[] { "exp-ann", "exp-bob", "exp-cat" })
        {
            await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = name, displayName = name, password = "Pass@123456" }), platform);
        }

        // 校验：只能导出列表操作
        Assert.Equal("FILES_VALIDATION_012", (await SendAsync(server, HttpMethod.Post, "/api/export-tasks:submit", JsonSerializer.Serialize(Submit("users.get")), platform)).Body.GetProperty("code").GetString());

        // CSV：按快照里的关键字导搜索结果，带 BOM 与表头，枚举按映射显示
        var (submitted, csvTask) = await SendAsync(server, HttpMethod.Post, "/api/export-tasks:submit", JsonSerializer.Serialize(Submit("users.list", keyword: "exp-")), platform);
        Assert.Equal(HttpStatusCode.OK, submitted);
        Assert.Equal("Pending", csvTask.GetProperty("status").GetString());
        var csvDone = await FinishedAsync(server, platform, csvTask.GetProperty("id").GetString()!);
        Assert.Equal(("Success", 3L, 100), (csvDone.GetProperty("status").GetString(), csvDone.GetProperty("processedCount").GetInt64(), csvDone.GetProperty("progress").GetInt32()));
        var csvBytes = await DownloadAsync(server, platform, csvDone.GetProperty("fileId").GetString()!);
        Assert.Equal([0xEF, 0xBB, 0xBF], csvBytes[..3]);
        var csv = Encoding.UTF8.GetString(csvBytes, 3, csvBytes.Length - 3);
        Assert.StartsWith("Login,Status\r\n", csv, StringComparison.Ordinal);
        Assert.Contains("exp-bob,启用\r\n", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("superadmin", csv, StringComparison.Ordinal);
        Assert.Equal("users-Csv.csv", csvDone.GetProperty("fileName").GetString());

        // XLSX：全部（去掉关键字）也导出，是 zip 包
        var xlsxTask = (await SendAsync(server, HttpMethod.Post, "/api/export-tasks:submit", JsonSerializer.Serialize(Submit("users.list", "Xlsx", "All", keyword: "nobody")), platform)).Body;
        var xlsxDone = await FinishedAsync(server, platform, xlsxTask.GetProperty("id").GetString()!);
        Assert.Equal("Success", xlsxDone.GetProperty("status").GetString());
        Assert.True(xlsxDone.GetProperty("processedCount").GetInt64() >= 4);
        Assert.Equal("PK"u8.ToArray(), (await DownloadAsync(server, platform, xlsxDone.GetProperty("fileId").GetString()!))[..2]);
        Assert.Equal("FILES_CONFLICT_007", (await SendAsync(server, HttpMethod.Post, $"/api/export-tasks/{xlsxDone.GetProperty("id").GetString()}:cancel", "{}", platform)).Body.GetProperty("code").GetString());

        // 发起人没有列表操作的权限：任务失败，原因是那个操作的拒绝；任务只有自己看得到
        await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = "viewer", displayName = "viewer", password = "Pass@123456" }), platform);
        var viewer = (await AuthFlowTests.LoginAsync(server, "viewer", "Pass@123456")).GetProperty("accessToken").GetString()!;
        var deniedTask = (await SendAsync(server, HttpMethod.Post, "/api/export-tasks:submit", JsonSerializer.Serialize(Submit("users.list")), viewer)).Body;
        var denied = await FinishedAsync(server, viewer, deniedTask.GetProperty("id").GetString()!);
        Assert.Equal("Failed", denied.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(denied.GetProperty("errorMessage").GetString()));
        Assert.Equal("FILES_NOTFOUND_005", (await SendAsync(server, HttpMethod.Get, $"/api/export-tasks/{deniedTask.GetProperty("id").GetString()}", null, platform)).Body.GetProperty("code").GetString());
        var mine = (await SendAsync(server, HttpMethod.Post, "/api/export-tasks:list", """{"page":{"index":1,"size":20},"filters":[{"field":"status","operator":"in","value":"Success"}]}""", platform)).Body;
        Assert.Equal(2, mine.GetProperty("total").GetInt32());

        // 租户一侧的导出落在租户自己的文件里
        var tenantTask = (await SendAsync(server, HttpMethod.Post, "/api/export-tasks:submit", JsonSerializer.Serialize(Submit("users.list")), tenant)).Body;
        var tenantDone = await FinishedAsync(server, tenant, tenantTask.GetProperty("id").GetString()!);
        Assert.Equal("Success", tenantDone.GetProperty("status").GetString());
        var tenantFile = tenantDone.GetProperty("fileId").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Get, $"/api/files/{tenantFile}", null, tenant)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(server, HttpMethod.Get, $"/api/files/{tenantFile}", null, platform)).Status);

        // 删除任务连同结果文件
        var csvFile = csvDone.GetProperty("fileId").GetString()!;
        Assert.Empty((await SendAsync(server, HttpMethod.Delete, $"/api/export-tasks?ids={csvDone.GetProperty("id").GetString()}", null, platform)).Body.GetProperty("failed").EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(server, HttpMethod.Get, $"/api/files/{csvFile}", null, platform)).Status);

        // 导入历史：只看自己的，最近的在前，条数受限
        foreach (var (file, ok) in new[] { ("a.csv", 3), ("b.csv", 5) })
        {
            Assert.Equal(HttpStatusCode.Created, (await SendAsync(server, HttpMethod.Post, "/api/import-histories", JsonSerializer.Serialize(new { pageCode = "organization.positions", fileName = file, totalCount = ok + 1, successCount = ok, failCount = 1, errorSummary = "[{\"row\":2}]" }), platform)).Status);
        }

        await SendAsync(server, HttpMethod.Post, "/api/import-histories", JsonSerializer.Serialize(new { pageCode = "organization.positions", fileName = "other.csv", totalCount = 1, successCount = 1, failCount = 0 }), viewer);
        var recent = (await SendAsync(server, HttpMethod.Get, "/api/import-histories:recent?pageCode=organization.positions&count=1", null, platform)).Body.GetProperty("items");
        Assert.Equal("b.csv", Assert.Single(recent.EnumerateArray()).GetProperty("fileName").GetString());
        Assert.Equal(2, (await SendAsync(server, HttpMethod.Get, "/api/import-histories:recent?pageCode=organization.positions", null, platform)).Body.GetProperty("items").GetArrayLength());
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Auditing.Database.Trail;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证文件的上传、下载、隔离、去重与存储配置。
/// </summary>
/// <remarks>
/// 上传（multipart 普通端点）、签名下载地址与按身份读取、访问级别、两侧隔离、相同内容共用对象（删到最后一条才删对象）、秒传、归档、副本校验与主副本，
/// 普通端点的访问日志与操作审计；存储配置的校验、缺省配置的保护、还有副本的配置不能删、租户自己的缺省配置优先于平台的。
/// </remarks>
public sealed class FilesTests : IDisposable
{
    private readonly SqliteDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-files-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private Task<(BasicAppTestServer Server, string Platform, string Tenant)> StartAsync()
        => SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Configuration.AddInMemoryCollection(
        [
            new("BasicApp:Files:LocalRootPath", _root),
            new("BasicApp:Files:MaxUploadBytes", "1024"),
        ]));

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string? token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, body);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> UploadAsync(BasicAppTestServer server, string token, string name, byte[] content, string? accessLevel = null)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", name);
        if (accessLevel is not null)
        {
            form.Add(new StringContent(accessLevel), "accessLevel");
        }

        form.Add(new StringContent("a, b,a"), "tags");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/files/upload") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var (response, body) = await server.SendAsync(request);
        return (response.StatusCode, body);
    }

    private static async Task<(HttpStatusCode Status, string Body, HttpResponseMessage Response)> GetRawAsync(BasicAppTestServer server, string path, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await server.Client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(), response);
    }

    private static async Task<string> PresignAsync(BasicAppTestServer server, string token, string fileId)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Get, $"/api/files/{fileId}:presign", null, token);
        Assert.Equal(HttpStatusCode.OK, status);
        return body.GetProperty("url").GetString()!;
    }

    private static async Task<List<JsonElement>> StoragesAsync(BasicAppTestServer server, string token, string fileId)
        => [.. (await SendAsync(server, HttpMethod.Get, $"/api/files/{fileId}:storages", null, token)).Body.GetProperty("items").EnumerateArray()];

    private static async Task<List<JsonElement>> ListAsync(BasicAppTestServer server, string resource, string token, string body = """{"page":{"index":1,"size":200}}""")
    {
        var (status, json) = await SendAsync(server, HttpMethod.Post, $"/api/{resource}:list", body, token);
        Assert.Equal(HttpStatusCode.OK, status);
        return [.. json.GetProperty("items").EnumerateArray()];
    }

    /// <summary>
    /// 本地存储里对象的内容文件（目录存储把内容放在 <c>data</c>、元数据放在 <c>meta</c>）。
    /// </summary>
    private string BlobPathOf(string key) => Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Single(f => f.Contains(Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar, StringComparison.Ordinal) && Path.GetFileName(f).Contains(Path.GetFileNameWithoutExtension(key), StringComparison.Ordinal));

    private static string Sha256(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    [Fact]
    public async Task Files_UploadLinksAccessSharingAndCopies()
    {
        var (server, platform, tenant) = await StartAsync();
        await using var running = server;
        var hello = Encoding.UTF8.GetBytes("hello");

        // 上传：平台缺省的本地配置；大类、内容类型、SHA-256、标签规范化
        var (uploaded, first) = await UploadAsync(server, platform, "hello.txt", hello);
        Assert.Equal(HttpStatusCode.OK, uploaded);
        Assert.Equal(("hello.txt", "Document", 5, Sha256(hello), "a,b", "Authorized"), (first.GetProperty("originalName").GetString(), first.GetProperty("kind").GetString(), first.GetProperty("size").GetInt32(), first.GetProperty("hash").GetString(), first.GetProperty("tags").GetString(), first.GetProperty("accessLevel").GetString()));
        var firstId = first.GetProperty("id").GetString()!;
        var key = Assert.Single(await StoragesAsync(server, platform, firstId)).GetProperty("key").GetString()!;
        Assert.StartsWith("platform/", key, StringComparison.Ordinal);
        Assert.Equal("FILES_VALIDATION_005", (await UploadAsync(server, platform, "big.bin", new byte[2048])).Body.GetProperty("code").GetString());
        Assert.Equal("FILES_VALIDATION_006", (await UploadAsync(server, platform, "empty.txt", [])).Body.GetProperty("code").GetString());

        // 两侧隔离：租户看不到平台的文件，按 ID 也找不到
        Assert.Empty(await ListAsync(server, "files", tenant));
        Assert.Equal("FILES_NOTFOUND_001", (await SendAsync(server, HttpMethod.Get, $"/api/files/{firstId}", null, tenant)).Body.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await GetRawAsync(server, $"/api/files/{firstId}/content", tenant)).Status);

        // 签名地址不再校验身份；download=true 以附件下载；按身份读要登录
        var link = await PresignAsync(server, platform, firstId);
        var (signed, signedBody, _) = await GetRawAsync(server, link);
        Assert.Equal((HttpStatusCode.OK, "hello"), (signed, signedBody));
        var (_, _, attachment) = await GetRawAsync(server, link + "?download=true");
        Assert.Equal("hello.txt", attachment.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal(HttpStatusCode.NotFound, (await GetRawAsync(server, "/api/files/content/not-a-token")).Status);
        Assert.Equal((HttpStatusCode.OK, "hello"), ((await GetRawAsync(server, $"/api/files/{firstId}/content", platform)).Status, (await GetRawAsync(server, $"/api/files/{firstId}/content", platform)).Body));
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetRawAsync(server, $"/api/files/{firstId}/content")).Status);
        Assert.Equal(4, (await SendAsync(server, HttpMethod.Get, $"/api/files/{firstId}", null, platform)).Body.GetProperty("downloadCount").GetInt32());

        // 访问级别：没有 files.files.read 的平台用户换不到「需要授权」的地址，改成「已登录」即可
        await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = "viewer", displayName = "viewer", password = "Pass@123456" }), platform);
        var viewer = (await AuthFlowTests.LoginAsync(server, "viewer", "Pass@123456")).GetProperty("accessToken").GetString()!;
        var (denied, deniedBody) = await SendAsync(server, HttpMethod.Get, $"/api/files/{firstId}:presign", null, viewer);
        Assert.Equal((HttpStatusCode.Forbidden, "FILES_DENIED_001"), (denied, deniedBody.GetProperty("code").GetString()));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/files/{firstId}", """{"accessLevel":"Authenticated","remark":"open"}""", platform)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Get, $"/api/files/{firstId}:presign", null, viewer)).Status);
        Assert.Equal("KERNEL_VALIDATION_003", (await SendAsync(server, HttpMethod.Get, $"/api/files/{firstId}:presign?expiresInSeconds=999999999", null, platform)).Body.GetProperty("code").GetString());

        // 相同内容共用对象：再上传与秒传各得一条新记录、不另写对象；删一条对象还在，删到最后一条才删对象
        var second = (await UploadAsync(server, platform, "again.txt", hello)).Body.GetProperty("id").GetString()!;
        Assert.NotEqual(firstId, second);
        Assert.Equal(key, Assert.Single(await StoragesAsync(server, platform, second)).GetProperty("key").GetString());
        var (fast, fastBody) = await SendAsync(server, HttpMethod.Post, "/api/files:fast-upload", JsonSerializer.Serialize(new { hash = Sha256(hello), size = 5, originalName = "fast.txt" }), platform);
        Assert.Equal(HttpStatusCode.OK, fast);
        var fastId = fastBody.GetProperty("file").GetProperty("id").GetString()!;
        Assert.Equal("fast-upload", fastBody.GetProperty("file").GetProperty("source").GetString());
        Assert.Equal(JsonValueKind.Null, (await SendAsync(server, HttpMethod.Post, "/api/files:fast-upload", JsonSerializer.Serialize(new { hash = Sha256([1, 2, 3]), size = 3, originalName = "miss.bin" }), platform)).Body.GetProperty("file").ValueKind);
        Assert.Equal("FILES_VALIDATION_007", (await SendAsync(server, HttpMethod.Post, "/api/files:fast-upload", JsonSerializer.Serialize(new { hash = new string('z', 64), size = 3, originalName = "bad.bin" }), platform)).Body.GetProperty("code").GetString());
        var blob = BlobPathOf(key);
        Assert.Empty((await SendAsync(server, HttpMethod.Delete, $"/api/files?ids={second}&ids={fastId}", null, platform)).Body.GetProperty("failed").EnumerateArray());
        Assert.True(File.Exists(blob));
        Assert.Equal((HttpStatusCode.OK, "hello"), ((await GetRawAsync(server, link)).Status, (await GetRawAsync(server, link)).Body));

        // 租户上传落在平台的缺省配置上，对象键在租户自己的前缀下
        var tenantFile = (await UploadAsync(server, tenant, "mine.txt", hello)).Body.GetProperty("id").GetString()!;
        var tenantCopy = Assert.Single(await StoragesAsync(server, tenant, tenantFile));
        Assert.Equal(("local-default", true), (tenantCopy.GetProperty("storageConfigCode").GetString(), tenantCopy.GetProperty("key").GetString()!.StartsWith("contoso/", StringComparison.Ordinal)));
        Assert.Equal((HttpStatusCode.OK, "hello"), ((await GetRawAsync(server, await PresignAsync(server, tenant, tenantFile))).Status, (await GetRawAsync(server, await PresignAsync(server, tenant, tenantFile))).Body));

        // 归档后读不到，恢复即可
        Assert.Empty((await SendAsync(server, HttpMethod.Post, "/api/files:set-status", JsonSerializer.Serialize(new { ids = new[] { firstId }, status = "Archived" }), platform)).Body.GetProperty("failed").EnumerateArray());
        var (archived, archivedBody, _) = await GetRawAsync(server, link);
        Assert.Equal(HttpStatusCode.Conflict, archived);
        Assert.Contains("FILES_CONFLICT_005", archivedBody, StringComparison.Ordinal);
        Assert.Equal("FILES_VALIDATION_009", (await SendAsync(server, HttpMethod.Post, "/api/files:set-status", JsonSerializer.Serialize(new { ids = new[] { firstId }, status = "Corrupted" }), platform)).Body.GetProperty("code").GetString());
        await SendAsync(server, HttpMethod.Post, "/api/files:set-status", JsonSerializer.Serialize(new { ids = new[] { firstId }, status = "Normal" }), platform);

        // 副本：校验通过记时间；内容被改按 SHA-256 判不符，主副本不通过时文件损坏；主副本不能停用
        var copyId = Assert.Single(await StoragesAsync(server, platform, firstId)).GetProperty("id").GetString()!;
        Assert.Equal("Normal", (await SendAsync(server, HttpMethod.Post, $"/api/files/{firstId}:verify-storage", JsonSerializer.Serialize(new { storageId = copyId }), platform)).Body.GetProperty("status").GetString());
        Assert.Equal("FILES_CONFLICT_004", (await SendAsync(server, HttpMethod.Post, $"/api/files/{firstId}:set-storage-status", JsonSerializer.Serialize(new { storageId = copyId, status = "Unavailable" }), platform)).Body.GetProperty("code").GetString());
        await File.WriteAllTextAsync(blob, "HELLO");
        Assert.Equal("Corrupted", (await SendAsync(server, HttpMethod.Post, $"/api/files/{firstId}:verify-storage", JsonSerializer.Serialize(new { storageId = copyId }), platform)).Body.GetProperty("status").GetString());
        var failedCopy = Assert.Single(await StoragesAsync(server, platform, firstId));
        Assert.Equal(("VerificationFailed", "hash-mismatch"), (failedCopy.GetProperty("status").GetString(), failedCopy.GetProperty("remark").GetString()));
        Assert.Equal("FILES_CONFLICT_004", (await SendAsync(server, HttpMethod.Post, $"/api/files/{firstId}:set-primary-storage", JsonSerializer.Serialize(new { storageId = copyId }), platform)).Body.GetProperty("code").GetString());
        Assert.Equal("FILES_CONFLICT_005", (await SendAsync(server, HttpMethod.Post, "/api/files:set-status", JsonSerializer.Serialize(new { ids = new[] { firstId }, status = "Archived" }), platform)).Body.GetProperty("failed")[0].GetProperty("code").GetString());
        File.Delete(blob);
        await SendAsync(server, HttpMethod.Post, $"/api/files/{firstId}:verify-storage", JsonSerializer.Serialize(new { storageId = copyId }), platform);
        Assert.Equal("missing", Assert.Single(await StoragesAsync(server, platform, firstId)).GetProperty("remark").GetString());

        // 删除最后一条引用：对象已不在也照删记录
        Assert.Empty((await SendAsync(server, HttpMethod.Delete, $"/api/files?ids={firstId}", null, platform)).Body.GetProperty("failed").EnumerateArray());
        Assert.Empty(await ListAsync(server, "files", platform));

        // 普通端点的访问日志与操作审计
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var access = await ListAsync(server, "access-logs", platform);
        Assert.Contains(access, a => a.GetProperty("operationId").GetString() == "files.upload" && a.GetProperty("statusCode").GetInt32() == 200 && a.GetProperty("path").GetString() == "/api/files/upload");
        Assert.Contains(access, a => a.GetProperty("operationId").GetString() == "files.upload" && a.GetProperty("errorCode").GetString() == "FILES_VALIDATION_005");
        Assert.Contains(access, a => a.GetProperty("operationId").GetString() == "files.signed-content" && a.GetProperty("statusCode").GetInt32() == 409);
        Assert.Contains(access, a => a.GetProperty("operationId").GetString() == "files.content" && a.GetProperty("result").GetString() == "Success");
        Assert.Contains(await ListAsync(server, "operation-logs", platform), o => o.GetProperty("operationId").GetString() == "files.upload" && o.GetProperty("succeeded").GetBoolean());
        Assert.Contains(await ListAsync(server, "access-logs", tenant), a => a.GetProperty("operationId").GetString() == "files.upload");
    }

    [Fact]
    public async Task StorageConfigs_ValidateAndGuardDefaults()
    {
        var (server, platform, tenant) = await StartAsync();
        await using var running = server;
        async Task<(HttpStatusCode, JsonElement)> CreateAsync(string token, object config) => await SendAsync(server, HttpMethod.Post, "/api/storage-configs", JsonSerializer.Serialize(config), token);

        var seeded = Assert.Single(await ListAsync(server, "storage-configs", platform));
        Assert.Equal(("local-default", "Local", true, "files"), (seeded.GetProperty("code").GetString(), seeded.GetProperty("type").GetString(), seeded.GetProperty("isDefault").GetBoolean(), seeded.GetProperty("bucket").GetString()));
        var localDefault = seeded.GetProperty("id").GetString()!;

        // 写入校验
        Assert.Equal("FILES_VALIDATION_001", (await CreateAsync(platform, new { code = "bad code", name = "x", type = "Local", bucket = "files" })).Item2.GetProperty("code").GetString());
        Assert.Equal("FILES_VALIDATION_002", (await CreateAsync(platform, new { code = "bad-bucket", name = "x", type = "Local", bucket = "Bad_Bucket" })).Item2.GetProperty("code").GetString());
        Assert.Equal("FILES_VALIDATION_003", (await CreateAsync(platform, new { code = "s3", name = "x", type = "S3", bucket = "files" })).Item2.GetProperty("code").GetString());
        Assert.Equal("FILES_VALIDATION_004", (await CreateAsync(platform, new { code = "s3", name = "x", type = "MinIO", bucket = "files", endpoint = "ftp://minio", accessKeyId = "k", secretAccessKey = "s" })).Item2.GetProperty("code").GetString());

        // S3 兼容：密钥加密存储、不回显，修改时不填即保留
        var (created, s3) = await CreateAsync(platform, new { code = "minio", name = "MinIO", type = "MinIO", bucket = "files", endpoint = "http://localhost:9000", accessKeyId = "key", secretAccessKey = "secret" });
        Assert.Equal(HttpStatusCode.Created, created);
        Assert.True(s3.GetProperty("hasSecretAccessKey").GetBoolean());
        Assert.False(s3.TryGetProperty("secretAccessKey", out _));
        var minio = s3.GetProperty("id").GetString()!;
        Assert.Equal("FILES_CONFLICT_001", (await CreateAsync(platform, new { code = "minio", name = "again", type = "Local", bucket = "files" })).Item2.GetProperty("code").GetString());
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/storage-configs/{minio}", """{"name":"MinIO 2","type":"MinIO","bucket":"files","endpoint":"http://localhost:9000","accessKeyId":"key"}""", platform);
        Assert.Equal((HttpStatusCode.OK, true, "MinIO 2"), (updated, updatedBody.GetProperty("hasSecretAccessKey").GetBoolean(), updatedBody.GetProperty("name").GetString()));

        // 缺省配置：停用的不能设为缺省，缺省的不能停用或删除
        await SendAsync(server, HttpMethod.Post, $"/api/storage-configs/{minio}:set-status", """{"status":"Disabled"}""", platform);
        Assert.Equal("FILES_CONFLICT_006", (await SendAsync(server, HttpMethod.Post, $"/api/storage-configs/{minio}:set-default", "{}", platform)).Body.GetProperty("code").GetString());
        Assert.Equal("FILES_CONFLICT_002", (await SendAsync(server, HttpMethod.Post, $"/api/storage-configs/{localDefault}:set-status", """{"status":"Disabled"}""", platform)).Body.GetProperty("code").GetString());
        Assert.Equal("FILES_CONFLICT_002", (await SendAsync(server, HttpMethod.Delete, $"/api/storage-configs?ids={localDefault}", null, platform)).Body.GetProperty("failed")[0].GetProperty("code").GetString());

        // 换缺省：新上传落到新的缺省配置上；还有副本的配置不能删
        var (_, archiveBody) = await CreateAsync(platform, new { code = "archive", name = "Archive", type = "Local", bucket = "archive", isDefault = true });
        var archive = archiveBody.GetProperty("id").GetString()!;
        Assert.False(Assert.Single(await ListAsync(server, "storage-configs", platform, """{"page":{"index":1,"size":20},"filters":[{"field":"isDefault","operator":"eq","value":"false"},{"field":"type","operator":"in","value":"Local"}]}""")).GetProperty("isDefault").GetBoolean());
        var file = (await UploadAsync(server, platform, "a.txt", Encoding.UTF8.GetBytes("archived"))).Body.GetProperty("id").GetString()!;
        Assert.Equal("archive", Assert.Single(await StoragesAsync(server, platform, file)).GetProperty("storageConfigCode").GetString());
        await SendAsync(server, HttpMethod.Post, $"/api/storage-configs/{localDefault}:set-default", "{}", platform);
        Assert.Equal("FILES_CONFLICT_003", (await SendAsync(server, HttpMethod.Delete, $"/api/storage-configs?ids={archive}", null, platform)).Body.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Empty((await SendAsync(server, HttpMethod.Delete, $"/api/storage-configs?ids={minio}", null, platform)).Body.GetProperty("failed").EnumerateArray());
        Assert.Equal("FILES_VALIDATION_011", (await SendAsync(server, HttpMethod.Post, "/api/storage-configs:list", """{"filters":[{"field":"bucket","operator":"eq","value":"files"}]}""", platform)).Body.GetProperty("code").GetString());

        // 租户只看自己的配置；有了自己的缺省配置就不再用平台的
        Assert.Empty(await ListAsync(server, "storage-configs", tenant));
        Assert.Equal(HttpStatusCode.Created, (await CreateAsync(tenant, new { code = "own", name = "Own", type = "Local", bucket = "contoso-files", isDefault = true })).Item1);
        var tenantFile = (await UploadAsync(server, tenant, "t.txt", Encoding.UTF8.GetBytes("tenant"))).Body.GetProperty("id").GetString()!;
        Assert.Equal("own", Assert.Single(await StoragesAsync(server, tenant, tenantFile)).GetProperty("storageConfigCode").GetString());
        Assert.Equal("FILES_NOTFOUND_002", (await SendAsync(server, HttpMethod.Get, $"/api/storage-configs/{localDefault}", null, tenant)).Body.GetProperty("code").GetString());
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Buffers;
using System.Net;
using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Protocols.Grpc.Methods;
using XiHan.Framework.Protocols.Grpc.Proto;

namespace XiHan.BasicApp.Integration.Tests.Smoke;

/// <summary>
/// 验证 gRPC 与 HTTP 共用一份操作 Schema。
/// </summary>
/// <remarks>
/// 方案 9.1 第四项。
/// gRPC 面上的操作（操作表里显式带 <c>Expose.Grpc</c> 的 <c>users.get</c> / <c>users.list</c>）
/// 经 gRPC 调用得到与 HTTP 相同的结果与错误码；方法路径带包名 <c>xihan.basicapp</c>。
/// </remarks>
public sealed class GrpcParitySmokeTests : IDisposable
{
    private static readonly Marshaller<byte[]> _bytes = Marshallers.Create<byte[]>(static b => b, static b => b);

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Grpc_ReturnsTheSameResultsAndErrorCodesAsHttp()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        using var channel = server.CreateGrpcChannel();
        var table = server.Services.GetRequiredService<GrpcOperationTable>();

        // 只发布显式带 Expose.Grpc 的操作（操作表）：内部服务查用户目录的 get / list，写操作不在 gRPC 面上
        Assert.Equal(["users.get", "users.list"], table.Operations.Select(static o => o.Descriptor.Id).Order(StringComparer.Ordinal));
        var get = table.Operations.Single(static o => o.Descriptor.Id == "users.get");
        var list = table.Operations.Single(static o => o.Descriptor.Id == "users.list");
        Assert.Equal("xihan.basicapp", table.Package);
        Assert.Equal("Users/Get", get.Method.ToString());
        var proto = await server.Client.GetStringAsync("/operations.proto");
        Assert.Contains("package xihan.basicapp;", proto, StringComparison.Ordinal);
        Assert.Contains("service Users {", proto, StringComparison.Ordinal);
        Assert.DoesNotContain("rpc Create", proto, StringComparison.Ordinal);

        var admin = TestTokens.Issue("alice", "acme", "admin");
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"http.user","displayName":"Via HTTP"}""", admin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = createdBody.GetProperty("id").GetString()!;

        // 同一主体取同一用户：两边的响应形状一致
        var (httpGot, httpBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/users/{id}", token: admin));
        Assert.Equal(HttpStatusCode.OK, httpGot.StatusCode);
        using var grpcReply = await CallAsync(channel, get, $$"""{"id":"{{id}}"}""", ("authorization", "Bearer " + admin));
        var grpcBody = grpcReply.RootElement;
        Assert.Equal("http.user", grpcBody.GetProperty("userName").GetString());
        Assert.Equal("acme", grpcBody.GetProperty("homeTenantId").GetString());
        Assert.Equal("Active", grpcBody.GetProperty("status").GetString());
        // 线上没有 null：HTTP 响应里为 null 的属性（email、updatedBy 之类）在 gRPC 侧就是缺席，其余属性一一对应
        Assert.Equal(
            httpBody.EnumerateObject().Where(static p => p.Value.ValueKind != JsonValueKind.Null).Select(static p => p.Name).Order(StringComparer.Ordinal),
            grpcBody.EnumerateObject().Select(static p => p.Name).Order(StringComparer.Ordinal));

        // 列表：两边看到同样的总数
        var (httpList, httpListBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"page":{"index":1,"size":10}}""", admin));
        Assert.Equal(HttpStatusCode.OK, httpList.StatusCode);
        using var grpcList = await CallAsync(channel, list, """{"page":{"index":1,"size":10}}""", ("authorization", "Bearer " + admin));
        Assert.Equal(httpListBody.GetProperty("total").GetInt64(), grpcList.RootElement.GetProperty("total").GetInt64());
        Assert.Equal(1, grpcList.RootElement.GetProperty("items").GetArrayLength());

        // 错误码：未认证 → Unauthenticated / SECURITY_DENIED_001；无权限 → PermissionDenied / SECURITY_DENIED_004；校验 → InvalidArgument / KERNEL_VALIDATION_003；找不到 → NotFound / IDENTITY_NOTFOUND_001
        var anonymous = await Assert.ThrowsAsync<RpcException>(async () => await CallAsync(channel, list, """{"page":{"index":1,"size":10}}"""));
        Assert.Equal(StatusCode.Unauthenticated, anonymous.StatusCode);
        Assert.Equal("SECURITY_DENIED_001", anonymous.Trailers.GetValue("xihan-error-code"));

        var guest = await Assert.ThrowsAsync<RpcException>(async () => await CallAsync(channel, list, """{"page":{"index":1,"size":10}}""", ("authorization", "Bearer " + TestTokens.Issue("carol", "acme", "guest"))));
        Assert.Equal(StatusCode.PermissionDenied, guest.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", guest.Trailers.GetValue("xihan-error-code"));

        var invalid = await Assert.ThrowsAsync<RpcException>(async () => await CallAsync(channel, list, """{"page":{"index":1,"size":501}}""", ("authorization", "Bearer " + admin), ("accept-language", "zh-CN")));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Equal("KERNEL_VALIDATION_003", invalid.Trailers.GetValue("xihan-error-code"));
        var (httpInvalid, httpInvalidBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"page":{"index":1,"size":501}}""", admin, "zh-CN"));
        Assert.Equal(HttpStatusCode.BadRequest, httpInvalid.StatusCode);
        Assert.Equal(httpInvalidBody.GetProperty("detail").GetString(), invalid.Status.Detail);

        var missing = await Assert.ThrowsAsync<RpcException>(async () => await CallAsync(channel, get, $$"""{"id":"{{Guid.CreateVersion7()}}"}""", ("authorization", "Bearer " + admin)));
        Assert.Equal(StatusCode.NotFound, missing.StatusCode);
        Assert.Equal("IDENTITY_NOTFOUND_001", missing.Trailers.GetValue("xihan-error-code"));
        Assert.Equal(32, missing.Trailers.GetValue("xihan-trace-id")!.Length);
    }

    private static async Task<JsonDocument> CallAsync(GrpcChannel channel, GrpcOperation operation, string json, params (string Name, string Value)[] headers)
    {
        using var document = JsonDocument.Parse(json);
        var request = new ArrayBufferWriter<byte>();
        ProtoEncoder.Encode(operation.Request, document.RootElement, request);
        var metadata = new Metadata();
        foreach (var (name, value) in headers)
        {
            metadata.Add(name, value);
        }

        var method = new Method<byte[], byte[]>(MethodType.Unary, operation.Method.FullServiceName("xihan.basicapp"), operation.Method.Method, _bytes, _bytes);
        using var call = channel.CreateCallInvoker().AsyncUnaryCall(method, null, new CallOptions(metadata), request.WrittenSpan.ToArray());
        var response = await call.ResponseAsync;
        var output = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(output))
        {
            ProtoDecoder.Decode(operation.Response, response, writer);
        }

        return JsonDocument.Parse(output.WrittenMemory);
    }
}

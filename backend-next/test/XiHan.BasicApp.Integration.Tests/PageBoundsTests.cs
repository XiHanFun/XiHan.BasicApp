// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证页码的上限。
/// </summary>
/// <remarks>
/// 页码 1 到 <see cref="PageInfo.MaxIndex"/>：上限本身照常返回（空页），越界以 400 <c>KERNEL_VALIDATION_003</c> 拒绝，字段错误按请求的区域给出文案。
/// 原先只限到 <see cref="int.MaxValue"/>，换算跳过的行数时溢出成负数（SQLite 返回第一页，PostgreSQL 报错成 500）。
/// </remarks>
public sealed class PageBoundsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task PageIndex_BeyondTheLimit_IsRejectedWith400_InTheCallersLocale()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var token = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        async Task<(HttpStatusCode Status, JsonElement Body)> ListAsync(long index, string locale)
        {
            var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", JsonSerializer.Serialize(new { page = new { index, size = 500 } }), token, locale));
            return (response.StatusCode, body);
        }

        // 上限本身：超出数据的页照常是空页
        var (atLimit, atLimitBody) = await ListAsync(PageInfo.MaxIndex, "zh-CN");
        Assert.Equal(HttpStatusCode.OK, atLimit);
        Assert.Equal(0, atLimitBody.GetProperty("items").GetArrayLength());
        Assert.Equal(PageInfo.MaxIndex, atLimitBody.GetProperty("page").GetProperty("index").GetInt32());

        // 越界：400，字段错误按区域给文案（框架的消息目录，十种语言）
        foreach (var (index, locale, message) in new[] { (PageInfo.MaxIndex + 1L, "zh-CN", "值必须 <= 100000"), (int.MaxValue, "en-US", "value must be <= 100000"), (int.MaxValue, "ja-JP", "値は 100000 以下である必要があります") })
        {
            var (status, body) = await ListAsync(index, locale);
            Assert.Equal(HttpStatusCode.BadRequest, status);
            Assert.Equal("KERNEL_VALIDATION_003", body.GetProperty("code").GetString());
            Assert.Equal(message, Assert.Single(body.GetProperty("errors").GetProperty("page.index").EnumerateArray()).GetString());
        }

        // 平铺页码的查询（查询串）同一个上限
        var (candidates, candidatesBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/impersonation:candidates?pageIndex={PageInfo.MaxIndex + 1}", token: token, acceptLanguage: "zh-CN"));
        Assert.Equal(HttpStatusCode.BadRequest, candidates.StatusCode);
        Assert.Equal("KERNEL_VALIDATION_003", candidatesBody.GetProperty("code").GetString());
        Assert.Equal("值必须 <= 100000", Assert.Single(candidatesBody.GetProperty("errors").GetProperty("pageIndex").EnumerateArray()).GetString());
    }
}

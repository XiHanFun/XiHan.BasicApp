// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests.Smoke;

/// <summary>
/// 方案 9.1 第一项：方式一回调里 <c>ConnectionConfig.AopEvents.DataExecuting</c> 能否读到当前主体并填 <c>CreatedBy</c> / <c>ModifiedBy</c> / <c>DeletedBy</c>。
/// 结论：回调没有作用域，主体经 Transaction 阶段中间件放进异步本地值后回调能读到；插入、更新、软删除三条路径都填上了。
/// </summary>
public sealed class AuditorStampSmokeTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task AopEvents_FillCreatedBy_ModifiedBy_DeletedBy_FromTheCurrentPrincipal()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        var alice = TestTokens.Issue("alice", "acme", "admin");
        var dave = TestTokens.Issue("dave", "acme", "admin");

        var (created, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"stamped","displayName":"Stamped"}""", alice));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("alice", body.GetProperty("createdBy").GetString());
        var id = Guid.Parse(body.GetProperty("id").GetString()!);

        // 更新由另一个主体执行：ModifiedBy 是 dave，CreatedBy 保持 alice
        (await server.InvokeAsync(async ctx =>
        {
            var users = ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>();
            var user = (await users.FindAsync(id, ctx.CancellationToken)).ValueOrThrow();
            user.DisplayName = "Renamed";
            return await users.UpdateAsync(user, ctx.CancellationToken);
        }, dave)).ThrowIfFailure();

        var afterUpdate = await ReadAsync(server, id);
        Assert.Equal("alice", afterUpdate.CreatedBy);
        Assert.Equal("dave", afterUpdate.ModifiedBy);
        Assert.Null(afterUpdate.DeletedBy);
        Assert.False(afterUpdate.IsDeleted);

        // 软删除是整行写回：删除标记置位的那次写入填 DeletedBy
        (await server.InvokeAsync(async ctx =>
        {
            var users = ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>();
            var user = (await users.FindAsync(id, ctx.CancellationToken)).ValueOrThrow();
            return await users.RemoveAsync(user, ctx.CancellationToken);
        }, alice)).ThrowIfFailure();

        var afterDelete = await ReadAsync(server, id);
        Assert.True(afterDelete.IsDeleted);
        Assert.Equal("alice", afterDelete.DeletedBy);
        Assert.Equal("alice", afterDelete.ModifiedBy);

        // 没有主体（系统作业）的写入不动操作人字段
        var systemId = Guid.CreateVersion7();
        (await server.InvokeAsync(async ctx =>
        {
            var users = ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>();
            return await users.AddAsync(new SysUser { Id = systemId, UserName = "system.user", DisplayName = "System", HomeTenantId = "acme" }, ctx.CancellationToken);
        })).ThrowIfFailure();
        Assert.Null((await ReadAsync(server, systemId)).CreatedBy);
    }

    private static async Task<SysUser> ReadAsync(BasicAppTestServer server, Guid id)
    {
        SysUser? row = null;
        (await server.InvokeAsync(async ctx =>
        {
            // 绕过仓储的软删除过滤，直接读行
            row = await ctx.Services.GetRequiredService<ISqlSugarClient>().Queryable<SysUser>().Where(u => u.Id == id).FirstAsync(ctx.CancellationToken);
            return Result.Success();
        })).ThrowIfFailure();
        return Assert.IsType<SysUser>(row);
    }
}

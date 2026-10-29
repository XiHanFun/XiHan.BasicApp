// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证工作单元：一次调用里的多次写入一起提交或一起回滚。
/// </summary>
/// <remarks>
/// 单库版本；双库版本见 Smoke/MultiDatabaseSmokeTests。
/// </remarks>
public sealed class TransactionTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task FailedCall_RollsBackEveryWrite_OfTheInvocation()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        var acme = TestTokens.Issue("alice", "acme", "admin");
        var userId = Guid.CreateVersion7();

        var failed = await server.InvokeAsync(async ctx =>
        {
            (await ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>().AddAsync(new SysUser { Id = userId, UserName = "rollback", DisplayName = "Rollback", HomeTenantId = "acme" }, ctx.CancellationToken)).ThrowIfFailure();
            (await ctx.Services.GetRequiredService<IRepository<SysUserStatistics, Guid>>().AddAsync(new SysUserStatistics { Id = Guid.CreateVersion7(), UserId = userId }, ctx.CancellationToken)).ThrowIfFailure();
            return Error.Validation("IDENTITY_VALIDATION_001", "identity.user.tenant_required");
        }, acme);
        Assert.Equal("IDENTITY_VALIDATION_001", failed.Error?.Code);

        var counts = (-1L, -1L);
        (await server.InvokeAsync(async ctx =>
        {
            counts = (
                await ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>().CountAsync(EntityQuery.All<SysUser>(), ctx.CancellationToken),
                await ctx.Services.GetRequiredService<IRepository<SysUserStatistics, Guid>>().CountAsync(EntityQuery.All<SysUserStatistics>(), ctx.CancellationToken));
            return Result.Success();
        }, acme)).ThrowIfFailure();
        Assert.Equal((0L, 0L), counts);

        // 无租户的调用看不到任何租户库实体；平台实体照常可见
        var platformCounts = (-1L, -1L);
        (await server.InvokeAsync(async ctx =>
        {
            (await ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>().AddAsync(new SysUser { Id = Guid.CreateVersion7(), UserName = "kept", DisplayName = "Kept", HomeTenantId = "acme" }, ctx.CancellationToken)).ThrowIfFailure();
            return Result.Success();
        }, acme)).ThrowIfFailure();
        (await server.InvokeAsync(async ctx =>
        {
            platformCounts = (
                await ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>().CountAsync(EntityQuery.All<SysUser>(), ctx.CancellationToken),
                await ctx.Services.GetRequiredService<IRepository<SysUserStatistics, Guid>>().CountAsync(EntityQuery.All<SysUserStatistics>(), ctx.CancellationToken));
            return Result.Success();
        })).ThrowIfFailure();
        Assert.Equal((1L, 0L), platformCounts);
    }
}

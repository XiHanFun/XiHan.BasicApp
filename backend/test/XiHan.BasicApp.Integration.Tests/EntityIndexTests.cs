// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Host;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.Framework.Domain.Entities;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 守护实体的索引声明
/// </summary>
/// <remarks>
/// 软删除实体的唯一约束只约束未删除的行（<see cref="EntityIndexAttribute"/> 写 <c>IsUnique</c> 与 <c>Name</c>，框架建成部分唯一索引），不写唯一的 <c>[SugarIndex]</c>（同一个键第二次删除会撞约束）；
/// <c>[EntityIndex]</c> 只用于软删除实体的唯一约束，其余索引写 <c>[SugarIndex]</c>；索引列都是实体的属性；按实际表名算出的索引名不超过 PostgreSQL 的 63 字符且不重名（不区分大小写）
/// 语义上：未删的行插重复被数据库拒绝，已删的行同键可以有多条；并发写入越过操作里的查重、撞上索引时，第二个写入方拿到唯一键重复（409），不是 500
/// </remarks>
public sealed class EntityIndexTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    /// <summary>
    /// 宿主装配的全部能力包里的实体类型
    /// </summary>
    private static List<Type> EntityTypes()
    {
        var assemblies = typeof(BasicAppHost).Assembly.GetReferencedAssemblies()
            .Where(static name => name.Name!.StartsWith("XiHan.BasicApp.", StringComparison.Ordinal))
            .Select(Assembly.Load)
            .ToList();
        return [.. assemblies.SelectMany(static a => a.GetTypes())
            .Where(static t => t is { IsClass: true, IsAbstract: false } && typeof(IEntity<Guid>).IsAssignableFrom(t))];
    }

    private static string TableOf(Type type)
    {
        // 按月分表的表名带日期后缀（sys_access_log_20261101），索引名跟着变长
        var table = type.GetCustomAttribute<SugarTable>()!.TableName;
        return table.Replace("_{year}{month}{day}", "_20261101", StringComparison.Ordinal);
    }

    [Fact]
    public void Indexes_FollowTheSoftDeleteRule_NameLimit_AndPointAtRealProperties()
    {
        var types = EntityTypes();
        Assert.True(types.Count >= 60, $"Expected the BasicApp entities, found {types.Count}.");
        var problems = new List<string>();
        // PostgreSQL 把名字折成小写、Oracle 存成大写，只差大小写的两个名字在那里是同一个
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Name(Type type, string name)
        {
            if (name.Length > EntityIndexes.MaxNameLength)
            {
                problems.Add($"{type.Name}: index name '{name}' has {name.Length} characters.");
            }

            if (!names.TryAdd(name, type.Name))
            {
                problems.Add($"{type.Name}: index name '{name}' is also used by {names[name]}.");
            }
        }

        foreach (var type in types)
        {
            var softDeletable = typeof(ISoftDeletable).IsAssignableFrom(type);
            var table = TableOf(type);
            foreach (var index in type.GetCustomAttributes<SugarIndexAttribute>(inherit: false))
            {
                if (index.IsUnique && softDeletable)
                {
                    problems.Add($"{type.Name}: unique [SugarIndex] '{index.IndexName}' on a soft-deletable entity; use [EntityIndex(..., IsUnique = true, Name = ...)].");
                }

                problems.AddRange(index.IndexFields.Keys.Where(field => type.GetProperty(field) is null).Select(field => $"{type.Name}: [SugarIndex] '{index.IndexName}' names a missing property '{field}'."));
                Name(type, index.IndexName.Replace("{table}", table, StringComparison.Ordinal).Replace("{split_table}", table, StringComparison.Ordinal));
            }

            foreach (var index in type.GetCustomAttributes<EntityIndexAttribute>(inherit: false))
            {
                var columns = string.Join(", ", index.Properties);
                if (!softDeletable || !index.IsUnique || index.Properties.Contains(nameof(ISoftDeletable.IsDeleted)))
                {
                    problems.Add($"{type.Name}: [EntityIndex] on ({columns}) is only for the unique keys of soft-deletable entities (without 'IsDeleted'); use [SugarIndex].");
                }

                problems.AddRange(index.Properties.Where(column => type.GetProperty(column) is null).Select(column => $"{type.Name}: [EntityIndex] names a missing property '{column}'."));
                Name(type, index.Name ?? $"(unnamed on {columns})");
            }

            // 框架的声明规则（显式名字的写法、多租户实体的唯一索引含 TenantId 等）在建表时才报出，这里先过一遍
            try
            {
                _ = EntityIndexes.Resolve(type, table);
            }
            catch (InvalidOperationException exception)
            {
                problems.Add($"{type.Name}: {exception.Message}");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public async Task PartialUniqueIndex_RejectsDuplicateLiveRows_ButKeepsAnyNumberOfDeletedOnes()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await using var scope = server.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        SysRole Role(bool deleted) => new()
        {
            Id = Guid.NewGuid(),
            OwnerTenantId = "platform",
            Code = "index-probe",
            Name = "Index probe",
            IsDeleted = deleted,
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // 已删除的同键行不受约束：删了再建、再删都不冲突
        await client.Insertable(Role(deleted: true)).ExecuteCommandAsync(TestContext.Current.CancellationToken);
        await client.Insertable(Role(deleted: true)).ExecuteCommandAsync(TestContext.Current.CancellationToken);
        await client.Insertable(Role(deleted: false)).ExecuteCommandAsync(TestContext.Current.CancellationToken);

        // 未删的同键行只能有一条：操作里的查重之外，并发写入由数据库兜住
        await Assert.ThrowsAnyAsync<Exception>(() => client.Insertable(Role(deleted: false)).ExecuteCommandAsync(TestContext.Current.CancellationToken));
        Assert.Equal(3, await client.Ado.GetIntAsync("SELECT COUNT(*) FROM sys_role WHERE code = 'index-probe'"));
    }

    [Fact]
    public async Task WriteThatSlipsPastTheCodeCheck_IsAUniqueKeyConflict_NotAServerError()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        var scopes = server.Services.GetRequiredService<IServiceScopeFactory>();
        var cancellationToken = TestContext.Current.CancellationToken;
        SysDict Dict() => new() { Id = Guid.NewGuid(), OwnerTenantId = TenantIds.Platform, Code = "race-probe", Name = "Race probe" };
        Task<Result> AddAsync(SysDict dict) => StoreScope.WriteAsync(scopes, async (services, ct) => await services.GetRequiredService<IRepository<SysDict, Guid>>().AddAsync(dict, ct), cancellationToken);

        // 两个请求同时新建同一编码：先提交的那个照常写入
        (await AddAsync(Dict())).ThrowIfFailure();

        // 后一个的查重在前一个提交之前做完、没有查到，写入撞上部分唯一索引：仓储核对出唯一键重复（HTTP 409），工作单元回滚
        var outcome = await AddAsync(Dict());

        Assert.Equal(PersistenceErrors.UniqueKeyConflict, outcome.Error?.Code);
        Assert.Equal("ux_sys_dict_owner_tenant_id_code", outcome.Error!.Details!["index"]);
        await using var scope = server.Services.CreateAsyncScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ISqlSugarClient>().Ado.GetIntAsync("SELECT COUNT(*) FROM sys_dict WHERE code = 'race-probe'"));
    }
}

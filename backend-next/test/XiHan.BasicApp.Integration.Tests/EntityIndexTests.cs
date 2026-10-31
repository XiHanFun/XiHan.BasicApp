// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.BasicApp.Host;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 守护实体的索引声明。
/// </summary>
/// <remarks>
/// 软删除实体的唯一约束只约束未删除的行（<see cref="ActiveUniqueIndexAttribute"/>，部分唯一索引），不写唯一的 <c>[SugarIndex]</c>（同一个键第二次删除会撞约束）；
/// 部分唯一索引只用于软删除实体；索引列都是实体的属性；按实际表名算出的索引名不超过 PostgreSQL 的 63 字符且不重名。语义上：未删的行插重复被数据库拒绝，已删的行同键可以有多条。
/// </remarks>
public sealed class EntityIndexTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    /// <summary>
    /// 宿主装配的全部能力包里的实体类型。
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
        var table = type.GetCustomAttribute<SugarTable>()?.TableName ?? SnakeCaseNaming.Of(type.Name);
        return table.Replace("_{year}{month}{day}", "_20261101", StringComparison.Ordinal);
    }

    [Fact]
    public void Indexes_FollowTheSoftDeleteRule_NameLimit_AndPointAtRealProperties()
    {
        var types = EntityTypes();
        Assert.True(types.Count >= 60, $"Expected the BasicApp entities, found {types.Count}.");
        var problems = new List<string>();
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        void Name(Type type, string name)
        {
            if (name.Length > ActiveUniqueIndexes.MaxNameLength)
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
                    problems.Add($"{type.Name}: unique [SugarIndex] '{index.IndexName}' on a soft-deletable entity; use [ActiveUniqueIndex].");
                }

                problems.AddRange(index.IndexFields.Keys.Where(field => type.GetProperty(field) is null).Select(field => $"{type.Name}: [SugarIndex] '{index.IndexName}' names a missing property '{field}'."));
                Name(type, index.IndexName.Replace("{table}", table, StringComparison.Ordinal).Replace("{split_table}", table, StringComparison.Ordinal));
            }

            foreach (var index in type.GetCustomAttributes<ActiveUniqueIndexAttribute>(inherit: false))
            {
                if (!softDeletable)
                {
                    problems.Add($"{type.Name}: [ActiveUniqueIndex] on an entity without soft delete; use a unique [SugarIndex].");
                }

                problems.AddRange(index.Columns.Where(column => type.GetProperty(column) is null).Select(column => $"{type.Name}: [ActiveUniqueIndex] names a missing property '{column}'."));
                Name(type, index.Name ?? ActiveUniqueIndexes.NameOf(table, index.Columns.Select(SnakeCaseNaming.Of)));
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public async Task ActiveUniqueIndex_RejectsDuplicateLiveRows_ButKeepsAnyNumberOfDeletedOnes()
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
}

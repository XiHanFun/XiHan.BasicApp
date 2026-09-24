// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using System.Reflection;
using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Application.AppServices;
using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 零代码运行时读原始表，不经实体读过滤：租户与软删除条件必须按实体读过滤同口径显式补上（真实 SQLite）。
/// </summary>
public sealed class DynamicRuntimeTenancySqlTests : IDisposable
{
    private const long TenantTableId = 1;

    private const long PlainTableId = 2;

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-runtime-{Guid.NewGuid():N}.db");

    private readonly SqlSugarClient _client;

    public DynamicRuntimeTenancySqlTests()
    {
        _client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={_databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true
        });

        _ = _client.Ado.ExecuteCommand("CREATE TABLE biz_note (id INTEGER PRIMARY KEY, title TEXT, tenant_id INTEGER NOT NULL, is_deleted INTEGER NOT NULL)");
        _ = _client.Ado.ExecuteCommand("""
            INSERT INTO biz_note (id, title, tenant_id, is_deleted) VALUES
              (1, '平台模板', 0, 0),
              (2, '租户7', 7, 0),
              (3, '租户7已删', 7, 1),
              (4, '租户8', 8, 0)
            """);
        _ = _client.Ado.ExecuteCommand("CREATE TABLE plain_note (id INTEGER PRIMARY KEY, title TEXT)");
        _ = _client.Ado.ExecuteCommand("INSERT INTO plain_note (id, title) VALUES (1, '无租户列')");
    }

    /// <summary>
    /// 业务表严格隔离：租户只看本租户的行，看不到平台、别的租户与已删除行
    /// </summary>
    [Fact]
    public async Task TenantContext_SeesOnlyOwnRows()
    {
        var result = await CreateService(tenantId: 7).GetPageAsync(new DynamicRuntimePageQueryDto { TableId = TenantTableId });

        Assert.Equal(["租户7"], Titles(result));
    }

    /// <summary>
    /// 平台只看平台自己的行
    /// </summary>
    [Fact]
    public async Task PlatformContext_SeesOnlyPlatformRows()
    {
        var result = await CreateService(tenantId: null).GetPageAsync(new DynamicRuntimePageQueryDto { TableId = TenantTableId });

        Assert.Equal(["平台模板"], Titles(result));
    }

    /// <summary>
    /// 没有租户列的表不归属任何租户：租户上下文拒绝访问
    /// </summary>
    [Fact]
    public async Task TenantContext_TableWithoutTenantColumn_IsRejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(tenantId: 7).GetPageAsync(new DynamicRuntimePageQueryDto { TableId = PlainTableId }));
    }

    /// <summary>
    /// 没有租户列的表在平台可读
    /// </summary>
    [Fact]
    public async Task PlatformContext_TableWithoutTenantColumn_IsReadable()
    {
        var result = await CreateService(tenantId: null).GetPageAsync(new DynamicRuntimePageQueryDto { TableId = PlainTableId });

        Assert.Equal(["无租户列"], Titles(result));
    }

    public void Dispose()
    {
        _client.Ado.Connection.Close();
        _client.Dispose();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private static List<string?> Titles(DynamicRuntimePageResultDto result) =>
        [.. result.Rows.OrderBy(row => Convert.ToInt64(row["id"])).Select(row => row["title"]?.ToString())];

    private DynamicRuntimeAppService CreateService(long? tenantId)
    {
        var tables = new Mock<ICodeGenTableRepository>();
        tables.Setup(repo => repo.GetByIdAsync(TenantTableId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTable(TenantTableId, "biz_note"));
        tables.Setup(repo => repo.GetByIdAsync(PlainTableId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTable(PlainTableId, "plain_note"));

        var columns = new Mock<ICodeGenTableColumnRepository>();
        columns.Setup(repo => repo.GetByTableIdAsync(TenantTableId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Column("id"), Column("title"), Column("tenant_id"), Column("is_deleted")]);
        columns.Setup(repo => repo.GetByTableIdAsync(PlainTableId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Column("id"), Column("title")]);

        var resolver = new Mock<ISqlSugarClientResolver>();
        resolver.Setup(item => item.GetCurrentClient()).Returns(_client);

        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(tenant => tenant.Id).Returns(tenantId);

        return new DynamicRuntimeAppService(tables.Object, columns.Object, resolver.Object, currentTenant.Object);
    }

    private static SysCodeGenTable CreateTable(long id, string tableName)
    {
        var table = new SysCodeGenTable { TableName = tableName, Status = EnableStatus.Enabled };
        typeof(SysCodeGenTable).GetProperty("BasicId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(table, id);
        return table;
    }

    private static SysCodeGenTableColumn Column(string name) => new() { ColumnName = name };
}

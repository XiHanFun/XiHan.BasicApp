// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.Reflection;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模板 CodeFirst 数据库约束测试。
/// </summary>
public sealed class PrintTemplateDatabaseConstraintTests : IDisposable
{
    private const string TemplateJson = "{\"panels\":[{\"printElements\":[]}]}";
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-print-template-{Guid.NewGuid():N}.db");
    private readonly SqlSugarClient _client;

    /// <summary>
    /// 创建临时 SQLite 数据库并通过生产实体执行 CodeFirst 建表。
    /// </summary>
    public PrintTemplateDatabaseConstraintTests()
    {
        _client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={_databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = false
        });
        _client.CodeFirst.InitTables<SysPrintTemplate>();
    }

    /// <summary>
    /// 同租户活动模板编码必须唯一，而不同租户可以使用相同业务编码。
    /// </summary>
    [Fact]
    public void UniqueIndex_ShouldIsolateTemplateCodeByTenant()
    {
        _ = _client.Insertable(CreateTemplate(1, 7, "ORDER")).ExecuteCommand();

        _ = Assert.ThrowsAny<Exception>(
            () => _client.Insertable(CreateTemplate(2, 7, "ORDER")).ExecuteCommand());
        var inserted = _client.Insertable(CreateTemplate(3, 8, "ORDER")).ExecuteCommand();

        Assert.Equal(1, inserted);
        Assert.Equal(2, _client.Queryable<SysPrintTemplate>().Count());
    }

    /// <summary>
    /// CodeFirst 模型必须允许 NULL 数据源，使自由模板无需伪造代码注册项。
    /// </summary>
    [Fact]
    public void DataSourceCode_ShouldAllowNull()
    {
        var template = CreateTemplate(10, 7, "FREE");
        template.DataSourceCode = null;

        var inserted = _client.Insertable(template).ExecuteCommand();
        var saved = _client.Queryable<SysPrintTemplate>()
            .First(value => value.BasicId == template.BasicId);

        Assert.Equal(1, inserted);
        Assert.Null(saved.DataSourceCode);
    }

    /// <summary>
    /// 释放占用的资源
    /// </summary>
    public void Dispose()
    {
        _client.Ado.Connection.Close();
        _client.Dispose();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    /// <summary>
    /// 创建约束测试实体并模拟 ORM 主键回填。
    /// </summary>
    private static SysPrintTemplate CreateTemplate(long id, long tenantId, string code)
    {
        var template = new SysPrintTemplate
        {
            TenantId = tenantId,
            TemplateCode = code,
            DataSourceCode = "system.print-demo",
            TemplateName = code,
            TemplateJson = TemplateJson,
            EngineVersion = "0.0.60",
            Status = EnableStatus.Enabled,
            CreatedTime = DateTimeOffset.UnixEpoch
        };
        typeof(SysPrintTemplate)
            .GetProperty(
                nameof(SysPrintTemplate.BasicId),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(template, id);
        return template;
    }
}

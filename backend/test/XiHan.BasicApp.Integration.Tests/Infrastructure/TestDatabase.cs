// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Runtime.Persistence;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 每个测试一个临时库
/// </summary>
/// <remarks>
/// 缺省是 SQLite 临时文件。环境变量 <c>XIHAN_TEST_DB</c> 设为 <c>postgresql</c>、<c>mysql</c>、<c>sqlserver</c> 或 <c>oracle</c> 时，
/// 改在 <c>XIHAN_TEST_POSTGRES</c>、<c>XIHAN_TEST_MYSQL</c>、<c>XIHAN_TEST_SQLSERVER</c>、<c>XIHAN_TEST_ORACLE</c> 指向的服务器上建临时库（Oracle 是临时用户），整套用例换一种库跑；
/// 选了库却没配服务器即抛出，不悄悄退回 SQLite
/// </remarks>
public sealed class TestDatabase : IDisposable
{
    /// <summary>
    /// 选库的环境变量
    /// </summary>
    public const string Variable = "XIHAN_TEST_DB";

    private readonly SqliteDatabase? _sqlite;
    private readonly ExternalDatabase? _external;

    /// <summary>
    /// 建一个临时库
    /// </summary>
    public TestDatabase()
    {
        if (Provider == "sqlite")
        {
            _sqlite = new SqliteDatabase();
            return;
        }

        _external = ExternalDatabase.CreateRequired(Provider);
    }

    /// <summary>
    /// 这一轮用例用的库类型名（数据库绑定的 <c>provider</c>）
    /// </summary>
    public static string Provider { get; } = Environment.GetEnvironmentVariable(Variable) is { Length: > 0 } selected
        ? selected.Trim().ToLowerInvariant() switch
        {
            "postgresql" or "postgres" => "postgresql",
            "mysql" => "mysql",
            "sqlserver" or "mssql" => "sqlserver",
            "oracle" => "oracle",
            "sqlite" => "sqlite",
            var other => throw new InvalidOperationException($"{Variable}='{other}' is not one of sqlite, postgresql, mysql, sqlserver, oracle."),
        }
        : "sqlite";

    /// <summary>
    /// 是否在 SQLite 上跑
    /// </summary>
    public static bool IsSqlite => Provider == "sqlite";

    /// <summary>
    /// 临时库的连接串
    /// </summary>
    public string ConnectionString => _sqlite?.ConnectionString ?? _external!.ConnectionString;

    /// <summary>
    /// 直接连临时库的客户端
    /// </summary>
    /// <remarks>
    /// 给用例绕过宿主核对库里的行；不带框架约定，语句按各库都认的写法写
    /// </remarks>
    public SqlSugarClient Client()
        => new(new ConnectionConfig { DbType = DatabaseProviders.Parse(Provider), ConnectionString = ConnectionString, IsAutoCloseConnection = true });

    /// <inheritdoc />
    public void Dispose()
    {
        _sqlite?.Dispose();
        _external?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

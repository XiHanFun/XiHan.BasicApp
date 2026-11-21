// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data.Common;
using SqlSugar;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 外部库上的临时库（代码生成读库结构的实测）
/// </summary>
/// <remarks>
/// 照框架同名测试的做法：PostgreSQL、MySQL、SQL Server 各建一个临时库，Oracle 建一个临时用户（12c 及以上）；服务器来自环境变量
/// <c>XIHAN_TEST_POSTGRES</c>、<c>XIHAN_TEST_MYSQL</c>、<c>XIHAN_TEST_SQLSERVER</c>（连接串不带库名）与 <c>XIHAN_TEST_ORACLE</c>（能建用户的账号），没设即跳过
/// <see cref="DatabaseType"/> 是代码生成数据源的数据库类型名
/// </remarks>
public sealed class ExternalDatabase : IAsyncDisposable
{
    private readonly DbType _type;
    private readonly string _server;
    private readonly string _name;

    private ExternalDatabase(DbType type, string server, string name, string databaseType, string connectionString)
    {
        _type = type;
        _server = server;
        _name = name;
        DatabaseType = databaseType;
        ConnectionString = connectionString;
    }

    /// <summary>
    /// 代码生成数据源的数据库类型名
    /// </summary>
    public string DatabaseType { get; }

    /// <summary>
    /// 临时库的连接串
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// 在环境变量指向的服务器上建一个临时库，没设即跳过用例
    /// </summary>
    /// <param name="databaseType">数据库类型名：PostgreSql、MySql、SqlServer 或 Oracle</param>
    public static ExternalDatabase Create(string databaseType)
    {
        var (variable, _) = Server(databaseType);
        Assert.SkipWhen(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)), $"{variable} is not set; this case only runs against a real {databaseType} server.");
        return CreateOn(databaseType);
    }

    /// <summary>
    /// 在环境变量指向的服务器上建一个临时库，没设即抛出
    /// </summary>
    /// <remarks>
    /// 给整套用例换库（<see cref="TestDatabase"/>）用：选了库却没配服务器是配置错误，不跳过
    /// </remarks>
    /// <param name="provider">数据库绑定的库类型名：postgresql、mysql、sqlserver 或 oracle</param>
    public static ExternalDatabase CreateRequired(string provider)
    {
        var databaseType = provider switch
        {
            "postgresql" => "PostgreSql",
            "mysql" => "MySql",
            "sqlserver" => "SqlServer",
            "oracle" => "Oracle",
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
        };
        var (variable, _) = Server(databaseType);
        return string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable))
            ? throw new InvalidOperationException($"{TestDatabase.Variable}={provider} needs {variable} pointing at a {databaseType} server.")
            : CreateOn(databaseType);
    }

    private static (string Variable, DbType Type) Server(string databaseType) => databaseType switch
    {
        "PostgreSql" => (PostgresDatabase.Variable, DbType.PostgreSQL),
        "MySql" => ("XIHAN_TEST_MYSQL", DbType.MySql),
        "SqlServer" => ("XIHAN_TEST_SQLSERVER", DbType.SqlServer),
        "Oracle" => ("XIHAN_TEST_ORACLE", DbType.Oracle),
        _ => throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, null),
    };

    private static ExternalDatabase CreateOn(string databaseType)
    {
        var (variable, type) = Server(databaseType);
        var server = Environment.GetEnvironmentVariable(variable)!.TrimEnd(';');
        if (type == DbType.Oracle)
        {
            var user = $"CG_{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            var password = $"P{Guid.NewGuid():N}"[..20];
            using (var maintenance = Client(type, server))
            {
                maintenance.Ado.ExecuteCommand($"CREATE USER {user} IDENTIFIED BY \"{password}\" QUOTA UNLIMITED ON USERS");
                maintenance.Ado.ExecuteCommand($"GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE TO {user}");
            }

            var builder = new DbConnectionStringBuilder { ConnectionString = server };
            builder["User Id"] = user;
            builder["Password"] = password;
            // 不用连接池：删用户时不能有它的会话（同框架用例）
            builder["Pooling"] = "false";
            return new ExternalDatabase(type, server, user, databaseType, builder.ConnectionString);
        }

        var name = $"cg_{Guid.NewGuid():N}";
        using (var maintenance = Client(type, MaintenanceOf(type, server)))
        {
            maintenance.Ado.ExecuteCommand(type switch
            {
                DbType.MySql => $"CREATE DATABASE `{name}` CHARACTER SET utf8mb4",
                DbType.SqlServer => $"CREATE DATABASE [{name}]",
                _ => $"CREATE DATABASE \"{name}\"",
            });

            // 读已提交按快照读，与正式部署的要求一致（宿主启动时建表在结构升级之前，升级看到已有表的库没打开会失败）
            // 强制参数化：SqlSugar 建表时逐列按字面量查一遍列说明，每条都要现编译（一个库约 1700 条、每条十几毫秒），参数化后同形语句共用一份计划
            if (type == DbType.SqlServer)
            {
                maintenance.Ado.ExecuteCommand($"ALTER DATABASE [{name}] SET READ_COMMITTED_SNAPSHOT ON");
                maintenance.Ado.ExecuteCommand($"ALTER DATABASE [{name}] SET PARAMETERIZATION FORCED");
            }
        }

        return new ExternalDatabase(type, server, name, databaseType, $"{server};Database={name}");
    }

    /// <summary>
    /// 在临时库里执行一组语句
    /// </summary>
    /// <param name="statements">语句</param>
    public void Execute(params string[] statements)
    {
        using var client = Client(_type, ConnectionString);
        foreach (var statement in statements)
        {
            client.Ado.ExecuteCommand(statement);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        SqlConnectionPools.Clear();
        using var maintenance = Client(_type, _type == DbType.Oracle ? _server : MaintenanceOf(_type, _server));
        if (_type == DbType.Oracle)
        {
            // 还连着的会话（宿主停下前后台作业开的）会挡住 DROP USER（ORA-01940）
            foreach (var session in maintenance.Ado.SqlQuery<string>("SELECT sid || ',' || serial# FROM v$session WHERE username = :u", new SugarParameter(":u", _name)))
            {
                maintenance.Ado.ExecuteCommand($"ALTER SYSTEM KILL SESSION '{session}' IMMEDIATE");
            }
        }

        maintenance.Ado.ExecuteCommand(_type switch
        {
            DbType.Oracle => $"DROP USER {_name} CASCADE",
            DbType.MySql => $"DROP DATABASE IF EXISTS `{_name}`",
            DbType.SqlServer => $"ALTER DATABASE [{_name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_name}]",
            _ => $"DROP DATABASE IF EXISTS \"{_name}\" WITH (FORCE)",
        });
        TestFileStorage.Delete(ConnectionString);
        return ValueTask.CompletedTask;
    }

    private static string MaintenanceOf(DbType type, string server) => type switch
    {
        DbType.MySql => $"{server};Database=mysql",
        DbType.SqlServer => $"{server};Database=master",
        _ => $"{server};Database=postgres",
    };

    private static SqlSugarClient Client(DbType type, string connectionString)
        => new(new ConnectionConfig { DbType = type, ConnectionString = connectionString, IsAutoCloseConnection = true });

    private static class SqlConnectionPools
    {
        public static void Clear()
        {
            MySqlConnector.MySqlConnection.ClearAllPools();
            Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
            Oracle.ManagedDataAccess.Client.OracleConnection.ClearAllPools();
            Npgsql.NpgsqlConnection.ClearAllPools();
        }
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 数据库绑定的 <c>provider</c> 属性到 SqlSugar 库类型的映射
/// </summary>
/// <remarks>
/// <c>provider</c> 也是租户记录的 <c>DatabaseProvider</c> 属性
/// </remarks>
public static class DatabaseProviders
{
    /// <summary>
    /// PostgreSQL：缺省库
    /// </summary>
    public const string PostgreSql = "postgresql";

    /// <summary>
    /// SQLite：测试用
    /// </summary>
    public const string Sqlite = "sqlite";

    /// <summary>
    /// MySQL
    /// </summary>
    public const string MySql = "mysql";

    /// <summary>
    /// SQL Server
    /// </summary>
    public const string SqlServer = "sqlserver";

    /// <summary>
    /// Oracle
    /// </summary>
    public const string Oracle = "oracle";

    /// <summary>
    /// 正式环境支持的库类型名
    /// </summary>
    /// <remarks>
    /// 升级脚本按这些名字分目录（<c>Schema/&lt;版本&gt;/&lt;库类型&gt;/</c>）；SQLite 只给测试，不在其列
    /// </remarks>
    public static IReadOnlyList<string> Production { get; } = [PostgreSql, MySql, SqlServer, Oracle];

    /// <summary>
    /// 按名字取库类型
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> 或空取 PostgreSQL
    /// </remarks>
    /// <exception cref="FrameworkException">不认识的名字是装配错误</exception>
    public static DbType Parse(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return DbType.PostgreSQL;
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            PostgreSql or "postgres" or "pgsql" or "npgsql" => DbType.PostgreSQL,
            Sqlite => DbType.Sqlite,
            MySql => DbType.MySql,
            SqlServer or "mssql" => DbType.SqlServer,
            Oracle => DbType.Oracle,
            _ => throw new FrameworkException(RuntimeErrors.DatabaseProviderUnsupported, $"Database provider '{provider}' is not supported; use one of postgresql, mysql, sqlserver, oracle, sqlite."),
        };
    }

    /// <summary>
    /// 库类型的规范名
    /// </summary>
    /// <remarks>
    /// 与 <see cref="Parse"/> 互逆，升级脚本的库类型目录用它
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">不是这里支持的库类型</exception>
    public static string NameOf(DbType dbType) => dbType switch
    {
        DbType.PostgreSQL => PostgreSql,
        DbType.Sqlite => Sqlite,
        DbType.MySql => MySql,
        DbType.SqlServer => SqlServer,
        DbType.Oracle => Oracle,
        _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, "Unsupported database type."),
    };
}

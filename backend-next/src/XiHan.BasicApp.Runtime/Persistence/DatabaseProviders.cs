// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 数据库绑定的 <c>provider</c> 属性到 SqlSugar 库类型的映射。
/// </summary>
/// <remarks>
/// <c>provider</c> 也是租户记录的 <c>DatabaseProvider</c> 属性。
/// </remarks>
public static class DatabaseProviders
{
    /// <summary>
    /// PostgreSQL：生产与开发的缺省库。
    /// </summary>
    public const string PostgreSql = "postgresql";

    /// <summary>
    /// SQLite：测试用。
    /// </summary>
    public const string Sqlite = "sqlite";

    /// <summary>
    /// MySQL。
    /// </summary>
    public const string MySql = "mysql";

    /// <summary>
    /// SQL Server。
    /// </summary>
    public const string SqlServer = "sqlserver";

    /// <summary>
    /// 按名字取库类型。
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> 或空取 PostgreSQL。
    /// </remarks>
    /// <exception cref="FrameworkException">不认识的名字是装配错误。</exception>
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
            _ => throw new FrameworkException(RuntimeErrors.DatabaseProviderUnsupported, $"Database provider '{provider}' is not supported; use one of postgresql, sqlite, mysql, sqlserver."),
        };
    }
}

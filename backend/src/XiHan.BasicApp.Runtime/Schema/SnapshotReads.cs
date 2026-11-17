// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// SQL Server 的读已提交按快照读
/// </summary>
/// <remarks>
/// 框架的工作单元要求未提交的写入对别的工作单元不可见、也不挡读，与 PostgreSQL、MySQL、Oracle 的读已提交一致；
/// SQL Server 缺省的读已提交是加锁读，读要等写入方提交，独立于调用事务的读（<c>StoreScope</c>）碰上本次调用写过的行会一直等到语句超时
/// 库要打开 <c>READ_COMMITTED_SNAPSHOT</c>：还没有表的新库由升级在建表之前打开，已有表的库没打开即升级失败，由 DBA 在维护窗口里打开。其他库类型直接通过
/// </remarks>
internal static class SnapshotReads
{
    /// <summary>
    /// 核对或打开库的快照读
    /// </summary>
    /// <param name="client">库的连接</param>
    /// <param name="failure">失败用的错误码（平台库或租户库）</param>
    /// <returns>已打开或刚打开即成功；已有表的库没打开即失败，消息里带要执行的语句</returns>
    public static Result Ensure(ISqlSugarClient client, string failure)
    {
        if (client.CurrentConnectionConfig.DbType != DbType.SqlServer
            || client.Ado.GetInt("SELECT CAST(is_read_committed_snapshot_on AS int) FROM sys.databases WHERE name = DB_NAME()") == 1)
        {
            return Result.Success();
        }

        var database = client.Ado.GetString("SELECT DB_NAME()");
        if (client.Ado.GetInt("SELECT COUNT(*) FROM sys.tables") == 0)
        {
            // 空库只有这次升级在用，断开别的会话不丢数据；改库选项不能在事务里执行
            client.Ado.ExecuteCommand("ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE");
            return Result.Success();
        }

        return Error.Literal(failure, $"SQL Server database '{database}' must have READ_COMMITTED_SNAPSHOT on; run: ALTER DATABASE [{database}] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE");
    }
}

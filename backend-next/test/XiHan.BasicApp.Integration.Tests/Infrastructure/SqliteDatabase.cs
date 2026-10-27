// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Data.Sqlite;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 每个测试一个临时 SQLite 文件；释放时清空连接池再删文件（Windows 上池化连接持有文件句柄；宿主停止后后台的发件箱 / 审计批次可能还在收尾，删文件带短暂重试）。
/// </summary>
public sealed class SqliteDatabase : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"xihan-basicapp-{Guid.NewGuid():N}.db");

    public string ConnectionString => $"Data Source={Path}";

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
        {
            DeleteWithRetry(Path + suffix);
        }
    }

    private static void DeleteWithRetry(string path)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Delete(path);
                return;
            }
            catch (IOException) when (attempt < 10)
            {
                // 临时库文件还被收尾中的连接持有：等一下再删，最多约 1 秒
                SqliteConnection.ClearAllPools();
                Thread.Sleep(100);
            }
        }
    }
}

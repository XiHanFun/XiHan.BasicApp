// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Data.Sqlite;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 每个测试一个临时 SQLite 文件；释放时清空连接池再删文件（Windows 上池化连接持有文件句柄）。
/// </summary>
public sealed class SqliteDatabase : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"xihan-basicapp-{Guid.NewGuid():N}.db");

    public string ConnectionString => $"Data Source={Path}";

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(Path);
        File.Delete(Path + "-journal");
        File.Delete(Path + "-wal");
        File.Delete(Path + "-shm");
    }
}

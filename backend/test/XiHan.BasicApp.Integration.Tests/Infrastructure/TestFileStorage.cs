// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 临时库配套的本地文件存储目录
/// </summary>
/// <remarks>
/// 文件库与框架缺省对象存储的根目录按测试服务器连的库各取一个：连同一个库的宿主（重启、多实例）共用，别的用例看不到
/// 文件系统存储启动时把根目录里的临时文件当作上次崩溃留下的删掉，所有宿主共用输出目录下的缺省根目录时，后起的宿主会删掉别的用例正写到一半的文件
/// 随临时库一起删
/// </remarks>
public static class TestFileStorage
{
    /// <summary>
    /// 这个库的存储根目录
    /// </summary>
    /// <param name="connectionString">临时库的连接串</param>
    public static string RootOf(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(connectionString)))[..16];
        return Path.Combine(Path.GetTempPath(), $"xihan-basicapp-storage-{hash}");
    }

    /// <summary>
    /// 文件库的本地根目录（<c>BasicApp:Files:LocalRootPath</c>）
    /// </summary>
    /// <param name="connectionString">临时库的连接串</param>
    public static string FilesOf(string connectionString) => Path.Combine(RootOf(connectionString), "files");

    /// <summary>
    /// 框架缺省对象存储的根目录（<c>XiHan:ObjectStorage:FileSystem:RootPath</c>）
    /// </summary>
    /// <param name="connectionString">临时库的连接串</param>
    public static string BlobsOf(string connectionString) => Path.Combine(RootOf(connectionString), "blobs");

    /// <summary>
    /// 删掉这个库的存储目录
    /// </summary>
    /// <param name="connectionString">临时库的连接串</param>
    public static void Delete(string connectionString)
    {
        var root = RootOf(connectionString);
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

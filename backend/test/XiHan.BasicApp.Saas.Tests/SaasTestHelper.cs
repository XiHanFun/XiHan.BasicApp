// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 测试夹具共享工具。
/// </summary>
public static class SaasTestHelper
{
    /// <summary>
    /// 测试中模拟持久化层回填受保护的实体主键。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="entity">待回填主键的实体。</param>
    /// <param name="id">主键值。</param>
    public static void SetBasicId<TEntity>(TEntity entity, long id)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var property = typeof(TEntity).GetProperty(
            "BasicId",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"未找到 {typeof(TEntity).Name} 的主键属性。");
        property.SetValue(entity, id);
    }

    /// <summary>
    /// 删除用例的临时 SQLite 库文件，句柄尚未释放时短暂重试。
    /// </summary>
    /// <param name="databasePath">库文件路径。</param>
    public static void DeleteTemporaryDatabase(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath) || !File.Exists(databasePath))
        {
            return;
        }

        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                File.Delete(databasePath);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(25);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(25);
            }
        }
    }

}

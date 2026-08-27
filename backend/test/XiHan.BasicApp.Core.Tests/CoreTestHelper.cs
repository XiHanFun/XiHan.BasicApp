// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using SqlSugar;

namespace XiHan.BasicApp.Core.Tests;

/// <summary>
/// 核心库测试夹具共享工具。
/// </summary>
/// <remarks>
/// 本测试项目的断言几乎全部是「形状断言」：属性存在与否、SqlSugar 列特性的标志位、继承链。
/// 把这些反射细节收在一处，避免每个测试文件各写一份 BindingFlags 组合而出现口径不一致
/// （尤其是 <see cref="BindingFlags.FlattenHierarchy"/>：漏掉它会让基类上声明的属性查不到，
/// 从而把「继承来的属性」误判成「不存在」，负向断言会假绿）。
/// </remarks>
public static class CoreTestHelper
{
    /// <summary>
    /// 查找公开实例属性（含继承自基类的属性）。
    /// </summary>
    /// <param name="type">被查找的类型。</param>
    /// <param name="propertyName">属性名称。</param>
    /// <returns>找到返回属性信息，未找到返回 null。</returns>
    public static PropertyInfo? FindProperty(Type type, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
    }

    /// <summary>
    /// 查找公开实例属性，找不到直接失败（用于正向断言的前置条件）。
    /// </summary>
    /// <param name="type">被查找的类型。</param>
    /// <param name="propertyName">属性名称。</param>
    /// <returns>属性信息。</returns>
    public static PropertyInfo RequireProperty(Type type, string propertyName)
    {
        return FindProperty(type, propertyName)
            ?? throw new InvalidOperationException($"类型 {type.FullName} 上未找到属性 {propertyName}。");
    }

    /// <summary>
    /// 读取属性上的 SqlSugar 列特性，找不到直接失败。
    /// </summary>
    /// <remarks>
    /// 列特性写在框架基类的 <c>override</c> 属性上，因此必须从声明该 override 的那一层读取；
    /// <see cref="PropertyInfo.GetCustomAttribute{T}()"/> 默认沿继承链查找，可以正确取到。
    /// </remarks>
    /// <param name="type">被查找的类型。</param>
    /// <param name="propertyName">属性名称。</param>
    /// <returns>列特性。</returns>
    public static SugarColumn RequireSugarColumn(Type type, string propertyName)
    {
        var property = RequireProperty(type, propertyName);
        return property.GetCustomAttribute<SugarColumn>(true)
            ?? throw new InvalidOperationException($"属性 {type.FullName}.{propertyName} 上未标注 SugarColumn。");
    }

    /// <summary>
    /// 模拟持久化层回填受保护的实体主键。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="entity">待回填主键的实体。</param>
    /// <param name="id">主键值。</param>
    public static void SetBasicId<TEntity>(TEntity entity, long id)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var property = typeof(TEntity).GetProperty(
            "BasicId",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
            ?? throw new InvalidOperationException($"未找到 {typeof(TEntity).Name} 的主键属性。");
        property.SetValue(entity, id);
    }

    /// <summary>
    /// 展开一条类型的继承链（从自身开始，直到但不包含 <see cref="object"/>）。
    /// </summary>
    /// <param name="type">起始类型。</param>
    /// <returns>继承链上的类型序列。</returns>
    public static IReadOnlyList<Type> GetInheritanceChain(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        List<Type> chain = [];
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            chain.Add(current);
        }

        return chain;
    }

    /// <summary>
    /// 读取核心库源码文件的原始文本（UTF-8 只读）。
    /// </summary>
    /// <remarks>
    /// 以本文件位置为锚点向上回溯定位 src，不依赖运行目录与工作目录：
    /// <c>backend/test/XiHan.BasicApp.Core.Tests/</c> → <c>backend/src/framework/XiHan.BasicApp.Core/</c>。
    /// </remarks>
    /// <param name="relativePath">相对于核心库工程根目录的路径片段。</param>
    /// <param name="testFilePath">调用方源文件路径，由编译器填充，不要显式传值。</param>
    /// <returns>源码文本。</returns>
    public static string ReadCoreSourceText(string relativePath, [CallerFilePath] string testFilePath = "")
    {
        var testDirectory = Path.GetDirectoryName(testFilePath)
            ?? throw new InvalidOperationException("无法解析测试源文件目录。");

        var fullPath = Path.GetFullPath(Path.Combine(
            testDirectory, "..", "..", "src", "framework", "XiHan.BasicApp.Core", relativePath));

        return File.ReadAllText(fullPath, Encoding.UTF8);
    }
}

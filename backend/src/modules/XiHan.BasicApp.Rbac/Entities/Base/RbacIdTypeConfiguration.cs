#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacIdTypeConfiguration
// Guid:8b28152c-d6e9-4396-addb-b479254bad98
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

/// <summary>
/// 全局 ID 类型别名
/// 这是实现统一 ID 类型的核心：通过 global using 实现全局类型别名
///
/// 使用方法：
/// 1. 修改下面的 using 语句来切换整个模块的 ID 类型
/// 2. 重新编译项目即可应用新的 ID 类型
///
/// 常用配置：
/// - 使用 long：  global using RbacIdType = System.Int64;
/// - 使用 int：   global using RbacIdType = System.Int32;
/// - 使用 Guid：  global using RbacIdType = System.Guid;
/// - 使用 string：global using RbacIdType = System.String;
/// </summary>
global using RbacIdType = System.Int64;

namespace XiHan.BasicApp.Rbac.Entities.Base;

/// <summary>
/// RBAC 模块 ID 类型统一配置
/// 这是推荐的最佳实践方案：简单、统一、易维护
/// </summary>
public static class RbacIdTypeConfiguration
{
    /// <summary>
    /// 当前项目使用的 ID 类型
    /// 修改此属性可以全局切换整个 RBAC 模块的 ID 类型
    /// </summary>
    public static readonly Type CurrentIdType = typeof(long);

    /// <summary>
    /// 获取当前配置的 ID 类型名称
    /// </summary>
    public static string CurrentIdTypeName => CurrentIdType.Name;

    /// <summary>
    /// 验证给定类型是否为当前配置的 ID 类型
    /// </summary>
    /// <param name="type">要验证的类型</param>
    /// <returns>是否匹配</returns>
    public static bool IsCurrentIdType(Type type) => type == CurrentIdType;

    /// <summary>
    /// 验证给定类型是否为当前配置的 ID 类型
    /// </summary>
    /// <typeparam name="T">要验证的类型</typeparam>
    /// <returns>是否匹配</returns>
    public static bool IsCurrentIdType<T>() => typeof(T) == CurrentIdType;
}

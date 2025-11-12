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
    public static readonly Type CurrentIdType = typeof(RbacIdType);

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

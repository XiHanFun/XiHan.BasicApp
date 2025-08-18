#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacEntityConfiguration
// Guid:bc28152c-d6e9-4396-addb-b479254bad95
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Reflection;

namespace XiHan.BasicApp.Rbac.Entities.Base;

/// <summary>
/// RBAC 实体配置管理器
/// 支持运行时动态配置 ID 类型
/// </summary>
public static class RbacEntityConfiguration
{
    private static readonly Dictionary<Type, Type> EntityIdTypes = [];
    private static Type DefaultIdType = typeof(long);

    /// <summary>
    /// 设置默认 ID 类型
    /// </summary>
    /// <typeparam name="T">ID 类型</typeparam>
    public static void SetDefaultIdType<T>() where T : IEquatable<T>
    {
        DefaultIdType = typeof(T);
    }

    /// <summary>
    /// 为特定实体类型设置 ID 类型
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">ID 类型</typeparam>
    public static void SetEntityIdType<TEntity, TKey>()
        where TEntity : class
        where TKey : IEquatable<TKey>
    {
        EntityIdTypes[typeof(TEntity)] = typeof(TKey);
    }

    /// <summary>
    /// 获取实体的 ID 类型
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>ID 类型</returns>
    public static Type GetIdType(Type entityType)
    {
        // 首先检查特定配置
        if (EntityIdTypes.TryGetValue(entityType, out var specificType))
        {
            return specificType;
        }

        // 检查特性配置
        var attribute = entityType.GetCustomAttribute<RbacIdTypeAttribute>();
        if (attribute != null)
        {
            return attribute.IdType;
        }

        // 返回默认类型
        return DefaultIdType;
    }

    /// <summary>
    /// 获取实体的 ID 类型
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns>ID 类型</returns>
    public static Type GetIdType<TEntity>() where TEntity : class
    {
        return GetIdType(typeof(TEntity));
    }

    /// <summary>
    /// 批量配置实体ID类型
    /// </summary>
    /// <param name="configurations">配置字典</param>
    public static void ConfigureEntityIdTypes(Dictionary<Type, Type> configurations)
    {
        foreach (var (entityType, idType) in configurations)
        {
            EntityIdTypes[entityType] = idType;
        }
    }

    /// <summary>
    /// 清除所有配置
    /// </summary>
    public static void ClearConfigurations()
    {
        EntityIdTypes.Clear();
        DefaultIdType = typeof(long);
    }
}

/// <summary>
/// 配置扩展方法
/// </summary>
public static class RbacEntityConfigurationExtensions
{
    /// <summary>
    /// 链式配置 ID 类型
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">ID 类型</typeparam>
    /// <param name="configurations">配置字典</param>
    /// <returns>配置字典</returns>
    public static Dictionary<Type, Type> WithIdType<TEntity, TKey>(
        this Dictionary<Type, Type> configurations)
        where TEntity : class
        where TKey : IEquatable<TKey>
    {
        configurations[typeof(TEntity)] = typeof(TKey);
        return configurations;
    }
}

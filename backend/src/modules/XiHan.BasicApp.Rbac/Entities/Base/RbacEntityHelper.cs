#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacEntityHelper
// Guid:dc28152c-d6e9-4396-addb-b479254bad93
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Reflection;

namespace XiHan.BasicApp.Rbac.Entities.Base;

/// <summary>
/// RBAC 实体帮助类
/// 提供实体相关的实用工具方法
/// </summary>
public static class RbacEntityHelper
{
    /// <summary>
    /// 获取当前配置的 ID 类型
    /// </summary>
    /// <returns>ID 类型</returns>
    public static Type GetCurrentIdType() => RbacIdTypeConfiguration.CurrentIdType;

    /// <summary>
    /// 获取当前配置的 ID 类型名称
    /// </summary>
    /// <returns>ID 类型名称</returns>
    public static string GetCurrentIdTypeName() => RbacIdTypeConfiguration.CurrentIdTypeName;

    /// <summary>
    /// 创建默认 ID 值
    /// </summary>
    /// <returns>默认 ID 值</returns>
    public static RbacIdType CreateDefaultId()
    {
        var idType = typeof(RbacIdType);
        
        if (idType == typeof(long)) return (RbacIdType)(object)0L;
        if (idType == typeof(int)) return (RbacIdType)(object)0;
        if (idType == typeof(Guid)) return (RbacIdType)(object)Guid.Empty;
        if (idType == typeof(string)) return (RbacIdType)(object)string.Empty;
        
        // 对于其他类型，尝试使用默认构造函数
        try
        {
            return (RbacIdType)Activator.CreateInstance(idType)!;
        }
        catch
        {
            return default(RbacIdType)!;
        }
    }

    /// <summary>
    /// 创建新的 ID 值
    /// </summary>
    /// <returns>新的 ID 值</returns>
    public static RbacIdType CreateNewId()
    {
        var idType = typeof(RbacIdType);
        
        if (idType == typeof(long)) return (RbacIdType)(object)DateTimeOffset.UtcNow.Ticks;
        if (idType == typeof(int)) return (RbacIdType)(object)Environment.TickCount;
        if (idType == typeof(Guid)) return (RbacIdType)(object)Guid.NewGuid();
        if (idType == typeof(string)) return (RbacIdType)(object)Guid.NewGuid().ToString();
        
        // 对于其他类型，返回默认值
        return CreateDefaultId();
    }

    /// <summary>
    /// 验证 ID 值是否有效
    /// </summary>
    /// <param name="id">要验证的 ID</param>
    /// <returns>是否有效</returns>
    public static bool IsValidId(RbacIdType? id)
    {
        if (id == null) return false;
        
        var idType = typeof(RbacIdType);
        
        if (idType == typeof(long)) return !id.Equals(0L);
        if (idType == typeof(int)) return !id.Equals(0);
        if (idType == typeof(Guid)) return !id.Equals(Guid.Empty);
        if (idType == typeof(string)) return !string.IsNullOrWhiteSpace(id.ToString());
        
        return !id.Equals(CreateDefaultId());
    }

    /// <summary>
    /// 将对象转换为 RbacIdType
    /// </summary>
    /// <param name="value">要转换的值</param>
    /// <returns>转换后的 ID 值</returns>
    public static RbacIdType? ConvertToId(object? value)
    {
        if (value == null) return default;
        
        try
        {
            if (value is RbacIdType directId)
                return directId;
                
            return (RbacIdType)Convert.ChangeType(value, typeof(RbacIdType));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 获取实体的所有 RBAC 相关属性
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>属性信息列表</returns>
    public static IEnumerable<PropertyInfo> GetRbacProperties(Type entityType)
    {
        return entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsRbacProperty(p));
    }

    /// <summary>
    /// 判断属性是否为 RBAC 相关属性
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>是否为 RBAC 属性</returns>
    private static bool IsRbacProperty(PropertyInfo property)
    {
        var rbacPropertyNames = new[]
        {
            nameof(IRbacEntity<RbacIdType>.Id),
            nameof(IRbacEntity<RbacIdType>.CreatedTime),
            nameof(IRbacEntity<RbacIdType>.UpdatedTime),
            nameof(IMultiTenantRbacEntity<RbacIdType>.TenantId),
            nameof(IUserRelatedRbacEntity<RbacIdType>.UserId),
            nameof(ISoftDeleteRbacEntity<RbacIdType>.IsDeleted),
            nameof(ISoftDeleteRbacEntity<RbacIdType>.DeletedTime),
            nameof(IAuditableRbacEntity<RbacIdType>.CreatedBy),
            nameof(IAuditableRbacEntity<RbacIdType>.UpdatedBy)
        };

        return rbacPropertyNames.Contains(property.Name);
    }

    /// <summary>
    /// 检查实体是否实现了指定的 RBAC 接口
    /// </summary>
    /// <typeparam name="TInterface">接口类型</typeparam>
    /// <param name="entityType">实体类型</param>
    /// <returns>是否实现了指定接口</returns>
    public static bool ImplementsInterface<TInterface>(Type entityType)
    {
        return typeof(TInterface).IsAssignableFrom(entityType);
    }

    /// <summary>
    /// 检查实体是否支持多租户
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>是否支持多租户</returns>
    public static bool IsMultiTenant(Type entityType)
    {
        return ImplementsInterface<IMultiTenantRbacEntity<RbacIdType>>(entityType);
    }

    /// <summary>
    /// 检查实体是否与用户关联
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>是否与用户关联</returns>
    public static bool IsUserRelated(Type entityType)
    {
        return ImplementsInterface<IUserRelatedRbacEntity<RbacIdType>>(entityType);
    }

    /// <summary>
    /// 检查实体是否支持软删除
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>是否支持软删除</returns>
    public static bool IsSoftDeleteEnabled(Type entityType)
    {
        return ImplementsInterface<ISoftDeleteRbacEntity<RbacIdType>>(entityType);
    }

    /// <summary>
    /// 检查实体是否支持审计
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>是否支持审计</returns>
    public static bool IsAuditable(Type entityType)
    {
        return ImplementsInterface<IAuditableRbacEntity<RbacIdType>>(entityType);
    }
}

/// <summary>
/// RBAC 实体扩展方法
/// 为实体实例提供便捷的操作方法
/// </summary>
public static class RbacEntityExtensions
{
    /// <summary>
    /// 设置创建信息
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体实例</param>
    /// <param name="createdBy">创建者ID</param>
    /// <returns>实体实例</returns>
    public static T SetCreatedInfo<T>(this T entity, RbacIdType? createdBy = null) 
        where T : IRbacEntity<RbacIdType>
    {
        entity.CreatedTime = DateTimeOffset.UtcNow;
        
        if (entity is IAuditableRbacEntity<RbacIdType> auditableEntity)
        {
            auditableEntity.CreatedBy = createdBy;
        }
        
        return entity;
    }

    /// <summary>
    /// 设置更新信息
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体实例</param>
    /// <param name="updatedBy">更新者ID</param>
    /// <returns>实体实例</returns>
    public static T SetUpdatedInfo<T>(this T entity, RbacIdType? updatedBy = null) 
        where T : IRbacEntity<RbacIdType>
    {
        entity.UpdatedTime = DateTimeOffset.UtcNow;
        
        if (entity is IAuditableRbacEntity<RbacIdType> auditableEntity)
        {
            auditableEntity.UpdatedBy = updatedBy;
        }
        
        return entity;
    }

    /// <summary>
    /// 设置软删除信息
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体实例</param>
    /// <returns>实体实例</returns>
    public static T SetDeleted<T>(this T entity) 
        where T : ISoftDeleteRbacEntity<RbacIdType>
    {
        entity.IsDeleted = true;
        entity.DeletedTime = DateTimeOffset.UtcNow;
        
        return entity;
    }

    /// <summary>
    /// 恢复软删除
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体实例</param>
    /// <returns>实体实例</returns>
    public static T SetUndeleted<T>(this T entity) 
        where T : ISoftDeleteRbacEntity<RbacIdType>
    {
        entity.IsDeleted = false;
        entity.DeletedTime = null;
        
        return entity;
    }

    /// <summary>
    /// 设置租户信息
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体实例</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>实体实例</returns>
    public static T SetTenant<T>(this T entity, RbacIdType? tenantId) 
        where T : IMultiTenantRbacEntity<RbacIdType>
    {
        entity.TenantId = tenantId;
        return entity;
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体实例</param>
    /// <param name="userId">用户ID</param>
    /// <returns>实体实例</returns>
    public static T SetUser<T>(this T entity, RbacIdType? userId) 
        where T : IUserRelatedRbacEntity<RbacIdType>
    {
        entity.UserId = userId;
        return entity;
    }
}

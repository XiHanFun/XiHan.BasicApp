#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacIdTypeAttribute
// Guid:ac28152c-d6e9-4396-addb-b479254bad96
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Entities.Base;

/// <summary>
/// RBAC ID 类型特性
/// 用于标记实体使用的 ID 类型
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class RbacIdTypeAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="idType">ID类型</param>
    public RbacIdTypeAttribute(Type idType)
    {
        IdType = idType ?? throw new ArgumentNullException(nameof(idType));

        // 验证类型是否支持作为主键
        if (!IsValidIdType(idType))
        {
            throw new ArgumentException($"类型 {idType.Name} 不支持作为主键类型", nameof(idType));
        }
    }

    /// <summary>
    /// ID 类型
    /// </summary>
    public Type IdType { get; }

    /// <summary>
    /// 验证是否为有效的ID类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>是否有效</returns>
    private static bool IsValidIdType(Type type)
    {
        // 支持的ID类型
        var supportedTypes = new[]
        {
            typeof(int), typeof(long), typeof(Guid), typeof(string),
            typeof(short), typeof(uint), typeof(ulong), typeof(ushort)
        };

        return supportedTypes.Contains(type) ||
               type.IsEnum ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                IsValidIdType(Nullable.GetUnderlyingType(type)!));
    }
}

/// <summary>
/// 常用 ID 类型的特性快捷方式
/// </summary>
public static class RbacIdTypes
{
    /// <summary>
    /// 长整型 ID
    /// </summary>
    public class LongId : RbacIdTypeAttribute
    {
        public LongId() : base(typeof(long))
        {
        }
    }

    /// <summary>
    /// 整型 ID
    /// </summary>
    public class IntId : RbacIdTypeAttribute
    {
        public IntId() : base(typeof(int))
        {
        }
    }

    /// <summary>
    /// GUID ID
    /// </summary>
    public class GuidId : RbacIdTypeAttribute
    {
        public GuidId() : base(typeof(Guid))
        {
        }
    }

    /// <summary>
    /// 字符串 ID
    /// </summary>
    public class StringId : RbacIdTypeAttribute
    {
        public StringId() : base(typeof(string))
        {
        }
    }
}

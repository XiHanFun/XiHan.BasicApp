#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppIdType
// Guid:8922764a-9cd8-45bd-97cf-b6923a4be145
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/10 6:39:05
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Core;

/// <summary>
/// 全局 Id 类型别名
/// </summary>
/// <param name="value"></param>
public readonly struct XiHanBasicAppIdType(long value)
    : IEquatable<XiHanBasicAppIdType>
{
    /// <summary>
    /// 值
    /// </summary>
    public long Value { get; } = value;

    /// <summary>
    /// 隐式转换为 long
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static implicit operator long(XiHanBasicAppIdType id) => id.Value;

    /// <summary>
    /// 隐式转换为 XiHanBasicAppIdType
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator XiHanBasicAppIdType(long value) => new(value);

    /// <summary>
    /// 相等运算符
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(XiHanBasicAppIdType left, XiHanBasicAppIdType right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// 不相等运算符
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(XiHanBasicAppIdType left, XiHanBasicAppIdType right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// 等于运算符
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(XiHanBasicAppIdType other) => Value == other.Value;

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Value.ToString();

    /// <summary>
    /// 等于运算符
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        return obj is XiHanBasicAppIdType type && Equals(type);
    }
}

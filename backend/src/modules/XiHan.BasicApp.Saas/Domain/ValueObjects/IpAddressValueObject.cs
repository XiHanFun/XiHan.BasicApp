#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IpAddressValueObject.cs
// Guid:e7a1c5d8-2f6b-4e0a-d345-b9a2f1e6c8d0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Net;

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// IP 地址值对象
/// </summary>
public readonly record struct IpAddressValueObject
{
    public string Value { get; }

    private IpAddressValueObject(string value)
    {
        Value = value;
    }

    /// <summary>
    /// 从字符串创建
    /// </summary>
    public static IpAddressValueObject? Create(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return null;

        return new IpAddressValueObject(ip.Trim());
    }

    /// <summary>
    /// 是否为私有网络地址
    /// </summary>
    public bool IsPrivateNetwork()
    {
        if (!IPAddress.TryParse(Value, out var address))
            return false;

        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4)
            return false;

        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168);
    }

    /// <summary>
    /// 是否为回环地址
    /// </summary>
    public bool IsLoopback()
    {
        return IPAddress.TryParse(Value, out var address) && IPAddress.IsLoopback(address);
    }

    /// <summary>
    /// 是否在同一 /16 子网
    /// </summary>
    public bool IsSameSubnet16(IpAddressValueObject other)
    {
        if (!IPAddress.TryParse(Value, out var a) || !IPAddress.TryParse(other.Value, out var b))
            return false;

        var ab = a.GetAddressBytes();
        var bb = b.GetAddressBytes();
        if (ab.Length != 4 || bb.Length != 4)
            return false;

        return ab[0] == bb[0] && ab[1] == bb[1];
    }

    /// <summary>
    /// 判断是否为风险登录（IP 变更检测）
    /// </summary>
    public static bool DetermineRiskLogin(IpAddressValueObject? previous, IpAddressValueObject? current)
    {
        if (previous is null || current is null)
            return false;

        if (current.Value.IsLoopback() || previous.Value.IsLoopback())
            return false;

        if (current.Value.IsPrivateNetwork() && previous.Value.IsPrivateNetwork())
            return false;

        return !current.Value.IsSameSubnet16(previous.Value);
    }

    public override string ToString() => Value;
}

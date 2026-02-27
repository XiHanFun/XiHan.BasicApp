#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PasswordValueObject
// Guid:23fc69ac-8d3f-46f8-bfc5-4e32a231e3f6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:36:33
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Authentication.Password;

namespace XiHan.BasicApp.Rbac.Domain.ValueObjects;

/// <summary>
/// 密码值对象（仅承载哈希值与校验语义）
/// </summary>
public readonly record struct PasswordValueObject
{
    /// <summary>
    /// 哈希后的密码字符串
    /// </summary>
    public string Hash { get; }

    private PasswordValueObject(string hash)
    {
        Hash = hash;
    }

    /// <summary>
    /// 由明文密码创建值对象
    /// </summary>
    public static PasswordValueObject Create(string plainPassword, IPasswordHasher passwordHasher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        return new PasswordValueObject(passwordHasher.HashPassword(plainPassword));
    }

    /// <summary>
    /// 由已有哈希创建值对象
    /// </summary>
    public static PasswordValueObject FromHash(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        return new PasswordValueObject(hash);
    }

    /// <summary>
    /// 校验明文密码
    /// </summary>
    public bool Verify(string plainPassword, IPasswordHasher passwordHasher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        return passwordHasher.VerifyPassword(Hash, plainPassword);
    }
}

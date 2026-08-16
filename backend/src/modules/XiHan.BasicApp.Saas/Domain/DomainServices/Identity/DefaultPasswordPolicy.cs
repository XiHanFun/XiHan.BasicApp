// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 默认密码策略：判定提交的密码是否命中内置或配置的种子默认密码。
/// </summary>
/// <remarks>
/// 种子数据使用配置值或内置默认值作为超管初始密码（见 <c>SaasIdentitySeeder</c>），
/// 使用默认密码登录意味着凭证已公开，必须强制修改后才能继续使用系统。
/// 比较采用恒定时间比较，防时序侧信道。
/// </remarks>
public static class DefaultPasswordPolicy
{
    /// <summary>
    /// 内置默认密码（与种子器的内置默认值保持一致）
    /// </summary>
    public const string BuiltInDefaultPassword = "SuperAdmin@123";

    /// <summary>
    /// 种子超管初始密码配置键（环境变量形式 Saas__Seed__SuperAdminPassword）
    /// </summary>
    public const string SeedPasswordConfigKey = "Saas:Seed:SuperAdminPassword";

    /// <summary>
    /// 判定密码是否命中默认密码（内置默认或配置的种子密码任一命中即视为默认密码）
    /// </summary>
    /// <param name="password">登录提交的明文密码</param>
    /// <param name="configuredSeedPassword">配置的种子初始密码（可为空）</param>
    /// <returns>true 表示使用了默认密码</returns>
    public static bool IsDefaultPassword(string? password, string? configuredSeedPassword)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        if (FixedTimeEquals(password, BuiltInDefaultPassword))
        {
            return true;
        }

        return !string.IsNullOrEmpty(configuredSeedPassword)
               && FixedTimeEquals(password, configuredSeedPassword);
    }

    /// <summary>
    /// 恒定时间字符串比较（先比长度再恒定时间比内容）
    /// </summary>
    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
               && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

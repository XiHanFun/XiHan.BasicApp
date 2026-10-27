// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 表名与列名的 snake_case 约定：<c>SysUser</c> → <c>sys_user</c>、<c>HomeTenantId</c> → <c>home_tenant_id</c>、<c>IPAddress</c> → <c>ip_address</c>。
/// 实体或属性显式标了 SqlSugar 名字特性时不改写。
/// </summary>
public static class SnakeCaseNaming
{
    /// <summary>
    /// PascalCase / camelCase 转 snake_case。
    /// </summary>
    public static string Of(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var builder = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            if (char.IsUpper(current))
            {
                // 大写前面是小写或数字时断词（userId → user_id）；连续大写段的最后一个大写后面跟小写时也断词（IPAddress → ip_address）
                var previousBreaks = i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]));
                var nextBreaks = i > 0 && char.IsUpper(name[i - 1]) && i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (previousBreaks || nextBreaks)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}

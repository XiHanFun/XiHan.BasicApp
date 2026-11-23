// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;

namespace XiHan.BasicApp.Plugins.Domain.Packages;

/// <summary>
/// 插件版本号的先后
/// </summary>
/// <remarks>
/// 按 SemVer 2.0 的优先级比较：主、次、修订号按数值，有预发布标识的排在没有的之前，预发布标识逐段比较（纯数字按数值且排在字母数字之前，其余按 ASCII），段数少的在前；构建元数据不参与
/// 版本号已经由框架清单按 SemVer 文法核对过
/// </remarks>
public static class PluginVersions
{
    /// <summary>
    /// 比较两个版本号
    /// </summary>
    /// <param name="left">版本号</param>
    /// <param name="right">版本号</param>
    /// <returns>小于 0 即 <paramref name="left"/> 在前，0 即优先级相同</returns>
    public static int Compare(string left, string right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var (leftCore, leftPre) = Split(left);
        var (rightCore, rightPre) = Split(right);
        var core = leftCore.Zip(rightCore, static (l, r) => l.CompareTo(r)).FirstOrDefault(static c => c != 0);
        if (core != 0)
        {
            return core;
        }

        if (leftPre.Length == 0 || rightPre.Length == 0)
        {
            // 有预发布标识的在前，都没有即相同
            return (leftPre.Length == 0, rightPre.Length == 0) switch
            {
                (true, true) => 0,
                (true, false) => 1,
                _ => -1,
            };
        }

        for (var i = 0; i < Math.Min(leftPre.Length, rightPre.Length); i++)
        {
            var compared = CompareIdentifier(leftPre[i], rightPre[i]);
            if (compared != 0)
            {
                return compared;
            }
        }

        return leftPre.Length.CompareTo(rightPre.Length);
    }

    /// <summary>
    /// 拆出三段数字与预发布标识，去掉构建元数据
    /// </summary>
    private static (ulong[] Core, string[] PreRelease) Split(string version)
    {
        var withoutBuild = version.Split('+', 2)[0];
        var parts = withoutBuild.Split('-', 2);
        var core = parts[0].Split('.').Select(static n => ulong.Parse(n, NumberStyles.None, CultureInfo.InvariantCulture)).ToArray();
        return (core, parts.Length > 1 ? parts[1].Split('.') : []);
    }

    private static int CompareIdentifier(string left, string right)
    {
        var leftNumeric = ulong.TryParse(left, NumberStyles.None, CultureInfo.InvariantCulture, out var l);
        var rightNumeric = ulong.TryParse(right, NumberStyles.None, CultureInfo.InvariantCulture, out var r);
        return (leftNumeric, rightNumeric) switch
        {
            (true, true) => l.CompareTo(r),
            (true, false) => -1,
            (false, true) => 1,
            _ => string.CompareOrdinal(left, right),
        };
    }
}

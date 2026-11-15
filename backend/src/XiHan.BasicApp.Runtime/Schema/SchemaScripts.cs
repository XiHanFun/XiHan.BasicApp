// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一个版本目录里某种库的升级脚本
/// </summary>
/// <param name="Version">版本</param>
/// <param name="Name">目录名（台账里记的版本写法）</param>
/// <param name="Files">平台库的脚本完整路径，按文件名顺序</param>
/// <param name="TenantFiles">租户独立库的脚本完整路径，按文件名顺序</param>
public sealed record SchemaVersionScripts(Version Version, string Name, IReadOnlyList<string> Files, IReadOnlyList<string> TenantFiles);

/// <summary>
/// 找出脚本目录里的版本与某种库的脚本
/// </summary>
/// <remarks>
/// 表结构由 ORM 按实体建出与补齐，脚本只做 ORM 做不了的事（改名、改类型、删列、数据迁移）。每个子目录是一个版本（<c>主.次.修订</c>），
/// 其下按库类型分目录（<see cref="DatabaseProviders.Production"/> 与测试用的 <c>sqlite</c>），租户独立库的脚本在库类型目录下的 <c>tenant</c> 里
/// 目录名不是版本号、库类型认不出、脚本没放进库类型目录都报错，不跳过；没有脚本目录即没有脚本
/// </remarks>
public static class SchemaScripts
{
    /// <summary>
    /// 租户独立库脚本所在的子目录名
    /// </summary>
    public const string TenantDirectory = "tenant";

    /// <summary>
    /// 最早的结构版本
    /// </summary>
    /// <remarks>
    /// 新库建好即登记为当前结构版本：它与脚本目录里最高的版本取大者
    /// </remarks>
    public static Version Baseline { get; } = new(6, 0, 0);

    /// <summary>
    /// 列出脚本目录里的全部版本，带某种库的脚本
    /// </summary>
    /// <param name="directory">脚本目录</param>
    /// <param name="provider">库类型名（<see cref="DatabaseProviders.NameOf"/>）</param>
    public static Result<IReadOnlyList<SchemaVersionScripts>> Discover(string directory, string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        if (!Directory.Exists(directory))
        {
            return Array.Empty<SchemaVersionScripts>();
        }

        var versions = new List<SchemaVersionScripts>();
        foreach (var path in Directory.EnumerateDirectories(directory))
        {
            var name = Path.GetFileName(path);
            if (!System.Version.TryParse(name, out var version) || version.Build < 0 || version.Revision >= 0)
            {
                return Error.Literal(RuntimeErrors.SchemaScriptsInvalid, $"Schema script directory '{name}' is not a version (major.minor.patch).");
            }

            if (Directory.EnumerateFiles(path, "*.sql").FirstOrDefault() is { } loose)
            {
                return Error.Literal(RuntimeErrors.SchemaScriptsInvalid, $"Schema script '{name}/{Path.GetFileName(loose)}' must be placed in a database type directory ({string.Join(", ", DatabaseProviders.Production)}).");
            }

            foreach (var typed in Directory.EnumerateDirectories(path))
            {
                var type = Path.GetFileName(typed);
                if (!DatabaseProviders.Production.Contains(type, StringComparer.Ordinal) && type != DatabaseProviders.Sqlite)
                {
                    return Error.Literal(RuntimeErrors.SchemaScriptsInvalid, $"Schema script directory '{name}/{type}' is not a database type ({string.Join(", ", DatabaseProviders.Production)}, {DatabaseProviders.Sqlite}).");
                }
            }

            var own = Path.Combine(path, provider);
            versions.Add(new SchemaVersionScripts(version, name, ScriptsIn(own), ScriptsIn(Path.Combine(own, TenantDirectory))));
        }

        return versions.OrderBy(static v => v.Version).ToList();
    }

    /// <summary>
    /// 当前结构版本：脚本目录里最高的版本与 <see cref="Baseline"/> 取大者
    /// </summary>
    public static Version Current(IReadOnlyList<SchemaVersionScripts> versions)
    {
        ArgumentNullException.ThrowIfNull(versions);
        return versions.Count > 0 && versions[^1].Version > Baseline ? versions[^1].Version : Baseline;
    }

    /// <summary>
    /// 版本号在台账里的写法（<c>主.次.修订</c>）
    /// </summary>
    public static string NameOf(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return version.ToString(3);
    }

    private static string[] ScriptsIn(string folder)
        => Directory.Exists(folder) ? [.. Directory.EnumerateFiles(folder, "*.sql").Order(StringComparer.Ordinal)] : [];
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一个版本目录里的升级脚本。
/// </summary>
/// <param name="Version">版本。</param>
/// <param name="Name">目录名（台账里记的版本写法）。</param>
/// <param name="Files">脚本文件的完整路径，按文件名顺序。</param>
public sealed record SchemaVersionScripts(Version Version, string Name, IReadOnlyList<string> Files);

/// <summary>
/// 找出脚本目录里的版本与脚本。
/// </summary>
/// <remarks>
/// 每个子目录是一个版本（<c>主.次.修订</c>），按版本排序；最低的那个是基线（空库从它建起）。目录名不是版本号即报错，不跳过。
/// </remarks>
public static class SchemaScripts
{
    /// <summary>
    /// 列出脚本目录里的全部版本。
    /// </summary>
    public static Result<IReadOnlyList<SchemaVersionScripts>> Discover(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!Directory.Exists(directory))
        {
            return Error.Literal(RuntimeErrors.SchemaScriptsInvalid, $"Schema script directory '{directory}' does not exist.");
        }

        var versions = new List<SchemaVersionScripts>();
        foreach (var path in Directory.EnumerateDirectories(directory))
        {
            var name = Path.GetFileName(path);
            if (!System.Version.TryParse(name, out var version) || version.Build < 0 || version.Revision >= 0)
            {
                return Error.Literal(RuntimeErrors.SchemaScriptsInvalid, $"Schema script directory '{name}' is not a version (major.minor.patch).");
            }

            var files = Directory.EnumerateFiles(path, "*.sql").Order(StringComparer.Ordinal).ToList();
            versions.Add(new SchemaVersionScripts(version, name, files));
        }

        if (versions.Count == 0)
        {
            return Error.Literal(RuntimeErrors.SchemaScriptsInvalid, $"Schema script directory '{directory}' has no version directories; the baseline is missing.");
        }

        return versions.OrderBy(static v => v.Version).ToList();
    }
}

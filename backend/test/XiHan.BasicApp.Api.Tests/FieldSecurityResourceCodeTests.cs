// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 字段安全的资源编码必须是权限资源编码（如 user、role、code_gen），不能是实体名。
/// </summary>
/// <remarks>
/// 回归锚点：各查询服务曾以 "SysUser" 这类实体名解析字段规则，而管理端建规则时只能选权限目录里的资源
/// （编码是 user），两边永远对不上，规则建了也不生效。这里扫描各模块源码，字段安全调用的资源参数
/// 一律引用权限码常量（SaasPermissionCodes.X.Group、XxxPermissionCodes.Resource）。
/// </remarks>
public sealed partial class FieldSecurityResourceCodeTests
{
    /// <summary>
    /// 没有权限资源、因而也挂不上字段规则的例外：文件名 → 原因。
    /// </summary>
    private static readonly Dictionary<string, string> Exemptions = new(StringComparer.Ordinal)
    {
        ["ExportTaskQueryService.cs"] = "只查当前用户自己的导出任务，没有对应的权限资源",
    };

    /// <summary>
    /// 字段安全调用的资源参数不许写实体名字符串或 nameof(实体)。
    /// </summary>
    [Fact]
    public void FieldSecurityCalls_ShouldUsePermissionResourceCodes()
    {
        var violations = Directory
            .EnumerateFiles(ModulesDirectory(), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path) && !Exemptions.ContainsKey(Path.GetFileName(path)))
            .SelectMany(path => EntityNamedCall().Matches(File.ReadAllText(path))
                .Select(match => $"{Path.GetFileName(path)}：{match.Value}"))
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"以下字段安全调用仍按实体名解析资源，应改为权限码常量：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 例外表里的文件必须真实存在，删改后及时清理，免得白名单悄悄放过新代码。
    /// </summary>
    [Fact]
    public void Exemptions_ShouldPointToExistingFiles()
    {
        var files = Directory
            .EnumerateFiles(ModulesDirectory(), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(Exemptions.Keys, name => Assert.Contains(name, files));
    }

    [GeneratedRegex(@"_fieldSecurity\w*\.\w+Async\((?:[^,()]+,\s*)?(?:""Sys[A-Za-z]+""|nameof\(Sys[A-Za-z]+\))")]
    private static partial Regex EntityNamedCall();

    private static bool IsBuildOutput(string path)
    {
        var separator = Path.DirectorySeparatorChar;
        return path.Contains($"{separator}bin{separator}", StringComparison.Ordinal)
            || path.Contains($"{separator}obj{separator}", StringComparison.Ordinal);
    }

    private static string ModulesDirectory([CallerFilePath] string testFilePath = "")
    {
        var testDirectory = Path.GetDirectoryName(testFilePath)
            ?? throw new InvalidOperationException("无法解析测试源文件目录。");

        return Path.GetFullPath(Path.Combine(testDirectory, "..", "..", "src", "modules"));
    }
}

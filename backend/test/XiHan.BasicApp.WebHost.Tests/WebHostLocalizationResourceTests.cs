// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;

namespace XiHan.BasicApp.WebHost.Tests;

/// <summary>
/// 宿主本地化资源完整性测试。
/// </summary>
/// <remarks>
/// 本地化缺键不会报错，只会让该语言静默回落到键名或另一种语言——用户看到的是一串英文标识符。
/// 这类劣化没有任何运行期信号，只能靠对账测试兜住，所以这里逐资源比对两种文化的键集合。
/// </remarks>
public sealed class WebHostLocalizationResourceTests
{
    /// <summary>
    /// 每个资源都必须同时提供中英两种文化的文件，不允许只加中文不加英文。
    /// </summary>
    [Fact]
    public void Localization_EveryResourceShouldShipBothCultures()
    {
        var files = ReadResourceFiles();
        Assert.NotEmpty(files);

        var missing = files
            .GroupBy(file => file.Resource, StringComparer.Ordinal)
            .Where(group => group.Select(file => file.Culture).Distinct(StringComparer.Ordinal).Count() < 2)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0, $"这些资源缺少某一种文化的文件：{string.Join("、", missing)}");
    }

    /// <summary>
    /// 同一资源在两种文化下的键集合必须完全一致，任何一侧缺键都会造成静默回落。
    /// </summary>
    [Fact]
    public void Localization_TextKeysShouldMatchAcrossCultures()
    {
        foreach (var group in ReadResourceFiles().GroupBy(file => file.Resource, StringComparer.Ordinal))
        {
            var chinese = group.Single(file => string.Equals(file.Culture, "zh-CN", StringComparison.Ordinal));
            var english = group.Single(file => string.Equals(file.Culture, "en-US", StringComparison.Ordinal));

            var missingInEnglish = chinese.Keys.Except(english.Keys, StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToList();
            var missingInChinese = english.Keys.Except(chinese.Keys, StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToList();

            Assert.True(
                missingInEnglish.Count == 0,
                $"资源 {group.Key} 的 en-US 缺少这些键：{string.Join("、", missingInEnglish)}");
            Assert.True(
                missingInChinese.Count == 0,
                $"资源 {group.Key} 的 zh-CN 缺少这些键：{string.Join("、", missingInChinese)}");
        }
    }

    /// <summary>
    /// 文件内的 resource 与 culture 字段必须与文件名的两段一致，对不上会让框架按错误的资源/文化装载。
    /// </summary>
    [Fact]
    public void Localization_ResourceAndCultureFieldsShouldMatchFileName()
    {
        foreach (var file in ReadResourceFiles())
        {
            Assert.Equal(file.Resource, file.DeclaredResource, StringComparer.Ordinal);
            Assert.Equal(file.Culture, file.DeclaredCulture, StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// 所有文案值都不得为空或纯空白：空值等于把空字符串当文案返回给前端。
    /// </summary>
    [Fact]
    public void Localization_TextValuesShouldNotBeBlank()
    {
        foreach (var file in ReadResourceFiles())
        {
            var blank = file.BlankKeys.Order(StringComparer.Ordinal).ToList();

            Assert.True(
                blank.Count == 0,
                $"{file.Resource}.{file.Culture} 的这些键取值为空白：{string.Join("、", blank)}");
        }
    }

    /// <summary>
    /// 读取 Localization 目录下的全部资源文件。
    /// </summary>
    /// <returns>解析后的资源文件描述集合。</returns>
    private static List<LocalizationResourceFile> ReadResourceFiles()
    {
        var directory = Path.Combine(WebHostTestHelper.ResolveWebHostProjectRoot(), "Localization");
        Assert.True(Directory.Exists(directory), $"本地化资源目录不存在：{directory}");

        return [.. Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(LocalizationResourceFile.Load)
            .OrderBy(file => file.Resource, StringComparer.Ordinal)
            .ThenBy(file => file.Culture, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 单个本地化资源文件的解析结果。
    /// </summary>
    private sealed class LocalizationResourceFile
    {
        /// <summary>
        /// 从文件名解析出的资源名。
        /// </summary>
        public required string Resource { get; init; }

        /// <summary>
        /// 从文件名解析出的文化名。
        /// </summary>
        public required string Culture { get; init; }

        /// <summary>
        /// 文件内声明的资源名。
        /// </summary>
        public required string? DeclaredResource { get; init; }

        /// <summary>
        /// 文件内声明的文化名。
        /// </summary>
        public required string? DeclaredCulture { get; init; }

        /// <summary>
        /// 全部文案键。
        /// </summary>
        public required HashSet<string> Keys { get; init; }

        /// <summary>
        /// 取值为空或纯空白的键。
        /// </summary>
        public required List<string> BlankKeys { get; init; }

        /// <summary>
        /// 解析一个资源文件。
        /// </summary>
        /// <param name="path">资源文件绝对路径。</param>
        /// <returns>解析结果。</returns>
        public static LocalizationResourceFile Load(string path)
        {
            // 文件名形如 <资源>.<文化>.json
            var fileName = Path.GetFileNameWithoutExtension(path);
            var separator = fileName.LastIndexOf('.');
            Assert.True(separator > 0, $"本地化文件名不符合 <资源>.<文化>.json 约定：{path}");

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            var keys = new HashSet<string>(StringComparer.Ordinal);
            var blankKeys = new List<string>();
            if (root.TryGetProperty("texts", out var texts) && texts.ValueKind == JsonValueKind.Object)
            {
                foreach (var text in texts.EnumerateObject())
                {
                    _ = keys.Add(text.Name);
                    if (text.Value.ValueKind != JsonValueKind.String
                        || string.IsNullOrWhiteSpace(text.Value.GetString()))
                    {
                        blankKeys.Add(text.Name);
                    }
                }
            }

            return new LocalizationResourceFile
            {
                Resource = fileName[..separator],
                Culture = fileName[(separator + 1)..],
                DeclaredResource = ReadString(root, "resource"),
                DeclaredCulture = ReadString(root, "culture"),
                Keys = keys,
                BlankKeys = blankKeys
            };
        }

        /// <summary>
        /// 读取根节点上的字符串属性。
        /// </summary>
        /// <param name="root">根节点。</param>
        /// <param name="name">属性名。</param>
        /// <returns>字符串值；缺失或非字符串时返回 null。</returns>
        private static string? ReadString(JsonElement root, string name)
        {
            return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
    }
}

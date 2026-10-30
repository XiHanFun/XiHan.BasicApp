// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Text.Json;
using XiHan.BasicApp.Architecture.Tests.Model;
using XiHan.BasicApp.Contracts;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions.Locales;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// 检查包的目录名、区域文件、错误码与清单。
/// </summary>
/// <remarks>
/// 方案 5.3：目录名在白名单内；10 个区域文件键集一致且与消息键常量一一对应；错误码带包前缀；清单 ID、包名与版本按附录 B。
/// </remarks>
public sealed class PackageStructureTests
{
    [Fact]
    public void CapabilityPackages_OnlyUseWhitelistedDirectories()
    {
        foreach (var (package, path) in Packages.Projects)
        {
            if (Packages.StructureExempt.Contains(package))
            {
                continue;
            }

            var directories = Directory.EnumerateDirectories(Path.GetDirectoryName(path)!)
                .Select(static d => Path.GetFileName(d))
                .Where(static d => d is not ("bin" or "obj"))
                .ToList();
            var unexpected = directories.Where(d => !Packages.CapabilityDirectories.Contains(d)).ToList();
            Assert.True(unexpected.Count == 0, $"{package}: directories not in the whitelist: {string.Join(", ", unexpected)}");
        }
    }

    [Fact]
    public void LocalizationFiles_CoverAllTenLocales_WithIdenticalKeys_MatchingMessageConstants()
    {
        foreach (var (package, path) in Packages.Projects)
        {
            var localization = Path.Combine(Path.GetDirectoryName(path)!, "Localization");
            var messagesType = Packages.Assemblies
                .Select(a => a.GetType($"{Packages.Prefix}{package}.{package}Messages"))
                .FirstOrDefault(static t => t is not null);
            if (!Directory.Exists(localization))
            {
                Assert.True(messagesType is null, $"{package}: {package}Messages exists but Localization/ is missing");
                continue;
            }

            Assert.NotNull(messagesType);
            var files = Directory.EnumerateFiles(localization, "*.json").Select(static f => Path.GetFileNameWithoutExtension(f)).Order(StringComparer.Ordinal).ToList();
            Assert.Equal(SupportedLocales.All.Order(StringComparer.Ordinal), files);
            var messageConstants = messagesType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(static f => f.IsLiteral && f.FieldType == typeof(string) && !string.Equals(f.Name, "CatalogName", StringComparison.Ordinal))
                .Select(static f => (string)f.GetRawConstantValue()!)
                .ToList();

            // 消息键常量之外，生成器按约定读的键（ADR-0052）：每个操作的 .title、本包的分组，以及本包登记的资源标签（每个资源恰好一个包登记，见下一个测试）
            var operations = OperationContractTests.Operations().Where(o => string.Equals(o.Package, package, StringComparison.Ordinal)).Select(static o => o.Attribute.Id).ToList();
            var group = package.ToLowerInvariant();
            var conventional = operations.Select(static id => KeyOf(id) + ".title")
                .Concat(operations.Count == 0 ? [] : [$"openapi.groups.{group}.name", $"openapi.groups.{group}.description"]);
            var resources = operations.Select(static id => KeyOf(id[..id.LastIndexOf('.')])).ToHashSet(StringComparer.Ordinal);
            foreach (var locale in files)
            {
                using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(localization, locale + ".json")));
                var keys = document.RootElement.EnumerateObject().Select(static p => p.Name).Order(StringComparer.Ordinal).ToList();
                var tags = keys.Where(static k => k.StartsWith(TagPrefix, StringComparison.Ordinal)).ToList();
                foreach (var tag in tags)
                {
                    var parts = tag[TagPrefix.Length..].Split('.');
                    Assert.True(parts.Length == 2 && resources.Contains(parts[0]) && parts[1] is "name" or "description", $"{package}/{locale}: '{tag}' is not a tag of this package's resources");
                }

                var constants = messageConstants.Concat(conventional).Concat(tags).Order(StringComparer.Ordinal).ToList();
                Assert.True(constants.SequenceEqual(keys), $"{package}/Localization/{locale}.json keys differ from {package}Messages constants and the OpenAPI keys.{Environment.NewLine}expected: {string.Join(", ", constants)}{Environment.NewLine}keys: {string.Join(", ", keys)}");
                foreach (var key in keys)
                {
                    Assert.True(MessageTemplate.IsValidKey(key), $"{package}/{locale}: '{key}' is not a valid message key");
                }
            }
        }
    }

    [Fact]
    public void EveryOperationResource_HasItsTagRegisteredByExactlyOnePackage()
    {
        // 资源标签的显示名与说明（openapi.tags.<资源>.name / .description）：跨包的资源（如 auth）只由一个包登记
        var owners = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var (package, path) in Packages.Projects)
        {
            var file = Path.Combine(Path.GetDirectoryName(path)!, "Localization", "zh-CN.json");
            if (!File.Exists(file))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllBytes(file));
            foreach (var key in document.RootElement.EnumerateObject().Select(static p => p.Name).Where(static k => k.StartsWith(TagPrefix, StringComparison.Ordinal) && k.EndsWith(".name", StringComparison.Ordinal)))
            {
                var resource = key[TagPrefix.Length..^".name".Length];
                (owners.TryGetValue(resource, out var list) ? list : owners[resource] = []).Add(package);
                Assert.True(document.RootElement.TryGetProperty(TagPrefix + resource + ".description", out _), $"{package}: tag '{resource}' has a name but no description");
            }
        }

        foreach (var resource in OperationContractTests.Operations().Select(static o => KeyOf(o.Attribute.Id[..o.Attribute.Id.LastIndexOf('.')])).Distinct(StringComparer.Ordinal))
        {
            Assert.True(owners.TryGetValue(resource, out var list) && list.Count == 1, $"resource '{resource}': tag registered by {(list is null ? "no package" : string.Join(", ", list))}, expected exactly one");
        }
    }

    [Fact]
    public void ErrorCodes_CarryThePackagePrefix_AndAValidKind()
    {
        foreach (var assembly in Packages.Assemblies)
        {
            var package = Packages.ShortName(assembly.GetName().Name!);
            var errorsType = assembly.GetType($"{Packages.Prefix}{package}.{package}Errors");
            if (errorsType is null)
            {
                continue;
            }

            foreach (var field in errorsType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(static f => f.IsLiteral && f.FieldType == typeof(string)))
            {
                var code = (string)field.GetRawConstantValue()!;
                Assert.Equal(code, ErrorCode.Validate(code));
                Assert.StartsWith(package.ToUpperInvariant() + "_", code, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Features_FollowManifestNaming_AndDeclareTheirPermissions()
    {
        var features = Packages.Assemblies
            .SelectMany(static a => a.GetTypes())
            .Where(static t => typeof(IFeature).IsAssignableFrom(t) && t is { IsAbstract: false, IsClass: true })
            .ToList();
        Assert.NotEmpty(features);
        foreach (var type in features)
        {
            var package = Packages.ShortName(type.Assembly.GetName().Name!);
            var feature = (IFeature)Activator.CreateInstance(type)!;
            Assert.Equal(AppInfo.ManifestIdPrefix + package.ToLowerInvariant(), feature.Manifest.Id);
            Assert.Equal(type.Assembly.GetName().Name, feature.Manifest.Package);
            Assert.Equal(AppInfo.Version, feature.Manifest.Version);
            Assert.Equal(type.FullName, feature.Manifest.Entry);
            var declared = PermissionCatalog.Of(package).Select(static p => p.Code).Order(StringComparer.Ordinal);
            Assert.Equal(declared, feature.Manifest.Contributes.Permissions.Order(StringComparer.Ordinal));
        }
    }

    private const string TagPrefix = "openapi.tags.";

    /// <summary>
    /// 操作 ID 或资源段对应的消息键前缀。
    /// </summary>
    /// <remarks>
    /// 资源段与动词段里的连字符换成下划线（<c>my-login-logs.list</c> → <c>my_login_logs.list</c>）。
    /// </remarks>
    private static string KeyOf(string id) => id.Replace('-', '_');
}

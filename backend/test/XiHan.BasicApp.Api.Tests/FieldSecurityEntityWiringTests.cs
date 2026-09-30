// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.AI.Extensions;
using XiHan.BasicApp.CodeGeneration.Extensions;
using XiHan.BasicApp.Printing.Extensions;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Extensions;
using XiHan.BasicApp.Workflow.Extensions;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 字段安全的登记与落地必须一一对应：登记了就要落地，落地了就要登记。
/// </summary>
/// <remarks>
/// 规则只能配给登记过的实体；实体登记了，查询服务却不按它门控，响应里认不出它的 DTO，或者应用服务新建、修改时不做写校验，
/// 配出的规则就会悄悄不生效。这里从各模块的登记方法取登记表，再核对 DTO 目录与源码里的落地点。
/// </remarks>
public sealed partial class FieldSecurityEntityWiringTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, Type>> RegisteredEntities = new(() =>
    {
        var services = new ServiceCollection()
            .AddSaasFieldSecurityEntities()
            .AddAIFieldSecurityEntities()
            .AddCodeGenerationFieldSecurityEntities()
            .AddPrintingFieldSecurityEntities()
            .AddWorkflowFieldSecurityEntities();
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<FieldSecurityEntityOptions>>().Value.Entities;
    });

    private static readonly Lazy<IReadOnlyList<(string File, string Text)>> Sources = new(() =>
        [
            .. Directory
                .EnumerateFiles(ModulesDirectory(), "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsBuildOutput(path))
                .Select(path => (Path.GetFileName(path), File.ReadAllText(path)))
        ]);

    /// <summary>
    /// 登记的实体都能描述出表说明与可配置字段。
    /// </summary>
    [Fact]
    public void RegisteredEntities_ShouldHaveDescriptionsAndFields()
    {
        var catalog = new FieldSecurityEntityCatalog(Options.Create(BuildOptions()));

        Assert.NotEmpty(catalog.GetEntities());
        Assert.All(catalog.GetEntities(), entity =>
        {
            Assert.NotEqual(entity.EntityName, entity.DisplayName);
            Assert.NotEmpty(entity.Fields);
        });
    }

    /// <summary>
    /// 字段安全调用里出现的实体都已登记（未登记的实体在运行时会直接报错）。
    /// </summary>
    [Fact]
    public void FieldSecurityCalls_ShouldOnlyUseRegisteredEntities()
    {
        var unregistered = Sources.Value
            .SelectMany(source => FieldSecurityCall().Matches(source.Text)
                .Select(match => (source.File, Entity: match.Groups["entity"].Value)))
            .Where(call => !RegisteredEntities.Value.ContainsKey(call.Entity))
            .Select(call => $"{call.File}：{call.Entity}")
            .Distinct()
            .ToList();

        Assert.True(unregistered.Count == 0, $"以下实体未登记字段安全：{Environment.NewLine}{string.Join(Environment.NewLine, unregistered)}");
    }

    /// <summary>
    /// 登记的实体都有查询门控。
    /// </summary>
    [Fact]
    public void RegisteredEntities_ShouldBeGuarded()
    {
        var guarded = Sources.Value
            .SelectMany(source => FieldSecurityCall().Matches(source.Text))
            .Where(match => match.Groups["method"].Value == "GuardQueryAsync")
            .Select(match => match.Groups["entity"].Value)
            .ToHashSet(StringComparer.Ordinal);

        var missing = RegisteredEntities.Value.Keys.Where(entity => !guarded.Contains(entity)).ToList();

        Assert.True(missing.Count == 0, $"以下实体缺查询门控 GuardQueryAsync：{string.Join("、", missing)}");
    }

    /// <summary>
    /// 响应打码靠 DTO 目录认出实体：每个登记实体都至少有一个经映射器生成的 DTO，且没有 DTO 同时属于两个实体（否则构建报错）。
    /// </summary>
    [Fact]
    public void RegisteredEntities_ShouldHaveRecognizableDtos()
    {
        var mappings = new FieldSecurityDtoCatalog(Options.Create(BuildOptions())).Mappings;

        var withoutDto = RegisteredEntities.Value.Values.Where(entity => !mappings.Values.Contains(entity)).Select(entity => entity.Name).ToList();

        Assert.True(withoutDto.Count == 0, $"以下实体没有经映射器生成的 DTO，响应里认不出它们：{string.Join("、", withoutDto)}");
    }

    /// <summary>
    /// 映射器把实体字段改名映射进 DTO 时（如 UserSessionId → SessionId），DTO 属性必须声明来源，否则按名字打不到码。
    /// </summary>
    [Fact]
    public void RenamedDtoProperties_ShouldDeclareFieldSecuritySource()
    {
        var catalog = new FieldSecurityEntityCatalog(Options.Create(BuildOptions()));
        var dtoTypes = RegisteredEntities.Value.Values
            .Select(entity => entity.Assembly)
            .Concat(AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.GetName().Name?.StartsWith("XiHan.BasicApp", StringComparison.Ordinal) == true))
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Name.EndsWith("Dto", StringComparison.Ordinal))
            .GroupBy(type => type.Name)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var missing = new List<string>();
        foreach (var (_, text) in Sources.Value.Where(source => source.File.EndsWith("Mapper.cs", StringComparison.Ordinal)))
        {
            foreach (Match method in MapperMethod().Matches(text))
            {
                var entity = catalog.Find(method.Groups["entity"].Value);
                if (entity is null
                    || !dtoTypes.TryGetValue(method.Groups["dto"].Value, out var dtoType)
                    || typeof(IFieldSecurityMultiSourceDto).IsAssignableFrom(dtoType))
                {
                    continue;
                }

                // 只认「属性 = 实体参数.字段」整句直取，三元、比较、调用等派生值不算改名
                var body = MethodBody(text, method.Index);
                var pattern = $@"\b(?<property>\w+) = {method.Groups["parameter"].Value}\.(?<field>\w+)\s*[,;\r\n]";
                foreach (Match assignment in Regex.Matches(body, pattern))
                {
                    var property = assignment.Groups["property"].Value;
                    var field = assignment.Groups["field"].Value;
                    if (property == field || entity.FindField(field) is null)
                    {
                        continue;
                    }

                    var declared = dtoType.GetProperty(property)?.GetCustomAttribute<FieldSecuritySourceAttribute>(inherit: true)?.EntityField;
                    if (declared != field)
                    {
                        missing.Add($"{dtoType.Name}.{property} ← {entity.EntityName}.{field}：缺 [FieldSecuritySource(nameof({entity.EntityName}.{field}))]");
                    }
                }
            }
        }

        Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing.Distinct()));
    }

    /// <summary>
    /// 响应过滤器必须排在框架动作过滤器（缓存、工作单元，默认顺序 0）外层，否则打码结果会被缓存给所有人。
    /// </summary>
    [Fact]
    public void ResponseFilter_ShouldRunOutsideCacheFilter()
    {
        Assert.True(FieldSecurityResponseFilter.FilterOrder < 0);
    }

    /// <summary>
    /// 入参为「实体名 + Create/Update/StatusUpdate + Dto」的应用服务方法，若实体已登记，方法里必须做对应的写校验。
    /// </summary>
    [Fact]
    public void WriteEndpointsOfRegisteredEntities_ShouldEnforceFieldSecurity()
    {
        var missing = new List<string>();
        foreach (var (file, text) in Sources.Value.Where(source => source.File.EndsWith("AppService.cs", StringComparison.Ordinal)))
        {
            foreach (Match method in WriteMethod().Matches(text))
            {
                var entity = $"Sys{method.Groups["prefix"].Value}";
                if (!RegisteredEntities.Value.ContainsKey(entity))
                {
                    continue;
                }

                var expected = method.Groups["kind"].Value == "Create" ? "EnsureCreatableAsync" : "EnsureUpdatableAsync";
                var body = MethodBody(text, method.Index);
                if (!body.Contains($"_fieldSecurity.{expected}(typeof({entity})", StringComparison.Ordinal))
                {
                    missing.Add($"{file}：{method.Groups["name"].Value} 缺 {expected}(typeof({entity}), …)");
                }
            }
        }

        Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
    }

    private static FieldSecurityEntityOptions BuildOptions()
    {
        var options = new FieldSecurityEntityOptions();
        var add = typeof(FieldSecurityEntityOptions).GetMethod(nameof(FieldSecurityEntityOptions.Add))!;
        foreach (var type in RegisteredEntities.Value.Values)
        {
            _ = add.MakeGenericMethod(type).Invoke(options, null);
        }

        return options;
    }

    /// <summary>
    /// 方法体：从签名到下一个成员的文档注释或声明
    /// </summary>
    private static string MethodBody(string text, int start)
    {
        var next = NextMember().Match(text, start + 1);
        return next.Success ? text[start..next.Index] : text[start..];
    }

    [GeneratedRegex(@"_fieldSecurity\.(?<method>\w+Async)\((?:[\w.]+,\s*)?typeof\((?<entity>\w+)\)")]
    private static partial Regex FieldSecurityCall();

    [GeneratedRegex(@"public async Task(?:<[^\n]*?>)? (?<name>\w+Async)\(\s*(?<prefix>\w+?)(?<kind>Create|Update|StatusUpdate)Dto \w+")]
    private static partial Regex WriteMethod();

    [GeneratedRegex(@"public static (?<dto>\w+Dto) \w+\(\s*(?<entity>Sys\w+) (?<parameter>\w+)")]
    private static partial Regex MapperMethod();

    [GeneratedRegex(@"\n {4}(?:/// <summary>|(?:public|private|protected|internal) )")]
    private static partial Regex NextMember();

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

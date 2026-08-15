// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Dtos.Metadata;
using XiHan.Framework.Localization.Abstractions.Enums;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 枚举元数据查询服务实现
/// </summary>
public sealed class EnumMetadataQueryService
    : IEnumMetadataQueryService
{
    // 扫描口径：已加载的 XiHan.BasicApp.* 程序集中、命名空间以 .Domain.Entities 或 .Domain.Enums 结尾的
    // public 顶层枚举（各模块领域枚举的落位约定），外加显式程序集白名单（实体直接引用的框架枚举）。
    // 端点只挂登录态、不挂权限码，故不整包放开 XiHan*：那会把框架全部枚举暴露给任意登录用户，并引入短名重复。
    private static readonly string[] DomainNamespaceSuffixes = [".Domain.Entities", ".Domain.Enums"];

    private static readonly string[] WhitelistedAssemblyNames = ["XiHan.Framework.Workflow.Abstractions"];

    // XML 摘要按程序集各取一份：只读单个程序集的 .xml 会让非本程序集枚举的描述恒为 null
    private static readonly ConcurrentDictionary<Assembly, XDocument?> XmlDocCache = new();

    // 短名重复记录（缓存首次构建时产生），由构造函数按进程报一次
    private static readonly List<string> AmbiguousShortNames = [];

    private static int _ambiguityReported;

    // 进程内 Lazy 缓存：枚举的结构性元数据（类型、成员名、数值、XML 描述）来自编译期程序集，
    // 仅随发版变化，进程生命周期内恒定，故静态缓存。显示文案（DisplayName）需按当前 UI 文化解析，
    // 不能进静态缓存，每次请求经 IEnumLocalizationService 解析（缺键回退 [Description]）。
    private static readonly Lazy<List<EnumStructure>> StructureCache = new(BuildAllStructures, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly IEnumLocalizationService _enumLocalizationService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EnumMetadataQueryService(
        IEnumLocalizationService enumLocalizationService,
        ILogger<EnumMetadataQueryService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _enumLocalizationService = enumLocalizationService;

        _ = StructureCache.Value;
        if (AmbiguousShortNames.Count > 0 && Interlocked.Exchange(ref _ambiguityReported, 1) == 0)
        {
            logger.LogWarning(
                "枚举元数据存在短名冲突，冲突组已整体丢弃：{Names}",
                string.Join(", ", AmbiguousShortNames));
        }
    }

    /// <summary>
    /// 获取全部枚举元数据
    /// </summary>
    public Task<List<EnumMetadataDto>> GetAllEnumsAsync()
    {
        var result = StructureCache.Value.ConvertAll(BuildLocalizedMetadata);
        return Task.FromResult(result);
    }

    /// <summary>
    /// 获取指定枚举类型的元数据
    /// </summary>
    public Task<EnumMetadataDto> GetEnumAsync(string enumTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enumTypeName);

        // 短名与全名都受理：列配置里存的是全名，端点键是短名
        var structure = StructureCache.Value
            .FirstOrDefault(item => string.Equals(item.EnumTypeName, enumTypeName, StringComparison.Ordinal)
                || string.Equals(item.FullName, enumTypeName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"枚举类型 '{enumTypeName}' 不存在。");

        return Task.FromResult(BuildLocalizedMetadata(structure));
    }

    /// <summary>
    /// 将结构性元数据与按当前文化解析的本地化显示文案合并为 DTO。
    /// 显示文案统一经 <see cref="IEnumLocalizationService"/> 解析，缺键时由其内部回退到 [Description]。
    /// </summary>
    private EnumMetadataDto BuildLocalizedMetadata(EnumStructure structure)
    {
        var definition = _enumLocalizationService.Get(structure.EnumType);

        // 按成员名建立本地化标签索引（Label 已含缺键回退到 [Description] 的逻辑）。
        var labelMap = definition.Items.ToDictionary(
            static item => item.Name,
            static item => item.Label,
            StringComparer.Ordinal);

        var items = structure.Items.ConvertAll(item => new EnumItemDto
        {
            Name = item.Name,
            Value = item.Value,
            DisplayName = labelMap.TryGetValue(item.Name, out var label) && !string.IsNullOrWhiteSpace(label)
                ? label
                : item.FallbackDisplayName,
            Description = item.Description
        });

        return new EnumMetadataDto
        {
            EnumTypeName = structure.EnumTypeName,
            FullName = structure.FullName,
            DisplayName = string.IsNullOrWhiteSpace(definition.DisplayName)
                ? structure.FallbackDisplayName
                : definition.DisplayName,
            Items = items
        };
    }

    /// <summary>
    /// 构建全部枚举的结构性元数据（Lazy 首次访问时执行一次）。
    /// </summary>
    private static List<EnumStructure> BuildAllStructures()
    {
        // 白名单程序集不是模块程序集、按需加载：首次请求早于 CLR 加载它时会永久缺席，故显式预加载
        foreach (var assemblyName in WhitelistedAssemblyNames)
        {
            try
            {
                _ = Assembly.Load(new AssemblyName(assemblyName));
            }
            catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
            {
                // 未部署该程序集时忽略
            }
        }

        var candidates = CollectCandidateEnumTypes();

        // 端点以短名为键：同名多类型时取 XiHan.BasicApp 下的；仍并列则整组丢弃并记录，
        // 不放任静默 last-wins 产出错标签
        var resolved = new List<Type>();
        foreach (var group in candidates.GroupBy(static type => type.Name, StringComparer.Ordinal))
        {
            var distinct = group.Distinct().ToList();
            if (distinct.Count == 1)
            {
                resolved.Add(distinct[0]);
                continue;
            }

            var preferred = distinct
                .Where(static type => type.Namespace?.StartsWith("XiHan.BasicApp.", StringComparison.Ordinal) == true)
                .ToList();
            if (preferred.Count == 1)
            {
                resolved.Add(preferred[0]);
                continue;
            }

            AmbiguousShortNames.Add(group.Key);
        }

        return
        [
            .. resolved
                .OrderBy(static type => type.Name, StringComparer.Ordinal)
                .Select(BuildEnumStructure)
        ];
    }

    /// <summary>
    /// 收集候选枚举类型
    /// </summary>
    private static List<Type> CollectCandidateEnumTypes()
    {
        var candidates = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            var name = assembly.GetName().Name;
            if (name is null)
            {
                continue;
            }

            var isBasicApp = name.StartsWith("XiHan.BasicApp.", StringComparison.Ordinal);
            var isWhitelisted = WhitelistedAssemblyNames.Contains(name, StringComparer.Ordinal);
            if (!isBasicApp && !isWhitelisted)
            {
                continue;
            }

            Type?[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                assemblyTypes = ex.Types;
            }
            catch (FileNotFoundException)
            {
                continue;
            }

            foreach (var type in assemblyTypes)
            {
                if (type is not { IsEnum: true, IsPublic: true, IsNested: false, Namespace: not null })
                {
                    continue;
                }

                // 白名单程序集整包收录；BasicApp 只收领域枚举命名空间，
                // 不把应用层 DTO/领域服务里的内部枚举一并暴露
                if (isBasicApp
                    && !Array.Exists(DomainNamespaceSuffixes, suffix => type.Namespace.EndsWith(suffix, StringComparison.Ordinal)))
                {
                    continue;
                }

                candidates.Add(type);
            }
        }

        return candidates;
    }

    private static EnumStructure BuildEnumStructure(Type enumType)
    {
        var items = Enum.GetValues(enumType)
            .Cast<object>()
            .Select(value => BuildEnumItemStructure(enumType, value))
            .ToList();

        return new EnumStructure
        {
            EnumType = enumType,
            EnumTypeName = enumType.Name,
            FullName = enumType.FullName!,
            FallbackDisplayName = ResolveEnumTypeDisplayName(enumType),
            Items = items
        };
    }

    private static EnumItemStructure BuildEnumItemStructure(Type enumType, object value)
    {
        var name = Enum.GetName(enumType, value)!;
        var field = enumType.GetField(name)!;

        return new EnumItemStructure
        {
            Name = name,
            Value = Convert.ToInt32(value, provider: null),
            FallbackDisplayName = ResolveEnumMemberDisplayName(field),
            Description = ResolveEnumMemberDescription(enumType, field)
        };
    }

    private static string? GetXmlDocSummary(Assembly assembly, string memberId)
    {
        var xmlDoc = XmlDocCache.GetOrAdd(assembly, LoadXmlDocumentation);
        if (xmlDoc is null)
        {
            return null;
        }

        var memberElement = xmlDoc.Root?
            .Elements("members")
            .Elements("member")
            .FirstOrDefault(element => string.Equals(element.Attribute("name")?.Value, memberId, StringComparison.Ordinal));

        var summaryText = memberElement?.Element("summary")?.Value;
        return string.IsNullOrWhiteSpace(summaryText) ? null : NormalizeXmlSummary(summaryText);
    }

    private static string MakeXmlMemberId(char prefix, string fullName)
    {
        return $"{prefix}:{fullName}";
    }

    private static string NormalizeXmlSummary(string raw)
    {
        var parts = raw.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select(static part => part.Trim()).Where(static part => part.Length > 0));
    }

    private static string? ResolveEnumMemberDescription(Type enumType, FieldInfo field)
    {
        var memberId = MakeXmlMemberId('F', $"{enumType.FullName}.{field.Name}");
        return GetXmlDocSummary(enumType.Assembly, memberId);
    }

    private static string ResolveEnumMemberDisplayName(FieldInfo field)
    {
        var descriptionAttr = field.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttr?.Description ?? field.Name;
    }

    private static string ResolveEnumTypeDisplayName(Type enumType)
    {
        var xmlSummary = GetXmlDocSummary(enumType.Assembly, MakeXmlMemberId('T', enumType.FullName!));
        if (!string.IsNullOrWhiteSpace(xmlSummary))
        {
            return xmlSummary;
        }

        var typeName = enumType.Name;
        if (typeName.EndsWith("Enum", StringComparison.Ordinal))
        {
            typeName = typeName[..^4];
        }

        return typeName;
    }

    private static XDocument? LoadXmlDocumentation(Assembly assembly)
    {
        try
        {
            var assemblyLocation = assembly.Location;
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                return null;
            }

            var xmlFilePath = Path.ChangeExtension(assemblyLocation, ".xml");
            if (!File.Exists(xmlFilePath))
            {
                return null;
            }

            return XDocument.Load(xmlFilePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 枚举的结构性元数据（与文化无关，可静态缓存）。
    /// </summary>
    private sealed class EnumStructure
    {
        public required Type EnumType { get; init; }

        public required string EnumTypeName { get; init; }

        /// <summary>
        /// 枚举类型全名
        /// </summary>
        public required string FullName { get; init; }

        /// <summary>
        /// 类型显示名缺键回退（来自 XML 摘要或类型名）。
        /// </summary>
        public required string FallbackDisplayName { get; init; }

        public required List<EnumItemStructure> Items { get; init; }
    }

    /// <summary>
    /// 枚举成员的结构性元数据（与文化无关，可静态缓存）。
    /// </summary>
    private sealed class EnumItemStructure
    {
        public required string Name { get; init; }

        public required int Value { get; init; }

        /// <summary>
        /// 成员显示名缺键回退（来自 [Description] 特性或字段名）。
        /// </summary>
        public required string FallbackDisplayName { get; init; }

        public required string? Description { get; init; }
    }
}

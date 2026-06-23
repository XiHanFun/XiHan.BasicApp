#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EnumMetadataQueryService
// Guid:3f4b5c28-f77d-48f7-89d6-0ed4c3bf504b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;
using System.Reflection;
using System.Xml.Linq;
using XiHan.BasicApp.Saas.Application.Dtos.Metadata;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Localization.Abstractions.Enums;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 枚举元数据查询服务实现
/// </summary>
public sealed class EnumMetadataQueryService
    : IEnumMetadataQueryService
{
    private const string TargetNamespace = "XiHan.BasicApp.Saas.Domain.Entities";
    private static readonly Assembly DomainAssembly = typeof(SysUser).Assembly;
    private static readonly Lazy<XDocument?> XmlDocCache = new(LoadXmlDocumentation, LazyThreadSafetyMode.ExecutionAndPublication);

    // 进程内 Lazy 缓存：枚举的结构性元数据（类型、成员名、数值、XML 描述）来自编译期程序集，
    // 仅随发版变化，进程生命周期内恒定，故静态缓存。显示文案（DisplayName）需按当前 UI 文化解析，
    // 不能进静态缓存，每次请求经 IEnumLocalizationService 解析（缺键回退 [Description]）。
    private static readonly Lazy<List<EnumStructure>> StructureCache = new(BuildAllStructures, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly IEnumLocalizationService _enumLocalizationService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EnumMetadataQueryService(IEnumLocalizationService enumLocalizationService)
    {
        _enumLocalizationService = enumLocalizationService;
    }

    /// <inheritdoc />
    public Task<List<EnumMetadataDto>> GetAllEnumsAsync()
    {
        var result = StructureCache.Value.ConvertAll(BuildLocalizedMetadata);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<EnumMetadataDto> GetEnumAsync(string enumTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enumTypeName);

        var structure = StructureCache.Value
            .FirstOrDefault(item => string.Equals(item.EnumTypeName, enumTypeName, StringComparison.Ordinal))
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
        var enumTypes = DomainAssembly.GetTypes()
            .Where(static type => type.IsEnum && string.Equals(type.Namespace, TargetNamespace, StringComparison.Ordinal))
            .OrderBy(static type => type.Name, StringComparer.Ordinal)
            .ToList();

        return enumTypes.ConvertAll(BuildEnumStructure);
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

    private static string? GetXmlDocSummary(string memberId)
    {
        var xmlDoc = XmlDocCache.Value;
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
        return GetXmlDocSummary(memberId);
    }

    private static string ResolveEnumMemberDisplayName(FieldInfo field)
    {
        var descriptionAttr = field.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttr?.Description ?? field.Name;
    }

    private static string ResolveEnumTypeDisplayName(Type enumType)
    {
        var xmlSummary = GetXmlDocSummary(MakeXmlMemberId('T', enumType.FullName!));
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

    private static XDocument? LoadXmlDocumentation()
    {
        try
        {
            var assemblyLocation = DomainAssembly.Location;
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

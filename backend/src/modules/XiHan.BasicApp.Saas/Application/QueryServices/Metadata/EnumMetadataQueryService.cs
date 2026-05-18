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

    /// <inheritdoc />
    public Task<List<EnumMetadataDto>> GetAllEnumsAsync()
    {
        var enumTypes = DomainAssembly.GetTypes()
            .Where(static type => type.IsEnum && string.Equals(type.Namespace, TargetNamespace, StringComparison.Ordinal))
            .OrderBy(static type => type.Name, StringComparer.Ordinal)
            .ToList();

        var result = enumTypes.ConvertAll(BuildEnumMetadata);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<EnumMetadataDto> GetEnumAsync(string enumTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enumTypeName);

        var enumType = DomainAssembly.GetTypes()
            .FirstOrDefault(type =>
                type.IsEnum &&
                string.Equals(type.Namespace, TargetNamespace, StringComparison.Ordinal) &&
                string.Equals(type.Name, enumTypeName, StringComparison.Ordinal));

        if (enumType is null)
        {
            throw new InvalidOperationException($"枚举类型 '{enumTypeName}' 不存在。");
        }

        return Task.FromResult(BuildEnumMetadata(enumType));
    }

    private static EnumItemDto BuildEnumItem(Type enumType, object value)
    {
        var name = Enum.GetName(enumType, value)!;
        var field = enumType.GetField(name)!;

        return new EnumItemDto
        {
            Name = name,
            Value = Convert.ToInt32(value, provider: null),
            DisplayName = ResolveEnumMemberDisplayName(field),
            Description = ResolveEnumMemberDescription(enumType, field)
        };
    }

    private static EnumMetadataDto BuildEnumMetadata(Type enumType)
    {
        var typeDisplayName = ResolveEnumTypeDisplayName(enumType);
        var items = Enum.GetValues(enumType)
            .Cast<object>()
            .Select(value => BuildEnumItem(enumType, value))
            .ToList();

        return new EnumMetadataDto
        {
            EnumTypeName = enumType.Name,
            DisplayName = typeDisplayName,
            Items = items
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
}

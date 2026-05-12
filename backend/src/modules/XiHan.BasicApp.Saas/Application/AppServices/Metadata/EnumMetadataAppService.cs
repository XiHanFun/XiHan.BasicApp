#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EnumMetadataAppService
// Guid:a7b2f1d6-3c9e-4a08-bf52-8d17e6c4b3a9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;
using System.Reflection;
using System.Xml.Linq;
using XiHan.BasicApp.Saas.Application.Dtos.Metadata;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices.Metadata;

/// <summary>
/// 枚举元数据应用服务
/// 通过反射扫描领域枚举定义，向前端暴露枚举名称、数值、显示名称与描述，
/// 消除前端硬编码枚举选项数组的需求。
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "枚举元数据")]
public sealed class EnumMetadataAppService : ApplicationServiceBase
{
    /// <summary>
    /// 领域层程序集
    /// </summary>
    private static readonly Assembly DomainAssembly = typeof(SysUser).Assembly;

    /// <summary>
    /// 枚举所在的目标命名空间
    /// </summary>
    private const string TargetNamespace = "XiHan.BasicApp.Saas.Domain.Entities";

    /// <summary>
    /// XML 文档缓存（延迟加载，线程安全）
    /// </summary>
    private static readonly Lazy<XDocument?> XmlDocCache = new(LoadXmlDocumentation, LazyThreadSafetyMode.ExecutionAndPublication);

    #region 公共方法

    /// <summary>
    /// 获取全部枚举元数据
    /// 扫描领域程序集中 <see cref="TargetNamespace"/> 命名空间下的所有枚举类型，
    /// 按类型名称排序后返回。
    /// </summary>
    /// <returns>全部枚举元数据列表</returns>
    public Task<List<EnumMetadataDto>> GetAllEnumsAsync()
    {
        var enumTypes = DomainAssembly.GetTypes()
            .Where(static type => type.IsEnum && string.Equals(type.Namespace, TargetNamespace, StringComparison.Ordinal))
            .OrderBy(static type => type.Name, StringComparer.Ordinal)
            .ToList();

        var result = enumTypes.ConvertAll(BuildEnumMetadata);
        return Task.FromResult(result);
    }

    /// <summary>
    /// 获取指定枚举类型的元数据
    /// 根据枚举类型名称精确匹配并返回单个枚举的完整元数据。
    /// </summary>
    /// <param name="enumTypeName">枚举类型名称（如 UserGender）</param>
    /// <returns>指定枚举的元数据</returns>
    /// <exception cref="InvalidOperationException">当指定名称的枚举类型不存在时抛出</exception>
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

    #endregion

    #region 枚举反射构建

    /// <summary>
    /// 构建单个枚举类型的完整元数据
    /// </summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns>枚举元数据 DTO</returns>
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

    /// <summary>
    /// 构建单个枚举项的元数据
    /// </summary>
    /// <param name="enumType">枚举类型</param>
    /// <param name="value">枚举值</param>
    /// <returns>枚举项 DTO</returns>
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

    #endregion

    #region 显示名称与描述解析

    /// <summary>
    /// 解析枚举类型级别的显示名称
    /// 优先级：XML 文档 Summary > 类型名称（去除"枚举"后缀）
    /// </summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns>类型显示名称</returns>
    private static string ResolveEnumTypeDisplayName(Type enumType)
    {
        // 优先从 XML 文档注释中获取 Summary
        var xmlSummary = GetXmlDocSummary(MakeXmlMemberId('T', enumType.FullName!));
        if (!string.IsNullOrWhiteSpace(xmlSummary))
        {
            return xmlSummary;
        }

        // 回退：使用类型名称，去除常见后缀
        var typeName = enumType.Name;
        if (typeName.EndsWith("Enum", StringComparison.Ordinal))
        {
            typeName = typeName[..^4];
        }

        return typeName;
    }

    /// <summary>
    /// 解析枚举成员的显示名称
    /// 读取字段上的 [Description] 特性，回退到字段名称。
    /// </summary>
    /// <param name="field">枚举字段信息</param>
    /// <returns>成员显示名称</returns>
    private static string ResolveEnumMemberDisplayName(FieldInfo field)
    {
        var descriptionAttr = field.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttr?.Description ?? field.Name;
    }

    /// <summary>
    /// 解析枚举成员的描述文本
    /// 从 XML 文档注释中获取该字段的 Summary，作为 Tooltip 级别的辅助描述。
    /// 注意：此处获取的是字段级 Summary，区别于 [Description] 特性的显示名称。
    /// 例如 UserGender.Male 的 Summary 为"男"（与 [Description] 相同），
    /// 而 DataPermissionScope.All 的 Summary 为"可以看到所有数据"（更详细的描述）。
    /// </summary>
    /// <param name="enumType">枚举类型</param>
    /// <param name="field">枚举字段信息</param>
    /// <returns>字段 XML 文档 Summary；若不存在则返回 null</returns>
    private static string? ResolveEnumMemberDescription(Type enumType, FieldInfo field)
    {
        var memberId = MakeXmlMemberId('F', $"{enumType.FullName}.{field.Name}");
        return GetXmlDocSummary(memberId);
    }

    /// <summary>
    /// 构造 XML 文档成员标识符
    /// 格式：{prefix}:{fullName}（如 T:Namespace.TypeName 或 F:Namespace.TypeName.FieldName）
    /// </summary>
    /// <param name="prefix">成员前缀（T 表示类型，F 表示字段）</param>
    /// <param name="fullName">完整名称</param>
    /// <returns>XML 文档成员 ID</returns>
    private static string MakeXmlMemberId(char prefix, string fullName)
    {
        return $"{prefix}:{fullName}";
    }

    /// <summary>
    /// 从缓存的 XML 文档中获取指定成员的 Summary 文本
    /// </summary>
    /// <param name="memberId">XML 文档成员 ID</param>
    /// <returns>Summary 文本；若不存在则返回 null</returns>
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

    /// <summary>
    /// 规范化 XML Summary 文本
    /// 去除前后空白并将内部连续空白合并为单个空格。
    /// </summary>
    /// <param name="raw">原始 Summary 文本</param>
    /// <returns>规范化后的文本</returns>
    private static string NormalizeXmlSummary(string raw)
    {
        // XML doc summary 中可能包含换行和缩进空白，统一整理为单行可读文本
        var parts = raw.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select(static part => part.Trim()).Where(static part => part.Length > 0));
    }

    #endregion

    #region XML 文档加载

    /// <summary>
    /// 加载领域程序集对应的 XML 文档文件
    /// 仅在首次调用时执行，结果被 <see cref="XmlDocCache"/> 缓存。
    /// </summary>
    /// <returns>XDocument 实例；若文件不存在或解析失败则返回 null</returns>
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
            // XML 文档不可用时静默降级，不影响核心功能
            return null;
        }
    }

    #endregion
}

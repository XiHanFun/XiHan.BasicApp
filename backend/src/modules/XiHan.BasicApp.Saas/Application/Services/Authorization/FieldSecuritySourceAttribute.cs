// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// DTO 属性改了名：声明它来自实体的哪个字段，字段安全按那个字段的规则给它打码
/// </summary>
/// <remarks>
/// 规则按实体字段名配置，响应打码按 DTO 属性名匹配。映射器把 <c>UserSessionId</c> 映射成 <c>SessionId</c> 这类改名，
/// 不声明就对不上，配在 <c>UserSessionId</c> 上的规则打不到它。有测试扫描映射器，改名未声明即失败。
/// </remarks>
/// <param name="entityField">来源实体的属性名</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FieldSecuritySourceAttribute(string entityField) : Attribute
{
    /// <summary>
    /// 来源实体的属性名
    /// </summary>
    public string EntityField { get; } = entityField;
}

/// <summary>
/// 由多个实体映射出来的 DTO（如跨日志类型的链路时间线）：每个实例自己说明来源实体与字段对应
/// </summary>
/// <remarks>
/// 用显式接口实现，免得这两个成员被序列化进响应。
/// </remarks>
public interface IFieldSecurityMultiSourceDto
{
    /// <summary>
    /// 本实例的来源实体（须已登记字段安全）；来源实体不受字段安全管理时为空
    /// </summary>
    Type? FieldSecurityEntity { get; }

    /// <summary>
    /// DTO 属性对应的来源实体字段；与来源无关的属性返回空
    /// </summary>
    string? FieldSecuritySourceOf(string dtoProperty);
}

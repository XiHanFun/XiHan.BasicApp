// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模板实体的持久化结构约束与自校验规则测试。
/// </summary>
/// <remarks>
/// 软删除实体的唯一索引必须把 <c>IsDeleted</c> 一起纳入：漏掉它以后，
/// 同一租户下"删掉再建同编码"会撞上历史软删行的唯一约束而永久建不出来；
/// 而 <c>IsGlobal</c> 是由 TenantId 派生的计算属性，必须保持 <c>IsIgnore</c>，
/// 否则 CodeFirst 会给它建一列，从此出现"列值与租户号不一致"的状态漂移。
/// 这两条都不会在编译期报错，只能靠反射断言守住。
/// </remarks>
public sealed class PrintingExtraEntityStructureTests
{
    /// <summary>
    /// 实体必须带 <c>SugarTable</c> 并保持既有表名，改名等同于线上改表。
    /// </summary>
    [Fact]
    public void Entity_ShouldDeclareStableSugarTableName()
    {
        var table = typeof(SysPrintTemplate).GetCustomAttributesData()
            .SingleOrDefault(attribute => attribute.AttributeType.Name == "SugarTable");

        Assert.True(table is not null, "SysPrintTemplate 缺少 [SugarTable]，CodeFirst 不会为它建表。");
        var tableName = table!.NamedArguments
            .Where(argument => argument.MemberName == "TableName")
            .Select(argument => argument.TypedValue.Value as string)
            .FirstOrDefault();
        Assert.Equal("Sys_Print_Template", tableName);
    }

    /// <summary>
    /// 模板编码的唯一索引必须同时覆盖租户与软删标记，缺一都会造成跨租户冲突或删后不可重建。
    /// </summary>
    [Fact]
    public void Entity_UniqueIndex_ShouldCoverTenantCodeAndSoftDelete()
    {
        var uniqueIndexes = typeof(SysPrintTemplate).GetCustomAttributesData()
            .Where(attribute => attribute.AttributeType.Name == "SugarIndexAttribute")
            .Select(attribute => attribute.ConstructorArguments
                .SelectMany(Flatten)
                .ToList())
            .Where(values => values.OfType<bool>().Any(isUnique => isUnique))
            .ToList();

        var unique = Assert.Single(uniqueIndexes);
        var columns = unique.OfType<string>().ToList();
        Assert.Contains(nameof(SysPrintTemplate.TenantId), columns, StringComparer.Ordinal);
        Assert.Contains(nameof(SysPrintTemplate.TemplateCode), columns, StringComparer.Ordinal);
        Assert.True(
            columns.Contains(nameof(SysPrintTemplate.IsDeleted), StringComparer.Ordinal),
            $"唯一索引未包含 IsDeleted，软删后无法用同编码重建模板。当前列：{string.Join("、", columns)}");
    }

    /// <summary>
    /// 派生属性 <see cref="SysPrintTemplate.IsGlobal"/> 不得落库，且取值完全由租户号决定。
    /// </summary>
    /// <param name="tenantId">租户号。</param>
    /// <param name="expected">期望的全局判定。</param>
    [Theory]
    [InlineData(0L, true)]
    [InlineData(1L, false)]
    [InlineData(long.MaxValue, false)]
    public void IsGlobal_ShouldBeDerivedFromTenantIdAndNotPersisted(long tenantId, bool expected)
    {
        var property = typeof(SysPrintTemplate).GetProperty(nameof(SysPrintTemplate.IsGlobal))!;
        var column = property.GetCustomAttributesData()
            .SingleOrDefault(attribute => attribute.AttributeType.Name == "SugarColumn");

        Assert.True(column is not null, "IsGlobal 缺少 [SugarColumn(IsIgnore = true)]，CodeFirst 会给派生属性建列。");
        Assert.Contains(column!.NamedArguments, argument => argument.MemberName == "IsIgnore" && Equals(argument.TypedValue.Value, true));
        Assert.Null(property.GetSetMethod());
        Assert.Equal(expected, new SysPrintTemplate { TenantId = tenantId }.IsGlobal);
    }

    /// <summary>
    /// 实体必须继承全量审计基类，租户过滤、软删除与行版本都来自这条继承链。
    /// </summary>
    [Fact]
    public void Entity_ShouldInheritFullAuditedBase()
    {
        Assert.True(
            typeof(SysPrintTemplate).IsAssignableTo(typeof(BasicAppFullAuditedEntity)),
            "SysPrintTemplate 未继承 BasicAppFullAuditedEntity，会同时丢掉租户过滤、软删除与乐观并发。");
        Assert.True(
            typeof(SysPrintTemplate).IsAssignableTo(typeof(IValidatableObject)),
            "SysPrintTemplate 未实现 IValidatableObject，实体自校验规则不会被调用。");
    }

    /// <summary>
    /// 数据源编码必须允许为空，自由模板才不需要伪造一个代码注册项。
    /// </summary>
    [Fact]
    public void DataSourceCode_ShouldBeNullableColumn()
    {
        var column = typeof(SysPrintTemplate)
            .GetProperty(nameof(SysPrintTemplate.DataSourceCode))!
            .GetCustomAttributesData()
            .Single(attribute => attribute.AttributeType.Name == "SugarColumn");

        Assert.Contains(column.NamedArguments, argument => argument.MemberName == "IsNullable" && Equals(argument.TypedValue.Value, true));
    }

    /// <summary>
    /// 新建实体的默认值必须落在"平台租户、启用、非开放"这一组安全默认上。
    /// </summary>
    [Fact]
    public void Entity_Defaults_ShouldBePlatformEnabledAndClosed()
    {
        var template = new SysPrintTemplate();

        Assert.Equal(0L, template.TenantId);
        Assert.Equal(EnableStatus.Enabled, template.Status);
        Assert.False(template.AllowTenantUse);
        Assert.Equal("0.0.60", template.EngineVersion);
        Assert.Equal(0, template.Sort);
        Assert.Null(template.DataSourceCode);
        Assert.Null(template.Remark);
    }

    /// <summary>
    /// 各业务字段的列长度必须与领域服务的校验上限对齐，否则超限值会在落库时被截断。
    /// </summary>
    /// <param name="propertyName">实体属性名。</param>
    /// <param name="expectedLength">期望列长度。</param>
    [Theory]
    [InlineData(nameof(SysPrintTemplate.TemplateCode), 100)]
    [InlineData(nameof(SysPrintTemplate.DataSourceCode), 100)]
    [InlineData(nameof(SysPrintTemplate.TemplateName), 100)]
    [InlineData(nameof(SysPrintTemplate.EngineVersion), 32)]
    [InlineData(nameof(SysPrintTemplate.Remark), 500)]
    public void Columns_LengthShouldMatchDomainValidationLimits(string propertyName, int expectedLength)
    {
        var column = typeof(SysPrintTemplate)
            .GetProperty(propertyName)!
            .GetCustomAttributesData()
            .Single(attribute => attribute.AttributeType.Name == "SugarColumn");
        var length = column.NamedArguments
            .Where(argument => argument.MemberName == "Length")
            .Select(argument => (int?)argument.TypedValue.Value)
            .FirstOrDefault();

        Assert.True(length == expectedLength, $"{propertyName} 的列长度为 {length}，与领域校验上限 {expectedLength} 不一致。");
    }

    /// <summary>
    /// 完整合法的实体自校验必须零失败项。
    /// </summary>
    [Fact]
    public void Validate_CompleteEntity_ShouldReturnNoResults()
    {
        Assert.Empty(Validate(CreateValidTemplate()));
    }

    /// <summary>
    /// 五条自校验规则各自独立触发，失败项要点名对应的成员，前端才能定位到具体输入框。
    /// </summary>
    /// <param name="mutation">把合法实体改坏的方式。</param>
    /// <param name="expectedMember">期望被点名的成员。</param>
    [Theory]
    [InlineData("code", nameof(SysPrintTemplate.TemplateCode))]
    [InlineData("dataSource", nameof(SysPrintTemplate.DataSourceCode))]
    [InlineData("name", nameof(SysPrintTemplate.TemplateName))]
    [InlineData("json", nameof(SysPrintTemplate.TemplateJson))]
    [InlineData("engine", nameof(SysPrintTemplate.EngineVersion))]
    public void Validate_BrokenField_ShouldReportOwningMember(string mutation, string expectedMember)
    {
        var template = CreateValidTemplate();
        switch (mutation)
        {
            case "code":
                template.TemplateCode = "  ";
                break;
            case "dataSource":
                template.DataSourceCode = "  ";
                break;
            case "name":
                template.TemplateName = string.Empty;
                break;
            case "json":
                template.TemplateJson = "   ";
                break;
            default:
                template.EngineVersion = string.Empty;
                break;
        }

        var result = Assert.Single(Validate(template));
        Assert.Equal([expectedMember], result.MemberNames);
    }

    /// <summary>
    /// 数据源编码为 null 表示自由模板，属于合法状态，不得触发"应保存为 null"的失败项。
    /// </summary>
    [Fact]
    public void Validate_NullDataSourceCode_ShouldBeAccepted()
    {
        var template = CreateValidTemplate();
        template.DataSourceCode = null;

        Assert.Empty(Validate(template));
    }

    /// <summary>
    /// 执行实体自校验并收集全部失败项。
    /// </summary>
    private static List<ValidationResult> Validate(SysPrintTemplate template)
    {
        return [.. template.Validate(new ValidationContext(template))];
    }

    /// <summary>
    /// 创建各字段均合法的模板实体。
    /// </summary>
    private static SysPrintTemplate CreateValidTemplate()
    {
        return new SysPrintTemplate
        {
            TenantId = 7,
            TemplateCode = "ORDER",
            DataSourceCode = "system.print-demo",
            TemplateName = "订单模板",
            TemplateJson = "{\"panels\":[{\"printElements\":[]}]}",
            EngineVersion = "0.0.60"
        };
    }

    /// <summary>
    /// 展平特性构造参数，兼容 params 数组形式的索引列声明。
    /// </summary>
    private static IEnumerable<object?> Flatten(CustomAttributeTypedArgument argument)
    {
        if (argument.Value is IReadOnlyCollection<CustomAttributeTypedArgument> nested)
        {
            return nested.SelectMany(Flatten);
        }

        return [argument.Value];
    }
}

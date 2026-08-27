// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Inference;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 表配置推断引擎测试。
/// </summary>
/// <remarks>
/// 推断引擎是"输入最小化"的落地载体，规则之间有明确的优先级：
/// <list type="number">
/// <item>命中项目内实体（<see cref="IEntityMetadataCatalog"/>）时，类名/命名空间/属性名/枚举全部取自实体，
/// 名称约定完全让位；未命中才走表名前缀剥离 + Pascal 化。</item>
/// <item>控件推断三层叠加：DB 类型映射给默认值 → 文本列再由列名语义覆盖 → 枚举属性最终覆盖为下拉。
/// 顺序反了会让枚举列退化成文本框、或让长文本列的多行输入被语义规则以外的规则抢走。</item>
/// <item>树表判定必须"父级列 + 显示名列"同时命中才成立——只有父级列却没有显示名列时保持单表，
/// 否则会生成一个无法渲染展开列的树页面。</item>
/// </list>
/// </remarks>
public sealed class CodeGenTableConfigInferenceEngineTests
{
    private readonly Mock<IEntityMetadataCatalog> _catalog = new();

    /// <summary>
    /// 构造推断引擎（类型映射用真实实现，保证推断与生成同口径）。
    /// </summary>
    /// <param name="tablePrefixes">表前缀配置</param>
    private TableConfigInferenceEngine CreateEngine(string tablePrefixes = "Sys_,Saas_")
    {
        return new TableConfigInferenceEngine(
            _catalog.Object,
            new DefaultTypeMappingProvider(),
            Options.Create(new CodeGenerationOptions { TablePrefixes = tablePrefixes }));
    }

    /// <summary>
    /// 让目录把指定表识别为项目内实体。
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="entityType">实体类型</param>
    private void GivenRegisteredEntity(string tableName, Type entityType)
    {
        var resolved = entityType;
        _catalog.Setup(catalog => catalog.TryGetEntityType(tableName, out resolved)).Returns(true);
    }

    /// <summary>
    /// 构造一列 DB 结构。
    /// </summary>
    /// <param name="columnName">列名</param>
    /// <param name="dbType">DB 类型</param>
    /// <param name="isPrimaryKey">是否主键</param>
    /// <param name="isIdentity">是否自增</param>
    /// <param name="isNullable">是否可空</param>
    /// <param name="length">长度</param>
    private static ColumnSchema DbColumn(
        string columnName,
        string dbType = "varchar",
        bool isPrimaryKey = false,
        bool isIdentity = false,
        bool isNullable = false,
        int? length = null)
    {
        return new ColumnSchema
        {
            ColumnName = columnName,
            DbType = dbType,
            IsPrimaryKey = isPrimaryKey,
            IsIdentity = isIdentity,
            IsNullable = isNullable,
            Length = length
        };
    }

    /// <summary>
    /// 构造一张表结构。
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="tableComment">表注释</param>
    /// <param name="columns">列集合</param>
    /// <param name="primaryKeyColumn">主键列名</param>
    private static TableSchema Table(
        string tableName = "Sys_Product",
        string? tableComment = "产品表",
        IReadOnlyList<ColumnSchema>? columns = null,
        string? primaryKeyColumn = "Basic_Id")
    {
        return new TableSchema
        {
            TableName = tableName,
            TableComment = tableComment,
            PrimaryKeyColumn = primaryKeyColumn,
            Columns = columns ?? [DbColumn("Basic_Id", "bigint", isPrimaryKey: true)]
        };
    }

    /// <summary>
    /// 默认推断上下文。
    /// </summary>
    /// <param name="userName">当前用户名</param>
    /// <param name="databaseType">数据库类型</param>
    private static InferenceContext Context(string? userName = "tester", DatabaseType databaseType = DatabaseType.MySql)
        => new(userName, databaseType);

    /// <summary>
    /// 表结构与上下文为空必须直接拒绝。
    /// </summary>
    [Fact]
    public void Infer_NullArgumentsShouldThrow()
    {
        var engine = CreateEngine();

        Assert.Throws<ArgumentNullException>(() => engine.Infer(null!, Context()));
        Assert.Throws<ArgumentNullException>(() => engine.Infer(Table(), null!));
    }

    /// <summary>
    /// 外部库的表走名称约定：剥离配置的表前缀后 Pascal 化，且不推断命名空间与模块。
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="expected">期望类名</param>
    [Theory]
    [InlineData("Sys_Product", "Product")]
    [InlineData("sys_product", "Product")]
    [InlineData("SYS_product_item", "ProductItem")]
    [InlineData("Saas_Tenant", "Tenant")]
    [InlineData("order_detail", "OrderDetail")]
    public void Infer_ExternalTableShouldDeriveClassNameFromTableName(string tableName, string expected)
    {
        var engine = CreateEngine();

        var suggestion = engine.Infer(Table(tableName: tableName), Context());

        Assert.False(suggestion.FromRegisteredEntity);
        Assert.Equal(expected, suggestion.ClassName, StringComparer.Ordinal);
        Assert.Null(suggestion.Namespace);
        Assert.Null(suggestion.ModuleName);
    }

    /// <summary>
    /// 前缀剥离取最长匹配，避免短前缀先命中导致少剥。
    /// </summary>
    [Fact]
    public void Infer_ShouldStripLongestMatchingPrefix()
    {
        var engine = CreateEngine("Sys_,Sys_Code");

        var suggestion = engine.Infer(Table(tableName: "Sys_CodeGen_Table"), Context());

        Assert.Equal("GenTable", suggestion.ClassName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 表名恰好等于前缀时不得剥空，否则类名会变成空串。
    /// </summary>
    [Fact]
    public void Infer_TableNameEqualToPrefixShouldNotBeStripped()
    {
        var engine = CreateEngine("Sys_");

        var suggestion = engine.Infer(Table(tableName: "Sys_"), Context());

        Assert.Equal("Sys", suggestion.ClassName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 项目内的表直接取实体类型：类名、根命名空间与模块名全部来自实体，不再走名称约定。
    /// </summary>
    [Fact]
    public void Infer_RegisteredEntityShouldDeriveIdentityFromEntityType()
    {
        GivenRegisteredEntity("Sys_CodeGen_Template", typeof(SysCodeGenTemplate));
        var engine = CreateEngine();

        var suggestion = engine.Infer(Table(tableName: "Sys_CodeGen_Template"), Context());

        Assert.True(suggestion.FromRegisteredEntity);
        Assert.Equal("SysCodeGenTemplate", suggestion.ClassName, StringComparer.Ordinal);
        Assert.Equal("XiHan.BasicApp.CodeGeneration", suggestion.Namespace, StringComparer.Ordinal);
        Assert.Equal("CodeGeneration", suggestion.ModuleName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 业务名取表注释并去掉结尾的"表"字；注释为空时回退类名。
    /// </summary>
    /// <param name="tableComment">表注释</param>
    /// <param name="expected">期望业务名</param>
    [Theory]
    [InlineData("产品表", "产品")]
    [InlineData("  产品信息表  ", "产品信息")]
    [InlineData("产品", "产品")]
    [InlineData(null, "Product")]
    [InlineData("", "Product")]
    [InlineData("   ", "Product")]
    [InlineData("表", "Product")]
    public void Infer_BusinessNameShouldComeFromTableComment(string? tableComment, string expected)
    {
        var engine = CreateEngine();

        var suggestion = engine.Infer(Table(tableComment: tableComment), Context());

        Assert.Equal(expected, suggestion.BusinessName, StringComparer.Ordinal);
        Assert.Equal(expected, suggestion.FunctionName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 作者取当前登录用户名；用户名空白时留空而不是写入空串。
    /// </summary>
    /// <param name="userName">当前用户名</param>
    /// <param name="expected">期望作者</param>
    [Theory]
    [InlineData("tester", "tester")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void Infer_AuthorShouldFollowCurrentUserName(string? userName, string? expected)
    {
        var engine = CreateEngine();

        var suggestion = engine.Infer(Table(), Context(userName: userName));

        Assert.Equal(expected, suggestion.Author, StringComparer.Ordinal);
    }

    /// <summary>
    /// 主键列名原样透传，列序按扫描顺序编号。
    /// </summary>
    [Fact]
    public void Infer_ShouldPassThroughPrimaryKeyAndNumberColumnsInOrder()
    {
        var engine = CreateEngine();
        var schema = Table(columns:
        [
            DbColumn("Basic_Id", "bigint", isPrimaryKey: true),
            DbColumn("Product_Name"),
            DbColumn("Price", "decimal")
        ]);

        var suggestion = engine.Infer(schema, Context());

        Assert.Equal("Basic_Id", suggestion.PrimaryKeyColumn, StringComparer.Ordinal);
        Assert.Equal([0, 1, 2], suggestion.Columns.Select(column => column.Sort));
    }

    /// <summary>
    /// 基类托管的通用列必须标 IsCommon，并把列表/新增/编辑/查询四个开关一并关掉。
    /// </summary>
    /// <param name="columnName">通用列名</param>
    [Theory]
    [InlineData("Basic_Id")]
    [InlineData("TenantId")]
    [InlineData("Created_Time")]
    [InlineData("Is_Deleted")]
    [InlineData("Row_Version")]
    public void Infer_BaseColumnShouldBeMarkedCommonAndSwitchedOff(string columnName)
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn(columnName, "bigint")]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.True(column.IsCommon);
        Assert.False(column.IsList);
        Assert.False(column.IsInsert);
        Assert.False(column.IsEdit);
        Assert.False(column.IsQuery);
    }

    /// <summary>
    /// 主键列即便不是基类托管列也不参与列表/新增/编辑。
    /// </summary>
    [Fact]
    public void Infer_PrimaryKeyColumnShouldNotBeEditable()
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn("Product_Code", isPrimaryKey: true)]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.False(column.IsCommon);
        Assert.False(column.IsList);
        Assert.False(column.IsInsert);
        Assert.False(column.IsEdit);
    }

    /// <summary>
    /// 普通业务列默认进入列表/新增/编辑。
    /// </summary>
    [Fact]
    public void Infer_BusinessColumnShouldBeEditableByDefault()
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn("Price", "decimal")]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.True(column.IsList);
        Assert.True(column.IsInsert);
        Assert.True(column.IsEdit);
    }

    /// <summary>
    /// 默认查询列只挑常用维度关键字，避免搜索区被全列撑爆。
    /// </summary>
    /// <param name="columnName">列名</param>
    /// <param name="expected">是否默认参与查询</param>
    [Theory]
    [InlineData("Product_Name", true)]
    [InlineData("Title", true)]
    [InlineData("Product_Code", true)]
    [InlineData("Status", true)]
    [InlineData("State", true)]
    [InlineData("Order_Type", true)]
    [InlineData("Publish_Time", true)]
    [InlineData("Birth_Date", true)]
    [InlineData("Price", false)]
    [InlineData("Weight", false)]
    public void Infer_DefaultQueryColumnsShouldFollowKeywordList(string columnName, bool expected)
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn(columnName)]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.Equal(expected, column.IsQuery);
    }

    /// <summary>
    /// 通用列即便列名命中查询关键字也不得参与查询（先判可编辑再判关键字）。
    /// </summary>
    [Fact]
    public void Infer_BaseColumnShouldNeverBecomeQueryColumnEvenWhenKeywordMatches()
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn("Created_Time", "datetime")]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.False(column.IsQuery);
    }

    /// <summary>
    /// 必填推断：非空、非自增、非主键三者同时成立才必填。
    /// </summary>
    /// <param name="isNullable">是否可空</param>
    /// <param name="isIdentity">是否自增</param>
    /// <param name="isPrimaryKey">是否主键</param>
    /// <param name="expected">期望必填</param>
    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    public void Infer_RequiredShouldRequireNonNullableNonIdentityNonPrimaryKey(
        bool isNullable,
        bool isIdentity,
        bool isPrimaryKey,
        bool expected)
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn("Price", "decimal", isPrimaryKey, isIdentity, isNullable)]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.Equal(expected, column.IsRequired);
    }

    /// <summary>
    /// C#/TS 类型与默认控件、查询方式必须来自类型映射器，与生成期同口径。
    /// </summary>
    [Fact]
    public void Infer_ColumnTypesShouldComeFromTypeMappingProvider()
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn("Publish_Time", "datetime", isNullable: true)]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.Equal("DateTimeOffset?", column.CSharpType, StringComparer.Ordinal);
        Assert.Equal("string", column.TsType, StringComparer.Ordinal);
        Assert.Equal(HtmlType.DateTimePicker, column.HtmlType);
        Assert.Equal(QueryType.Between, column.QueryType);
    }

    /// <summary>
    /// 文本列的控件由列名语义覆盖类型映射的默认值。
    /// </summary>
    [Fact]
    public void Infer_TextColumnShouldBeOverriddenByColumnSemantics()
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn("Avatar_Url"), DbColumn("Remark"), DbColumn("Product_Name")]);

        var columns = engine.Infer(schema, Context()).Columns;

        Assert.Equal(HtmlType.ImageUpload, columns[0].HtmlType);
        Assert.Equal(HtmlType.Textarea, columns[1].HtmlType);
        Assert.Equal(HtmlType.Input, columns[2].HtmlType);
    }

    /// <summary>
    /// 非文本列不受列名语义影响：数值/布尔/时间的控件完全由类型映射决定。
    /// </summary>
    /// <remarks>
    /// 名字里带 <c>remark</c> 的数值列若被语义规则改成多行文本框，表单会拿文本域收数字。
    /// </remarks>
    [Fact]
    public void Infer_NonTextColumnShouldIgnoreColumnSemantics()
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn("Remark_Count", "int"), DbColumn("Is_Image", "bit")]);

        var columns = engine.Infer(schema, Context()).Columns;

        Assert.Equal(HtmlType.InputNumber, columns[0].HtmlType);
        Assert.Equal(HtmlType.Switch, columns[1].HtmlType);
    }

    /// <summary>
    /// 超长字符串列即使列名无语义，也应推断为多行输入。
    /// </summary>
    [Fact]
    public void Infer_LongTextColumnShouldBecomeTextarea()
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn("Extra", "varchar", length: 2000)]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.Equal(HtmlType.Textarea, column.HtmlType);
    }

    /// <summary>
    /// 项目内实体的属性名优先于列名 Pascal 化，保证产物属性名与既有实体一致。
    /// </summary>
    [Fact]
    public void Infer_RegisteredEntityShouldResolvePropertyNameFromEntity()
    {
        GivenRegisteredEntity("Sys_CodeGen_Template", typeof(SysCodeGenTemplate));
        var engine = CreateEngine();
        var schema = Table(tableName: "Sys_CodeGen_Template", columns:
        [
            DbColumn("Template_Code"),
            DbColumn("not_in_entity")
        ]);

        var columns = engine.Infer(schema, Context()).Columns;

        Assert.Equal("TemplateCode", columns[0].CSharpProperty, StringComparer.Ordinal);
        Assert.Equal("NotInEntity", columns[1].CSharpProperty, StringComparer.Ordinal);
    }

    /// <summary>
    /// 外部表的属性名一律由列名 Pascal 化得到。
    /// </summary>
    [Fact]
    public void Infer_ExternalTableShouldPascalizeColumnNameAsProperty()
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn("product_name")]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.Equal("ProductName", column.CSharpProperty, StringComparer.Ordinal);
    }

    /// <summary>
    /// 实体属性是枚举时，推断为枚举选择器 + 下拉 + 等值查询，且 C# 类型产出真枚举而不是 int。
    /// </summary>
    /// <remarks>
    /// 产出 int 会让前端选中值（经全局转换器传成员名字符串）与行数据永远对不上。
    /// </remarks>
    [Fact]
    public void Infer_EnumPropertyShouldBecomeEnumSelectorWithRealEnumType()
    {
        GivenRegisteredEntity("Sys_CodeGen_Template", typeof(SysCodeGenTemplate));
        var engine = CreateEngine();
        var schema = Table(tableName: "Sys_CodeGen_Template", columns: [DbColumn("Template_Type", "int")]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.Equal(DictSelectorType.EnumSelector, column.DictSelectorType);
        Assert.Equal(typeof(TemplateType).FullName, column.EnumTypeName, StringComparer.Ordinal);
        Assert.Equal(HtmlType.Select, column.HtmlType);
        Assert.Equal(QueryType.Equal, column.QueryType);
        Assert.Equal("TemplateType", column.CSharpType, StringComparer.Ordinal);
        Assert.Equal("string", column.TsType, StringComparer.Ordinal);
    }

    /// <summary>
    /// 可空的枚举列产出可空枚举类型。
    /// </summary>
    [Fact]
    public void Infer_NullableEnumColumnShouldProduceNullableEnumType()
    {
        GivenRegisteredEntity("Sys_CodeGen_Template", typeof(SysCodeGenTemplate));
        var engine = CreateEngine();
        var schema = Table(tableName: "Sys_CodeGen_Template", columns: [DbColumn("Template_Type", "int", isNullable: true)]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.Equal("TemplateType?", column.CSharpType, StringComparer.Ordinal);
    }

    /// <summary>
    /// 实体上声明为可空枚举的属性同样解析得到底层枚举类型。
    /// </summary>
    [Fact]
    public void Infer_NullableEnumPropertyShouldUnwrapUnderlyingEnumType()
    {
        GivenRegisteredEntity("Sys_CodeGen_TableColumn", typeof(SysCodeGenTableColumn));
        var engine = CreateEngine();
        var schema = Table(tableName: "Sys_CodeGen_TableColumn", columns: [DbColumn("Dict_Selector_Type", "int")]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.Equal(DictSelectorType.EnumSelector, column.DictSelectorType);
        Assert.Equal(typeof(DictSelectorType).FullName, column.EnumTypeName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 非枚举属性不得被判成枚举选择器。
    /// </summary>
    [Fact]
    public void Infer_NonEnumPropertyShouldNotBecomeEnumSelector()
    {
        GivenRegisteredEntity("Sys_CodeGen_Template", typeof(SysCodeGenTemplate));
        var engine = CreateEngine();
        var schema = Table(tableName: "Sys_CodeGen_Template", columns: [DbColumn("Template_Code")]);

        var column = engine.Infer(schema, Context()).Columns[0];

        Assert.Null(column.DictSelectorType);
        Assert.Null(column.EnumTypeName);
    }

    /// <summary>
    /// 默认判为单表；无自关联列时不得出现树表字段。
    /// </summary>
    [Fact]
    public void Infer_WithoutSelfReferenceShouldStaySingleTable()
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn("Product_Name")]);

        var suggestion = engine.Infer(schema, Context());

        Assert.Equal(TemplateType.Single, suggestion.TemplateType);
        Assert.Null(suggestion.TreeParentColumn);
        Assert.Null(suggestion.TreeNameColumn);
    }

    /// <summary>
    /// 父级列与显示名列同时命中才判为树表。
    /// </summary>
    /// <param name="parentColumn">父级列名</param>
    /// <param name="nameColumn">显示名列名</param>
    [Theory]
    [InlineData("Parent_Id", "Name")]
    [InlineData("Pid", "Title")]
    [InlineData("Parent_Code", "Label")]
    [InlineData("ParentId", "Display_Name")]
    [InlineData("Product_Parent_Id", "Product_Name")]
    public void Infer_SelfReferenceWithNameColumnShouldBecomeTree(string parentColumn, string nameColumn)
    {
        var engine = CreateEngine();
        var schema = Table(columns:
        [
            DbColumn("Basic_Id", "bigint", isPrimaryKey: true),
            DbColumn(parentColumn, "bigint", isNullable: true),
            DbColumn(nameColumn)
        ]);

        var suggestion = engine.Infer(schema, Context());

        Assert.Equal(TemplateType.Tree, suggestion.TemplateType);
        Assert.Equal(parentColumn, suggestion.TreeParentColumn, StringComparer.Ordinal);
        Assert.Equal(nameColumn, suggestion.TreeNameColumn, StringComparer.Ordinal);
    }

    /// <summary>
    /// 只有父级列却找不到显示名列时保持单表，不硬判为树表。
    /// </summary>
    [Fact]
    public void Infer_ParentColumnWithoutNameColumnShouldStaySingleTable()
    {
        var engine = CreateEngine();
        var schema = Table(columns:
        [
            DbColumn("Basic_Id", "bigint", isPrimaryKey: true),
            DbColumn("Parent_Id", "bigint", isNullable: true),
            DbColumn("Price", "decimal")
        ]);

        var suggestion = engine.Infer(schema, Context());

        Assert.Equal(TemplateType.Single, suggestion.TemplateType);
        Assert.Null(suggestion.TreeParentColumn);
        Assert.Null(suggestion.TreeNameColumn);
    }

    /// <summary>
    /// 主键列即便叫 Pid 也不得被当成父级列。
    /// </summary>
    [Fact]
    public void Infer_PrimaryKeyNamedLikeParentShouldNotTriggerTreeDetection()
    {
        var engine = CreateEngine();
        var schema = Table(columns:
        [
            DbColumn("Pid", "bigint", isPrimaryKey: true),
            DbColumn("Name")
        ]);

        var suggestion = engine.Infer(schema, Context());

        Assert.Equal(TemplateType.Single, suggestion.TemplateType);
    }

    /// <summary>
    /// 列集合为空时不得抛异常，返回空列建议。
    /// </summary>
    [Fact]
    public void Infer_EmptyColumnsShouldReturnEmptySuggestionColumns()
    {
        var engine = CreateEngine();

        var suggestion = engine.Infer(Table(columns: []), Context());

        Assert.Empty(suggestion.Columns);
        Assert.Equal(TemplateType.Single, suggestion.TemplateType);
    }

    /// <summary>
    /// 推断是纯函数：同一输入连续两次推断的结果必须逐字段一致。
    /// </summary>
    [Fact]
    public void Infer_ShouldBeDeterministic()
    {
        var engine = CreateEngine();
        var schema = Table(columns: [DbColumn("Basic_Id", "bigint", isPrimaryKey: true), DbColumn("Product_Name")]);

        var first = engine.Infer(schema, Context());
        var second = engine.Infer(schema, Context());

        Assert.Equal(first.ClassName, second.ClassName, StringComparer.Ordinal);
        Assert.Equal(first.TemplateType, second.TemplateType);
        Assert.Equal(
            first.Columns.Select(column => column.HtmlType),
            second.Columns.Select(column => column.HtmlType));
    }
}

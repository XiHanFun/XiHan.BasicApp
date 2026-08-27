// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// Scriban 模板渲染器测试。
/// </summary>
/// <remarks>
/// 渲染器是模板与上下文之间的唯一契约层，三条约定必须锁死：
/// 变量以 PascalCase 键注入且关闭成员重命名（一旦退回 Scriban 默认的 snake_case，八个内置模板会静默渲染出空）；
/// 八个转义过滤器全部可用（模板据宿主上下文选用，缺一个就是一处注入面）；
/// i18n 键前缀在生成期 fail-closed（前端门禁对连字符与非 ASCII 是静默漏检，这里是唯一拦截点）。
/// 断言方式是用小段模板把变量反渲染出来，不反射私有方法。
/// </remarks>
public sealed class ScribanTemplateRendererTests
{
    private readonly ScribanTemplateRenderer _renderer = new();

    /// <summary>
    /// 渲染器声明的引擎必须是 Scriban，否则解析器按引擎注册会错位。
    /// </summary>
    [Fact]
    public void Engine_ShouldBeScriban()
    {
        Assert.Equal(TemplateEngine.Scriban, _renderer.Engine);
    }

    /// <summary>
    /// 上下文为 null 立即拒绝。
    /// </summary>
    [Fact]
    public async Task RenderAsync_NullContextShouldThrow()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => _renderer.RenderAsync("{{ ClassName }}", null!));
    }

    /// <summary>
    /// 模板源为空时直接返回空串（模板内容未填是常态，不该报错）。
    /// </summary>
    /// <param name="templateSource">空模板源</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RenderAsync_EmptyTemplateShouldReturnEmpty(string? templateSource)
    {
        var result = await _renderer.RenderAsync(templateSource!, CodeGenerationTestHelper.CreateContext());

        Assert.Equal(string.Empty, result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 已取消的令牌必须在解析模板之前抛出，避免白白解析一遍大模板。
    /// </summary>
    [Fact]
    public async Task RenderAsync_CancelledTokenShouldThrowBeforeParsing()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _renderer.RenderAsync("{{ 这是语法错误", CodeGenerationTestHelper.CreateContext(), cts.Token));
    }

    /// <summary>
    /// 模板语法错误必须抛出带原始错误文本的异常，否则用户只看到一句"渲染失败"无从排查。
    /// </summary>
    [Fact]
    public async Task RenderAsync_SyntaxErrorShouldThrowWithParserMessage()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _renderer.RenderAsync("{{ for item in }}{{ end }}", CodeGenerationTestHelper.CreateContext()));

        Assert.Contains("Scriban 模板解析失败", exception.Message, StringComparison.Ordinal);
        Assert.True(exception.Message.Length > "Scriban 模板解析失败：".Length, "必须带出解析器给出的原始错误文本");
    }

    /// <summary>
    /// 表级变量以 PascalCase 键注入且关闭成员重命名：模板写 <c>{{ ClassName }}</c> 就必须取到值。
    /// 这条一旦漂移（Scriban 默认会把成员名转 snake_case），全部内置模板都会渲染出空。
    /// </summary>
    [Fact]
    public async Task RenderAsync_TableLevelVariablesShouldBeInjectedInPascalCase()
    {
        var context = CodeGenerationTestHelper.CreateContext();

        var result = await _renderer.RenderAsync(
            "{{ TableName }}|{{ TableComment }}|{{ ClassName }}|{{ ClassNameCamel }}|{{ ClassNameKebab }}|"
            + "{{ ClassNameSnake }}|{{ ClassNameEn }}|{{ Namespace }}|{{ ModuleName }}|{{ BusinessName }}|"
            + "{{ FunctionName }}|{{ Author }}|{{ TemplateType }}|{{ I18nNamespace }}|{{ I18nPrefix }}",
            context);

        Assert.Equal(
            "sys_product|产品表|SysProduct|sysProduct|sys-product|sys_product|Sys Product|"
            + "XiHan.BasicApp.Catalog|Catalog|产品|产品|tester|Single|catalog|catalog.sys_product",
            result,
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 枚举以名称字符串透出，模板可写 <c>{{ if TemplateType == "Tree" }}</c> 按名比较。
    /// </summary>
    /// <param name="templateType">模板类型</param>
    /// <param name="expected">期望渲染出的名称</param>
    [Theory]
    [InlineData(TemplateType.Single, "Single")]
    [InlineData(TemplateType.Tree, "Tree")]
    [InlineData(TemplateType.MasterDetail, "MasterDetail")]
    public async Task RenderAsync_TemplateTypeShouldBeExposedAsName(TemplateType templateType, string expected)
    {
        var context = CodeGenerationTestHelper.CreateContext(templateType: templateType);

        Assert.Equal(expected, await _renderer.RenderAsync("{{ TemplateType }}", context), StringComparer.Ordinal);
    }

    /// <summary>
    /// 命名空间为空时回退到模块段，模块名为空时回退到类名；
    /// 直插 null 会渲染出 <c>using .Domain.Entities;</c> 与 <c>src/views//x</c> 这类坏产物。
    /// </summary>
    [Fact]
    public async Task RenderAsync_BlankNamespaceAndModuleShouldFallBack()
    {
        var context = CodeGenerationTestHelper.CreateContext(moduleName: "  ", namespaceValue: "  ");

        var result = await _renderer.RenderAsync("{{ Namespace }}|{{ ModuleName }}", context);

        Assert.Equal("SysProductGenerated|SysProduct", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 命名空间为空但模块名非空时，命名空间回退到模块段原值（不加 Generated 后缀）。
    /// </summary>
    [Fact]
    public async Task RenderAsync_BlankNamespaceWithModuleShouldUseModuleSegment()
    {
        var context = CodeGenerationTestHelper.CreateContext(namespaceValue: null);

        var result = await _renderer.RenderAsync("{{ Namespace }}|{{ ModuleName }}", context);

        Assert.Equal("Catalog|Catalog", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// i18n 键前缀不合规时必须在生成期抛出并带出前缀值：
    /// 前端门禁的孤儿扫描对空段/空格是静默漏检，这里是唯一拦截点。
    /// </summary>
    /// <param name="className">会产出不合规前缀的类名</param>
    [Theory]
    [InlineData("")]
    [InlineData("Sys Product")]
    public async Task RenderAsync_InvalidI18nPrefixShouldFailClosed(string className)
    {
        var context = CodeGenerationTestHelper.CreateContext(className: className);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _renderer.RenderAsync("{{ ClassName }}", context));

        Assert.Contains("i18n 键前缀不合规", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 中文模块名不会让前缀不合规：模块段先经 I18nSegment 归一化到哈希键段。
    /// </summary>
    [Fact]
    public async Task RenderAsync_ChineseModuleNameShouldStillProduceValidI18nPrefix()
    {
        var context = CodeGenerationTestHelper.CreateContext(moduleName: "商品中心");

        var result = await _renderer.RenderAsync("{{ I18nPrefix }}", context);

        Assert.Matches(@"^k_[0-9a-f]{8}\.sys_product$", result);
    }

    /// <summary>
    /// 八个转义过滤器必须全部注册且可在模板中以过滤器语法调用，缺一个就是一处未转义的注入面。
    /// </summary>
    [Fact]
    public async Task RenderAsync_AllEightEscapeFiltersShouldBeCallable()
    {
        var context = CodeGenerationTestHelper.CreateContext();

        var result = await _renderer.RenderAsync(
            "{{ '\"' | cs_string }}|{{ '<a>' | xml_doc }}|{{ \"it's\" | ts_string }}|{{ '<a>' | html_attr }}|"
            + "{{ 'a*/b' | block_comment }}|{{ 'a-->b' | html_comment }}|{{ '@' | i18n_message }}|{{ '[1]' | js_literal }}",
            context);

        Assert.Equal(
            "\\\"|&lt;a&gt;|it\\'s|&lt;a&gt;|a* /b|a- ->b|{\\'@\\'}|[1]",
            result,
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 已启用操作同时透出列表与三个便捷布尔，三者内容必须一致（模板两种写法结果不能打架）。
    /// </summary>
    /// <param name="actionsJoined">已启用操作（逗号分隔；空串表示空集）</param>
    /// <param name="expected">期望渲染结果</param>
    [Theory]
    [InlineData("create,update,delete", "create,update,delete|true|true|true")]
    [InlineData("create", "create|true|false|false")]
    [InlineData("update,delete", "update,delete|false|true|true")]
    [InlineData("", "|false|false|false")]
    public async Task RenderAsync_EnabledActionsListAndBooleansShouldAgree(string actionsJoined, string expected)
    {
        string[] actions = actionsJoined.Length == 0 ? [] : actionsJoined.Split(',');
        var context = CodeGenerationTestHelper.CreateContext(enabledActions: actions);

        var result = await _renderer.RenderAsync(
            "{{ EnabledActions | array.join \",\" }}|{{ CanCreate }}|{{ CanUpdate }}|{{ CanDelete }}",
            context);

        Assert.Equal(expected, result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 列级变量以 PascalCase 键注入，且可在 for 循环内逐列访问。
    /// </summary>
    [Fact]
    public async Task RenderAsync_ColumnLevelVariablesShouldBeInjectedInPascalCase()
    {
        var column = CodeGenerationTestHelper.CreateColumn("ProductName");
        var context = CodeGenerationTestHelper.CreateContext(columns: [column]);

        var result = await _renderer.RenderAsync(
            "{{ for col in Columns }}{{ col.ColumnName }}|{{ col.CSharpProperty }}|{{ col.TsProperty }}|"
            + "{{ col.CSharpType }}|{{ col.TsType }}|{{ col.I18nKey }}|{{ col.EnLabel }}|{{ col.HtmlType }}|{{ col.QueryType }}{{ end }}",
            context);

        Assert.Equal(
            "ProductName|ProductName|productName|string|string|product_name|Product Name|Input|Equal",
            result,
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 业务列判据 = 非基类托管列且非主键；<c>InDetail</c> 恒等于业务列判定，
    /// <c>InList/InCreate/InUpdate/InForm</c> 需先过业务列判定再看列开关。
    /// </summary>
    /// <param name="columnName">列名</param>
    /// <param name="isPrimaryKey">是否主键</param>
    /// <param name="expectedBusiness">是否应判为业务列</param>
    [Theory]
    [InlineData("ProductName", false, true)]
    [InlineData("BasicId", false, false)]
    [InlineData("Created_Time", false, false)]
    [InlineData("TenantId", false, false)]
    [InlineData("ProductName", true, false)]
    public async Task RenderAsync_BusinessColumnPredicateShouldGateAllSwitches(string columnName, bool isPrimaryKey, bool expectedBusiness)
    {
        var column = CodeGenerationTestHelper.CreateColumn(columnName, isPrimaryKey: isPrimaryKey);
        column.IsList = true;
        column.IsInsert = true;
        column.IsEdit = true;
        var context = CodeGenerationTestHelper.CreateContext(columns: [column]);

        var result = await _renderer.RenderAsync(
            "{{ for col in Columns }}{{ col.InDetail }}|{{ col.InList }}|{{ col.InCreate }}|{{ col.InUpdate }}|{{ col.InForm }}{{ end }}",
            context);

        var flag = expectedBusiness ? "true" : "false";
        Assert.Equal($"{flag}|{flag}|{flag}|{flag}|{flag}", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 列开关关闭时对应布尔为 false，但 <c>InDetail</c> 仍为 true（详情承载全部业务列）。
    /// </summary>
    [Fact]
    public async Task RenderAsync_ColumnSwitchesOffShouldStillKeepInDetailTrue()
    {
        var column = CodeGenerationTestHelper.CreateColumn("ProductName");
        column.IsList = false;
        column.IsInsert = false;
        column.IsEdit = false;
        var context = CodeGenerationTestHelper.CreateContext(columns: [column]);

        var result = await _renderer.RenderAsync(
            "{{ for col in Columns }}{{ col.InDetail }}|{{ col.InList }}|{{ col.InCreate }}|{{ col.InUpdate }}|{{ col.InForm }}{{ end }}",
            context);

        Assert.Equal("true|false|false|false|false", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 八个模板共用的查询派生布尔口径：
    /// 日期列 = 日期/日期时间控件；可查询 = 业务列 且 IsQuery 且非二进制；
    /// 区间查询 = 可查询 且 Between 且日期列；标量查询 = 可查询 且 非区间 且 QueryType ∈ {Equal, Between}；
    /// 关键字查询 = 可查询 且 Like。漂移会让取数侧与展现侧对不上。
    /// </summary>
    /// <param name="csharpType">C# 类型</param>
    /// <param name="htmlType">表单控件</param>
    /// <param name="queryType">查询方式</param>
    /// <param name="isQuery">列配置是否参与查询</param>
    /// <param name="expected">期望的五个布尔（IsDateColumn|IsQueryable|IsRangeQuery|IsScalarQuery|IsKeywordQuery）</param>
    [Theory]
    [InlineData("string", HtmlType.Input, QueryType.Like, true, "false|true|false|false|true")]
    [InlineData("string", HtmlType.Input, QueryType.Equal, true, "false|true|false|true|false")]
    [InlineData("DateTimeOffset", HtmlType.DateTimePicker, QueryType.Between, true, "true|true|true|false|false")]
    [InlineData("DateTimeOffset", HtmlType.DatePicker, QueryType.Between, true, "true|true|true|false|false")]
    [InlineData("decimal", HtmlType.InputNumber, QueryType.Between, true, "false|true|false|true|false")]
    [InlineData("DateTimeOffset", HtmlType.DateTimePicker, QueryType.Equal, true, "true|true|false|true|false")]
    [InlineData("byte[]", HtmlType.FileUpload, QueryType.Equal, true, "false|false|false|false|false")]
    [InlineData("string", HtmlType.Input, QueryType.Like, false, "false|false|false|false|false")]
    [InlineData("string", HtmlType.Input, QueryType.In, true, "false|true|false|false|false")]
    public async Task RenderAsync_QueryDerivedBooleansShouldFollowSharedPredicate(
        string csharpType, HtmlType htmlType, QueryType queryType, bool isQuery, string expected)
    {
        var column = CodeGenerationTestHelper.CreateColumn(
            "ProductName", csharpType: csharpType, isQuery: isQuery, queryType: queryType, htmlType: htmlType);
        var context = CodeGenerationTestHelper.CreateContext(columns: [column]);

        var result = await _renderer.RenderAsync(
            "{{ for col in Columns }}{{ col.IsDateColumn }}|{{ col.IsQueryable }}|{{ col.IsRangeQuery }}|"
            + "{{ col.IsScalarQuery }}|{{ col.IsKeywordQuery }}{{ end }}",
            context);

        Assert.Equal(expected, result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 基类托管列即使打开查询开关也不可查询（业务列判定在前）。
    /// </summary>
    [Fact]
    public async Task RenderAsync_BaseColumnShouldNeverBeQueryable()
    {
        var column = CodeGenerationTestHelper.CreateColumn("CreatedTime", isQuery: true, queryType: QueryType.Like);
        var context = CodeGenerationTestHelper.CreateContext(columns: [column]);

        var result = await _renderer.RenderAsync(
            "{{ for col in Columns }}{{ col.IsBaseColumn }}|{{ col.IsQueryable }}{{ end }}",
            context);

        Assert.Equal("true|false", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 限定类型名：枚举命名空间为空时等于原类型名，非空时补全为"命名空间.类型"，
    /// 否则产物里的枚举短名找不到定义、编译不过。
    /// </summary>
    [Fact]
    public async Task RenderAsync_QualifiedTypeNameShouldPrependEnumNamespaceWhenPresent()
    {
        var plain = CodeGenerationTestHelper.CreateColumn("ProductName");
        var enumColumn = CodeGenerationTestHelper.CreateColumn("Status", csharpType: "EnableStatus");
        enumColumn.EnumNamespace = "XiHan.BasicApp.Saas.Domain.Enums";
        enumColumn.EnumTypeShortName = "EnableStatus";
        var context = CodeGenerationTestHelper.CreateContext(columns: [plain, enumColumn]);

        var result = await _renderer.RenderAsync(
            "{{ for col in Columns }}{{ col.CSharpTypeQualified }};{{ end }}",
            context);

        Assert.Equal("string;XiHan.BasicApp.Saas.Domain.Enums.EnableStatus;", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 枚举短名不在值类型白名单内，但只要解析出了枚举事实就必须判为值类型，
    /// 否则模板会把枚举列当引用类型处理（可空判定与默认值都会错）。
    /// </summary>
    [Fact]
    public async Task RenderAsync_ResolvedEnumColumnShouldBeTreatedAsValueType()
    {
        var enumColumn = CodeGenerationTestHelper.CreateColumn("Status", csharpType: "EnableStatus");
        enumColumn.EnumTypeShortName = "EnableStatus";
        var unresolved = CodeGenerationTestHelper.CreateColumn("Kind", csharpType: "SomeUnknownEnum");
        var context = CodeGenerationTestHelper.CreateContext(columns: [enumColumn, unresolved]);

        var result = await _renderer.RenderAsync(
            "{{ for col in Columns }}{{ col.IsValueType }};{{ end }}",
            context);

        Assert.Equal("true;false;", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 主键、树表两列、主表为 null 时对应变量必须是 null（模板可用 <c>if</c> 判空），
    /// 而不是空字典让模板误以为有值。
    /// </summary>
    [Fact]
    public async Task RenderAsync_AbsentStructuralColumnsShouldRenderAsNull()
    {
        var context = CodeGenerationTestHelper.CreateContext();

        var result = await _renderer.RenderAsync(
            "{{ if PrimaryKey }}pk{{ else }}no-pk{{ end }}|"
            + "{{ if TreeParentColumn }}tp{{ else }}no-tp{{ end }}|"
            + "{{ if TreeNameColumn }}tn{{ else }}no-tn{{ end }}|"
            + "{{ if MasterTable }}mt{{ else }}no-mt{{ end }}|{{ HasDetailTables }}|{{ DetailTables.size }}",
            context);

        Assert.Equal("no-pk|no-tp|no-tn|no-mt|false|0", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 结构列存在时按列字典透出，树表模板可直接取到父级列与显示名列的属性名。
    /// </summary>
    [Fact]
    public async Task RenderAsync_StructuralColumnsShouldExposeColumnDictionary()
    {
        var pk = CodeGenerationTestHelper.CreateColumn("BasicId", csharpType: "long", isPrimaryKey: true);
        var parent = CodeGenerationTestHelper.CreateColumn("ParentId", csharpType: "long?");
        var name = CodeGenerationTestHelper.CreateColumn("Name");
        var context = CodeGenerationTestHelper.CreateContext(columns: [pk, parent, name], templateType: TemplateType.Tree);
        context.PrimaryKey = pk;
        context.TreeParentColumn = parent;
        context.TreeNameColumn = name;

        var result = await _renderer.RenderAsync(
            "{{ PrimaryKey.CSharpProperty }}|{{ PrimaryKey.CSharpType }}|{{ TreeParentColumn.CSharpProperty }}|{{ TreeNameColumn.CSharpProperty }}",
            context);

        Assert.Equal("BasicId|long|ParentId|Name", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 主子表关联透出主表与子表引用（含外键列），主表整体生成时可遍历子表。
    /// </summary>
    [Fact]
    public async Task RenderAsync_RelatedTablesShouldBeExposed()
    {
        var context = CodeGenerationTestHelper.CreateContext(templateType: TemplateType.MasterDetail);
        context.MasterTable = new RelatedTableRef
        {
            TableId = 12,
            TableName = "sys_order",
            ClassName = "SysOrder",
            ClassNameCamel = "sysOrder",
            ClassNameKebab = "sys-order",
            ForeignKeyColumn = "OrderId",
            ForeignKeyProperty = "OrderId"
        };
        context.DetailTables =
        [
            new RelatedTableRef
            {
                TableId = 34,
                TableName = "sys_order_item",
                ClassName = "SysOrderItem",
                ClassNameCamel = "sysOrderItem",
                ClassNameKebab = "sys-order-item",
                ForeignKeyColumn = "OrderId",
                ForeignKeyProperty = "OrderId"
            }
        ];

        var result = await _renderer.RenderAsync(
            "{{ MasterTable.ClassName }}|{{ MasterTable.TableId }}|{{ MasterTable.ForeignKeyProperty }}|"
            + "{{ HasDetailTables }}|{{ for d in DetailTables }}{{ d.ClassName }}:{{ d.ClassNameKebab }}:{{ d.ClassNameSnake }}{{ end }}",
            context);

        Assert.Equal("SysOrder|12|OrderId|true|SysOrderItem:sys-order-item:sys_order_item", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 扩展选项字典透出给模板，供表配置里的自定义键值参与渲染。
    /// </summary>
    [Fact]
    public async Task RenderAsync_OptionsShouldBeExposed()
    {
        var context = CodeGenerationTestHelper.CreateContext();
        context.Options["ParentMenuId"] = 88L;

        Assert.Equal("88", await _renderer.RenderAsync("{{ Options.ParentMenuId }}", context), StringComparer.Ordinal);
    }

    /// <summary>
    /// 空白模板校验为无效且错误信息明确，避免用户保存了一个空模板还以为可用。
    /// </summary>
    /// <param name="templateSource">空白模板源</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n")]
    public void Validate_BlankTemplateShouldBeInvalid(string? templateSource)
    {
        var validation = _renderer.Validate(templateSource!);

        Assert.False(validation.IsValid);
        Assert.Contains("模板内容为空", Assert.Single(validation.Errors), StringComparison.Ordinal);
    }

    /// <summary>
    /// 语法正确的模板校验通过且错误集合为空。
    /// </summary>
    [Fact]
    public void Validate_ValidTemplateShouldReturnValidWithNoErrors()
    {
        var validation = _renderer.Validate("{{ ClassName }}{{ for col in Columns }}{{ col.ColumnName }}{{ end }}");

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
    }

    /// <summary>
    /// 语法错误的模板校验失败并收集到具体错误消息（非空、可展示给用户）。
    /// </summary>
    [Fact]
    public void Validate_SyntaxErrorShouldCollectMessages()
    {
        var validation = _renderer.Validate("{{ for item in }}{{ end }}");

        Assert.False(validation.IsValid);
        Assert.NotEmpty(validation.Errors);
        Assert.All(validation.Errors, error => Assert.False(string.IsNullOrWhiteSpace(error)));
    }
}

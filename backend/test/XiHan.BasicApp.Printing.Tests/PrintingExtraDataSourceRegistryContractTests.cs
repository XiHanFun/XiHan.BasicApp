// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Printing.Domain.DataSources;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印数据源注册表的编码归一、字段契约边界与内置示例数据源契约测试。
/// </summary>
/// <remarks>
/// 注册表是设计器素材与模板写路径校验的共同事实源，且注册动作发生在应用启动阶段：
/// 这里的每一条拒绝路径都对应「应用起不来」而不是「某个请求失败」，因此边界必须逐条钉死。
/// </remarks>
public sealed class PrintingExtraDataSourceRegistryContractTests
{
    /// <summary>
    /// 注册表支持的全部字段类型。
    /// </summary>
    public static TheoryData<string> SupportedKinds => ["text", "image", "barcode", "qrcode"];

    /// <summary>
    /// 注册表支持的全部样例控件类型。
    /// </summary>
    public static TheoryData<string> SupportedInputTypes => ["boolean", "date", "datetime", "number", "text", "textarea"];

    /// <summary>
    /// 空定义必须以 <see cref="ArgumentNullException"/> 拒绝，而不是留下一个 null 目录项。
    /// </summary>
    [Fact]
    public void Register_NullDefinition_ShouldThrowArgumentNull()
    {
        var registry = new PrintDataSourceRegistry([]);

        _ = Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    /// <summary>
    /// 编码与名称为空、纯空白或 null 时必须立即失败；null 走的是派生的 <see cref="ArgumentNullException"/>。
    /// </summary>
    /// <param name="code">数据源编码。</param>
    /// <param name="name">数据源名称。</param>
    [Theory]
    [InlineData(null, "名称")]
    [InlineData("", "名称")]
    [InlineData("   ", "名称")]
    [InlineData("erp.order", null)]
    [InlineData("erp.order", "")]
    [InlineData("erp.order", "  ")]
    public void Register_BlankCodeOrName_ShouldThrow(string? code, string? name)
    {
        var registry = new PrintDataSourceRegistry([]);
        var definition = new PrintDataSourceDefinition(code!, name!, [new("title", "标题")], "{}");

        _ = Assert.ThrowsAny<ArgumentException>(() => registry.Register(definition));
    }

    /// <summary>
    /// 编码长度上限恰为 100：100 字符放行、101 字符拒绝，避免与实体列长度产生落库截断。
    /// </summary>
    [Fact]
    public void Register_CodeLength_ShouldAcceptExactlyHundredAndRejectMore()
    {
        var registry = new PrintDataSourceRegistry([]);

        registry.Register(new PrintDataSourceDefinition(new string('a', 100), "边界", [new("f", "字段")], "{}"));
        Assert.True(registry.IsRegistered(new string('a', 100)));

        _ = Assert.Throws<ArgumentException>(() => registry.Register(
            new PrintDataSourceDefinition(new string('b', 101), "越界", [new("f", "字段")], "{}")));
    }

    /// <summary>
    /// 字段编码同样以 100 字符为界，越界必须在注册阶段拒绝。
    /// </summary>
    [Fact]
    public void Register_FieldKeyLength_ShouldAcceptExactlyHundredAndRejectMore()
    {
        var registry = new PrintDataSourceRegistry([]);

        registry.Register(new PrintDataSourceDefinition("ok.key", "边界", [new(new string('a', 100), "字段")], "{}"));
        Assert.True(registry.IsRegistered("ok.key"));

        _ = Assert.Throws<ArgumentException>(() => registry.Register(
            new PrintDataSourceDefinition("bad.key-length", "越界", [new(new string('b', 101), "字段")], "{}")));
    }

    /// <summary>
    /// 各类空白字符（空格、制表、换行）出现在编码里都必须拒绝，避免路由与缓存键歧义。
    /// </summary>
    /// <param name="code">带空白字符的数据源编码。</param>
    [Theory]
    [InlineData("erp order")]
    [InlineData("erp\torder")]
    [InlineData("erp\norder")]
    public void Register_CodeWithWhitespace_ShouldThrow(string code)
    {
        var registry = new PrintDataSourceRegistry([]);

        _ = Assert.Throws<ArgumentException>(() => registry.Register(
            new PrintDataSourceDefinition(code, "带空白", [new("f", "字段")], "{}")));
    }

    /// <summary>
    /// 字段清单为空的数据源没有任何设计器素材，必须拒绝。
    /// </summary>
    [Fact]
    public void Register_EmptyFields_ShouldThrow()
    {
        var registry = new PrintDataSourceRegistry([]);

        var exception = Assert.Throws<ArgumentException>(() => registry.Register(
            new PrintDataSourceDefinition("erp.empty", "空字段", [], "{}")));

        Assert.Contains("至少需要一个字段", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 字段标签为空时必须拒绝，设计器素材面板不允许出现无标题项。
    /// </summary>
    [Fact]
    public void Register_BlankFieldLabel_ShouldThrow()
    {
        var registry = new PrintDataSourceRegistry([]);

        _ = Assert.ThrowsAny<ArgumentException>(() => registry.Register(
            new PrintDataSourceDefinition("erp.label", "空标签", [new("f", "  ")], "{}")));
    }

    /// <summary>
    /// 样例数据必须存在：null 或纯空白同样拒绝，设计器预览没有兜底数据可用。
    /// </summary>
    /// <param name="sampleDataJson">样例数据 JSON。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_BlankSampleDataJson_ShouldThrow(string? sampleDataJson)
    {
        var registry = new PrintDataSourceRegistry([]);

        _ = Assert.ThrowsAny<ArgumentException>(() => registry.Register(
            new PrintDataSourceDefinition("erp.sample", "样例", [new("f", "字段")], sampleDataJson!)));
    }

    /// <summary>
    /// 样例数据根节点必须是对象或数组，原始值（字符串、布尔、null 字面量）一律拒绝。
    /// </summary>
    /// <param name="sampleDataJson">根节点为原始值的样例数据。</param>
    [Theory]
    [InlineData("\"text\"")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("-1.5")]
    public void Register_PrimitiveRootSampleData_ShouldThrow(string sampleDataJson)
    {
        var registry = new PrintDataSourceRegistry([]);

        var exception = Assert.Throws<ArgumentException>(() => registry.Register(
            new PrintDataSourceDefinition("erp.root", "原始值根", [new("f", "字段")], sampleDataJson)));

        Assert.Contains("必须是对象或数组", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 全部受支持的字段类型都必须放行，且类型串按序数大小写敏感匹配。
    /// </summary>
    /// <param name="kind">字段类型。</param>
    [Theory]
    [MemberData(nameof(SupportedKinds))]
    public void Register_SupportedKinds_ShouldBeAccepted(string kind)
    {
        var registry = new PrintDataSourceRegistry([]);

        registry.Register(new PrintDataSourceDefinition($"erp.{kind}", kind, [new("f", "字段", kind)], "{}"));

        Assert.True(registry.IsRegistered($"erp.{kind}"));
    }

    /// <summary>
    /// 字段类型串大小写敏感：大写变体必须拒绝，避免设计器拿到无法渲染的元素类型。
    /// </summary>
    /// <param name="kind">大小写不匹配的字段类型。</param>
    [Theory]
    [InlineData("Text")]
    [InlineData("TEXT")]
    [InlineData("Table")]
    public void Register_KindIsCaseSensitive_ShouldReject(string kind)
    {
        var registry = new PrintDataSourceRegistry([]);

        _ = Assert.Throws<ArgumentException>(() => registry.Register(
            new PrintDataSourceDefinition("erp.case", "大小写", [new("f", "字段", kind)], "{}")));
    }

    /// <summary>
    /// 全部受支持的样例控件类型都必须放行，字段级与明细表列级采用同一套白名单。
    /// </summary>
    /// <param name="inputType">样例控件类型。</param>
    [Theory]
    [MemberData(nameof(SupportedInputTypes))]
    public void Register_SupportedInputTypes_ShouldBeAcceptedOnFieldAndColumn(string inputType)
    {
        var registry = new PrintDataSourceRegistry([]);

        registry.Register(new PrintDataSourceDefinition(
            $"erp.input-{inputType}",
            inputType,
            [
                new("f", "字段", "text", null, inputType),
                new("items", "明细", "table", [new("c", "列", 60, inputType)])
            ],
            "{}"));

        Assert.True(registry.IsRegistered($"erp.input-{inputType}"));
    }

    /// <summary>
    /// 明细表列的控件类型同样受白名单约束，非法值必须在注册阶段拒绝。
    /// </summary>
    [Fact]
    public void Register_InvalidColumnInputType_ShouldThrow()
    {
        var registry = new PrintDataSourceRegistry([]);

        var exception = Assert.Throws<ArgumentException>(() => registry.Register(
            new PrintDataSourceDefinition(
                "erp.column-input",
                "列控件",
                [new("items", "明细", "table", [new("c", "列", 60, "richtext")])],
                "{}")));

        Assert.Contains("样例控件类型无效", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 非 table 字段上的列定义当前不参与校验，这是「只有 table 才读 Columns」的直接后果；
    /// 该用例锁定现状，若将来改为一并校验，此处会立刻变红提示同步契约文档。
    /// </summary>
    [Fact]
    public void Register_NonTableFieldWithInvalidColumns_ShouldStillBeAccepted()
    {
        var registry = new PrintDataSourceRegistry([]);

        registry.Register(new PrintDataSourceDefinition(
            "erp.ignored-columns",
            "被忽略的列",
            [new("f", "字段", "text", [new(" ", " ")])],
            "{}"));

        Assert.True(registry.IsRegistered("erp.ignored-columns"));
    }

    /// <summary>
    /// 查找按序数比较且会去掉首尾空白；大小写不同视为不同数据源。
    /// </summary>
    [Fact]
    public void Find_ShouldTrimInputAndStayCaseSensitive()
    {
        var registry = new PrintDataSourceRegistry([]);
        registry.Register(new PrintDataSourceDefinition("erp.order", "订单", [new("f", "字段")], "{}"));

        Assert.NotNull(registry.Find("  erp.order  "));
        Assert.Null(registry.Find("ERP.ORDER"));
        Assert.Null(registry.Find("   "));
        Assert.Null(registry.Find(null!));
        Assert.False(registry.IsRegistered("ERP.ORDER"));
    }

    /// <summary>
    /// 注册失败不得留下半成品：重复编码抛出后目录内容与数量保持不变。
    /// </summary>
    [Fact]
    public void Register_FailedRegistration_ShouldNotMutateCatalog()
    {
        var registry = new PrintDataSourceRegistry([]);
        registry.Register(new PrintDataSourceDefinition("erp.order", "订单", [new("f", "字段")], "{}"));

        _ = Assert.Throws<InvalidOperationException>(() => registry.Register(
            new PrintDataSourceDefinition("erp.order", "订单改", [new("g", "另一个字段")], "{}")));
        _ = Assert.Throws<ArgumentException>(() => registry.Register(
            new PrintDataSourceDefinition("erp.bad", "坏字段", [new("f", "字段", "video")], "{}")));

        var single = Assert.Single(registry.GetAll());
        Assert.Equal("订单", single.Name);
    }

    /// <summary>
    /// DI 登记项中出现重复编码时构造即失败，让应用在启动阶段而不是首个请求上暴露冲突。
    /// </summary>
    [Fact]
    public void Constructor_DuplicateRegistrations_ShouldThrow()
    {
        var definition = new PrintDataSourceDefinition("erp.order", "订单", [new("f", "字段")], "{}");

        _ = Assert.Throws<InvalidOperationException>(() => new PrintDataSourceRegistry(
            [new PrintDataSourceRegistration(definition), new PrintDataSourceRegistration(definition)]));
    }

    /// <summary>
    /// 目录排序按序数进行：大写字母整体排在小写字母之前，前端下拉顺序因此稳定可预期。
    /// </summary>
    [Fact]
    public void GetAll_ShouldSortByOrdinalNotCultureRules()
    {
        var registry = new PrintDataSourceRegistry([]);
        foreach (var code in new[] { "b.two", "A.one", "a.zero", "B.three" })
        {
            registry.Register(new PrintDataSourceDefinition(code, code, [new("f", "字段")], "{}"));
        }

        Assert.Equal(["A.one", "B.three", "a.zero", "b.two"], registry.GetAll().Select(source => source.Code));
    }

    /// <summary>
    /// 内置示例数据源必须自洽：可被注册、字段类型齐全、明细表列完整。
    /// </summary>
    [Fact]
    public void SystemPrintDemo_ShouldSatisfyRegistryContract()
    {
        var registry = new PrintDataSourceRegistry([new PrintDataSourceRegistration(BuiltInPrintDataSources.SystemPrintDemo)]);
        var definition = registry.Find("system.print-demo");

        Assert.NotNull(definition);
        Assert.Equal("system.print-demo", definition.Code);
        Assert.Equal(
            ["title", "documentNo", "customerName", "createdTime", "logo", "barcode", "qrCode", "items"],
            definition.Fields.Select(field => field.Key));

        var table = Assert.Single(definition.Fields, field => field.Kind == "table");
        Assert.NotNull(table.Columns);
        Assert.Equal(["sku", "name", "quantity", "unit"], table.Columns.Select(column => column.Field));
    }

    /// <summary>
    /// 内置示例的样例数据必须是对象根，且顶层键与字段清单一一对应，
    /// 否则设计器预览会出现「素材有字段、样例无数据」的空白元素。
    /// </summary>
    [Fact]
    public void SystemPrintDemo_SampleDataShouldCoverEveryField()
    {
        var definition = BuiltInPrintDataSources.SystemPrintDemo;
        using var document = JsonDocument.Parse(definition.SampleDataJson);

        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        var sampleKeys = document.RootElement.EnumerateObject().Select(property => property.Name).ToList();
        var missing = definition.Fields.Select(field => field.Key).Where(key => !sampleKeys.Contains(key, StringComparer.Ordinal)).ToList();

        Assert.True(missing.Count == 0, $"样例数据缺少字段：{string.Join("、", missing)}");
    }

    /// <summary>
    /// 内置示例明细表的样例行必须给出每一列的值，表格预览不能出现半空行。
    /// </summary>
    [Fact]
    public void SystemPrintDemo_SampleTableRowsShouldCoverEveryColumn()
    {
        var definition = BuiltInPrintDataSources.SystemPrintDemo;
        var table = definition.Fields.Single(field => field.Kind == "table");
        using var document = JsonDocument.Parse(definition.SampleDataJson);
        var rows = document.RootElement.GetProperty(table.Key);

        Assert.Equal(JsonValueKind.Array, rows.ValueKind);
        Assert.True(rows.GetArrayLength() > 0, "明细表样例至少要有一行，否则设计器无法预览表体。");
        foreach (var row in rows.EnumerateArray())
        {
            var missing = table.Columns!
                .Select(column => column.Field)
                .Where(field => !row.TryGetProperty(field, out _))
                .ToList();
            Assert.True(missing.Count == 0, $"样例行缺少列：{string.Join("、", missing)}");
        }
    }
}

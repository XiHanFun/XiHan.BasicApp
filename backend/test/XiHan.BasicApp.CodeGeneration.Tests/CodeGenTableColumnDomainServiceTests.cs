// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 列配置领域服务的不变量测试。
/// </summary>
/// <remarks>
/// 锁定三组约定：
/// <list type="number">
/// <item>字典三分互斥：字典 / 枚举 / 常量三选一，选中哪个就只保留哪个字段、其余强制清空；
/// 选了却没填对应值一律拒绝。三个字段同时有值会让前端不知道该按哪个渲染选项。</item>
/// <item>批量保存与单列更新同口径记录"已人工修改"字段——列配置弹窗只走批量保存，
/// 批量侧若不记录，冻结机制对 UI 等于没生效，同步表结构会冲掉人工配置。</item>
/// <item>批量保存的每一列都必须属于命令声明的那张表，跨表混入直接拒绝。</item>
/// </list>
/// </remarks>
public sealed class CodeGenTableColumnDomainServiceTests
{
    private readonly Mock<ICodeGenTableColumnRepository> _columnRepository = new();
    private readonly CodeGenTableColumnDomainService _service;

    /// <summary>
    /// 构造被测领域服务。
    /// </summary>
    public CodeGenTableColumnDomainServiceTests()
    {
        _columnRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<SysCodeGenTableColumn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysCodeGenTableColumn entity, CancellationToken _) => entity);
        _columnRepository
            .Setup(repository => repository.UpdateRangeAsync(It.IsAny<IEnumerable<SysCodeGenTableColumn>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<SysCodeGenTableColumn> entities, CancellationToken _) =>
                (IReadOnlyList<SysCodeGenTableColumn>)entities.ToList());

        _service = new CodeGenTableColumnDomainService(_columnRepository.Object);
    }

    /// <summary>
    /// 构造一条单列更新命令；默认值刻意与实体默认值一致，便于制造"零变化"基线。
    /// </summary>
    /// <param name="basicId">列主键</param>
    /// <param name="csharpType">C# 类型</param>
    /// <param name="csharpProperty">C# 属性名</param>
    /// <param name="tsType">TypeScript 类型</param>
    /// <param name="isRequired">是否必填</param>
    /// <param name="isList">是否列表显示</param>
    /// <param name="isInsert">是否新增字段</param>
    /// <param name="isEdit">是否编辑字段</param>
    /// <param name="isQuery">是否查询字段</param>
    /// <param name="queryType">查询方式</param>
    /// <param name="htmlType">表单控件</param>
    /// <param name="dictSelectorType">字典选择器类型</param>
    /// <param name="dictCode">字典码</param>
    /// <param name="enumTypeName">枚举类型全名</param>
    /// <param name="constValues">常量项 JSON</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="regexPattern">正则</param>
    /// <param name="validationMessage">验证提示</param>
    /// <param name="sort">排序</param>
    /// <param name="status">状态</param>
    private static CodeGenTableColumnUpdateCommand Command(
        long basicId = 1,
        string? csharpType = null,
        string? csharpProperty = null,
        string? tsType = null,
        bool isRequired = false,
        bool isList = true,
        bool isInsert = true,
        bool isEdit = true,
        bool isQuery = false,
        QueryType queryType = QueryType.Equal,
        HtmlType htmlType = HtmlType.Input,
        DictSelectorType? dictSelectorType = null,
        string? dictCode = null,
        string? enumTypeName = null,
        string? constValues = null,
        string? defaultValue = null,
        string? regexPattern = null,
        string? validationMessage = null,
        int sort = 0,
        EnableStatus status = EnableStatus.Enabled)
    {
        return new CodeGenTableColumnUpdateCommand(
            basicId,
            csharpType,
            csharpProperty,
            tsType,
            isRequired,
            isList,
            isInsert,
            isEdit,
            isQuery,
            queryType,
            htmlType,
            dictSelectorType,
            dictCode,
            enumTypeName,
            constValues,
            defaultValue,
            regexPattern,
            validationMessage,
            sort,
            status);
    }

    /// <summary>
    /// 构造一条已落库的列配置。
    /// </summary>
    /// <param name="id">列主键</param>
    /// <param name="tableId">所属表主键</param>
    private static SysCodeGenTableColumn ExistingColumn(long id = 1, long tableId = 100)
    {
        return CodeGenerationTestHelper.WithId(
            new SysCodeGenTableColumn
            {
                TableId = tableId,
                ColumnName = "product_name",
                ColumnComment = "产品名"
            },
            id);
    }

    /// <summary>
    /// 把列配置挂到仓储上。
    /// </summary>
    /// <param name="columns">列配置集合</param>
    private void GivenColumns(params SysCodeGenTableColumn[] columns)
    {
        foreach (var column in columns)
        {
            _columnRepository
                .Setup(repository => repository.GetByIdAsync(column.BasicId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(column);
        }
    }

    /// <summary>
    /// 单列更新命令为空必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_NullCommandShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateColumnAsync(null!));
    }

    /// <summary>
    /// 单列更新的列主键必须大于 0。
    /// </summary>
    /// <param name="basicId">非法主键</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task UpdateColumnAsync_NonPositiveIdShouldThrow(long basicId)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateColumnAsync(Command(basicId: basicId)));

        Assert.Contains("列配置主键必须大于 0。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 列配置不存在必须报"不存在"。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_MissingColumnShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateColumnAsync(Command()));

        Assert.Equal("列配置不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 单列更新时令牌已取消必须在触库前抛出。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_CancelledTokenShouldThrowBeforeRepository()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.UpdateColumnAsync(Command(), cts.Token));

        _columnRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 查询方式取未定义枚举值必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_UndefinedQueryTypeShouldThrow()
    {
        GivenColumns(ExistingColumn());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateColumnAsync(Command(queryType: (QueryType)77)));

        Assert.Contains("查询方式无效。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 表单显示类型取未定义枚举值必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_UndefinedHtmlTypeShouldThrow()
    {
        GivenColumns(ExistingColumn());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateColumnAsync(Command(htmlType: (HtmlType)77)));

        Assert.Contains("表单显示类型无效。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 状态取未定义枚举值必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_UndefinedStatusShouldThrow()
    {
        GivenColumns(ExistingColumn());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateColumnAsync(Command(status: (EnableStatus)77)));

        Assert.Contains("状态无效。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 各可空文本字段超长必须拒绝，并带出对应提示词。
    /// </summary>
    /// <param name="fieldName">被撑爆的字段</param>
    /// <param name="maxLength">该字段上限</param>
    /// <param name="message">期望的提示词</param>
    [Theory]
    [InlineData("CSharpType", 100, "C# 类型最长 100 个字符。")]
    [InlineData("CSharpProperty", 200, "C# 属性名最长 200 个字符。")]
    [InlineData("TsType", 100, "TypeScript 类型最长 100 个字符。")]
    [InlineData("DefaultValue", 500, "默认值最长 500 个字符。")]
    [InlineData("RegexPattern", 500, "正则表达式最长 500 个字符。")]
    [InlineData("ValidationMessage", 500, "验证提示信息最长 500 个字符。")]
    public async Task UpdateColumnAsync_TooLongTextFieldShouldThrow(string fieldName, int maxLength, string message)
    {
        GivenColumns(ExistingColumn());
        var tooLong = new string('x', maxLength + 1);
        var command = fieldName switch
        {
            "CSharpType" => Command(csharpType: tooLong),
            "CSharpProperty" => Command(csharpProperty: tooLong),
            "TsType" => Command(tsType: tooLong),
            "DefaultValue" => Command(defaultValue: tooLong),
            "RegexPattern" => Command(regexPattern: tooLong),
            _ => Command(validationMessage: tooLong)
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateColumnAsync(command));

        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 可空文本字段恰好达到上限属于边界内，必须放行。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_TextFieldAtMaxLengthShouldPass()
    {
        GivenColumns(ExistingColumn());
        var atLimit = new string('x', 100);

        var result = await _service.UpdateColumnAsync(Command(csharpType: atLimit));

        Assert.Equal(atLimit, result.Column.CSharpType, StringComparer.Ordinal);
    }

    /// <summary>
    /// 空白文本字段归 null，并在两端去空格。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_BlankTextShouldBecomeNullAndValuesShouldBeTrimmed()
    {
        GivenColumns(ExistingColumn());

        var result = await _service.UpdateColumnAsync(Command(csharpType: "   ", csharpProperty: "  ProductName  "));

        Assert.Null(result.Column.CSharpType);
        Assert.Equal("ProductName", result.Column.CSharpProperty, StringComparer.Ordinal);
    }

    /// <summary>
    /// 未选字典选择器时，字典/枚举/常量三个字段必须一并清空。
    /// </summary>
    /// <remarks>
    /// 从"枚举下拉"改回"普通输入"后若残留 EnumTypeName，生成的表单仍会去取枚举选项。
    /// </remarks>
    [Fact]
    public async Task UpdateColumnAsync_NullSelectorShouldClearAllSelectorFields()
    {
        var column = ExistingColumn();
        column.DictSelectorType = DictSelectorType.EnumSelector;
        column.DictCode = "sys_status";
        column.EnumTypeName = "Some.Enum";
        column.ConstValues = "[]";
        GivenColumns(column);

        var result = await _service.UpdateColumnAsync(Command(dictSelectorType: null));

        Assert.Null(result.Column.DictSelectorType);
        Assert.Null(result.Column.DictCode);
        Assert.Null(result.Column.EnumTypeName);
        Assert.Null(result.Column.ConstValues);
    }

    /// <summary>
    /// 选系统字典时只保留字典码，枚举与常量必须清空。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_DictSelectorShouldKeepOnlyDictCode()
    {
        var column = ExistingColumn();
        column.EnumTypeName = "Some.Enum";
        column.ConstValues = "[]";
        GivenColumns(column);

        var result = await _service.UpdateColumnAsync(Command(
            dictSelectorType: DictSelectorType.DictSelector,
            dictCode: "  sys_status  ",
            enumTypeName: "Some.Other.Enum"));

        Assert.Equal(DictSelectorType.DictSelector, result.Column.DictSelectorType);
        Assert.Equal("sys_status", result.Column.DictCode, StringComparer.Ordinal);
        Assert.Null(result.Column.EnumTypeName);
        Assert.Null(result.Column.ConstValues);
    }

    /// <summary>
    /// 选枚举时只保留枚举全名，字典码与常量必须清空。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_EnumSelectorShouldKeepOnlyEnumTypeName()
    {
        var column = ExistingColumn();
        column.DictCode = "sys_status";
        GivenColumns(column);

        var result = await _service.UpdateColumnAsync(Command(
            dictSelectorType: DictSelectorType.EnumSelector,
            dictCode: "sys_status",
            enumTypeName: "XiHan.BasicApp.Saas.Domain.Enums.EnableStatus"));

        Assert.Equal(DictSelectorType.EnumSelector, result.Column.DictSelectorType);
        Assert.Equal("XiHan.BasicApp.Saas.Domain.Enums.EnableStatus", result.Column.EnumTypeName, StringComparer.Ordinal);
        Assert.Null(result.Column.DictCode);
        Assert.Null(result.Column.ConstValues);
    }

    /// <summary>
    /// 选常量时只保留常量 JSON，字典码与枚举全名必须清空。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_ConstSelectorShouldKeepOnlyConstValues()
    {
        var column = ExistingColumn();
        column.DictCode = "sys_status";
        column.EnumTypeName = "Some.Enum";
        GivenColumns(column);

        var result = await _service.UpdateColumnAsync(Command(
            dictSelectorType: DictSelectorType.ConstSelector,
            dictCode: "sys_status",
            enumTypeName: "Some.Enum",
            constValues: "  [{\"value\":1,\"label\":\"启用\"}]  "));

        Assert.Equal(DictSelectorType.ConstSelector, result.Column.DictSelectorType);
        Assert.Equal("[{\"value\":1,\"label\":\"启用\"}]", result.Column.ConstValues, StringComparer.Ordinal);
        Assert.Null(result.Column.DictCode);
        Assert.Null(result.Column.EnumTypeName);
    }

    /// <summary>
    /// 选了系统字典却没填字典码必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_DictSelectorWithoutDictCodeShouldThrow()
    {
        GivenColumns(ExistingColumn());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateColumnAsync(Command(dictSelectorType: DictSelectorType.DictSelector, dictCode: "  ")));

        Assert.Equal("系统字典选择器必须填写字典码。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 选了枚举却没填枚举全名必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_EnumSelectorWithoutEnumTypeNameShouldThrow()
    {
        GivenColumns(ExistingColumn());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateColumnAsync(Command(dictSelectorType: DictSelectorType.EnumSelector)));

        Assert.Equal("枚举选择器必须填写枚举类型全名。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 选了常量却没填常量 JSON 必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_ConstSelectorWithoutConstValuesShouldThrow()
    {
        GivenColumns(ExistingColumn());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateColumnAsync(Command(dictSelectorType: DictSelectorType.ConstSelector)));

        Assert.Equal("常量选择器必须填写常量项 JSON。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 常量项不是合法 JSON 必须拒绝——落库的脏 JSON 要到前端渲染时才炸。
    /// </summary>
    /// <param name="constValues">非法 JSON</param>
    [Theory]
    [InlineData("[{value:1}]")]
    [InlineData("not-json")]
    [InlineData("{\"a\":")]
    public async Task UpdateColumnAsync_InvalidConstValuesJsonShouldThrow(string constValues)
    {
        GivenColumns(ExistingColumn());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateColumnAsync(Command(dictSelectorType: DictSelectorType.ConstSelector, constValues: constValues)));

        Assert.Equal("常量项必须是合法 JSON。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 常量 JSON 的合法性在分支之前统一校验：即使选的是系统字典，带上非法常量 JSON 也会被拒。
    /// </summary>
    /// <remarks>写成回归用例是为了让"把校验挪进 ConstSelector 分支"这类改动能被立刻发现。</remarks>
    [Fact]
    public async Task UpdateColumnAsync_InvalidConstValuesShouldBeRejectedEvenForDictSelector()
    {
        GivenColumns(ExistingColumn());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateColumnAsync(Command(
                dictSelectorType: DictSelectorType.DictSelector,
                dictCode: "sys_status",
                constValues: "not-json")));

        Assert.Equal("常量项必须是合法 JSON。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 字典选择器类型取未定义枚举值必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_UndefinedDictSelectorTypeShouldThrow()
    {
        GivenColumns(ExistingColumn());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateColumnAsync(Command(dictSelectorType: (DictSelectorType)66)));

        Assert.Contains("字典选择器类型无效。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 字典码超长必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_TooLongDictCodeShouldThrow()
    {
        GivenColumns(ExistingColumn());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateColumnAsync(Command(
                dictSelectorType: DictSelectorType.DictSelector,
                dictCode: new string('d', 201))));

        Assert.Contains("字典码最长 200 个字符。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 枚举全名超长必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_TooLongEnumTypeNameShouldThrow()
    {
        GivenColumns(ExistingColumn());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateColumnAsync(Command(
                dictSelectorType: DictSelectorType.EnumSelector,
                enumTypeName: new string('e', 501))));

        Assert.Contains("枚举类型全名最长 500 个字符。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 值未变化时不得往"已人工修改"集合里塞字段。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_NoValueChangeShouldKeepUserModifiedFieldsNull()
    {
        GivenColumns(ExistingColumn());

        var result = await _service.UpdateColumnAsync(Command());

        Assert.Null(result.Column.UserModifiedFields);
    }

    /// <summary>
    /// 被跟踪字段变化必须并入"已人工修改"集合，供同步表结构时冻结。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_ChangedTrackedFieldsShouldBeRecorded()
    {
        GivenColumns(ExistingColumn());

        var result = await _service.UpdateColumnAsync(Command(htmlType: HtmlType.Textarea, isQuery: true, sort: 9));

        var recorded = UserModifiedFieldSet.Parse(result.Column.UserModifiedFields);
        Assert.Contains(nameof(SysCodeGenTableColumn.HtmlType), recorded);
        Assert.Contains(nameof(SysCodeGenTableColumn.IsQuery), recorded);
        Assert.Contains(nameof(SysCodeGenTableColumn.Sort), recorded);
    }

    /// <summary>
    /// 未纳入跟踪的字段（默认值/正则/提示/状态）即使变化也不得进入"已人工修改"。
    /// </summary>
    [Fact]
    public async Task UpdateColumnAsync_UntrackedFieldChangeShouldNotBeRecorded()
    {
        GivenColumns(ExistingColumn());

        var result = await _service.UpdateColumnAsync(Command(
            defaultValue: "0",
            regexPattern: "^\\d+$",
            validationMessage: "只能是数字",
            status: EnableStatus.Disabled));

        Assert.Null(result.Column.UserModifiedFields);
        Assert.Equal("0", result.Column.DefaultValue, StringComparer.Ordinal);
        Assert.Equal(EnableStatus.Disabled, result.Column.Status);
    }

    /// <summary>
    /// 批量保存命令为空必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task BatchSaveColumnsAsync_NullCommandShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.BatchSaveColumnsAsync(null!));
    }

    /// <summary>
    /// 批量保存的所属表主键必须大于 0。
    /// </summary>
    /// <param name="tableId">非法表主键</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task BatchSaveColumnsAsync_NonPositiveTableIdShouldThrow(long tableId)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BatchSaveColumnsAsync(new CodeGenTableColumnBatchSaveCommand(tableId, [Command()])));

        Assert.Contains("所属表主键必须大于 0。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 空列集合直接返回空结果，不得触库。
    /// </summary>
    [Fact]
    public async Task BatchSaveColumnsAsync_EmptyColumnsShouldReturnEmptyWithoutRepository()
    {
        var result = await _service.BatchSaveColumnsAsync(new CodeGenTableColumnBatchSaveCommand(100, []));

        Assert.Empty(result.Columns);
        _columnRepository.Verify(
            repository => repository.UpdateRangeAsync(It.IsAny<IEnumerable<SysCodeGenTableColumn>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 列集合为 null 时同样返回空结果，不得空引用。
    /// </summary>
    [Fact]
    public async Task BatchSaveColumnsAsync_NullColumnsShouldReturnEmpty()
    {
        var result = await _service.BatchSaveColumnsAsync(new CodeGenTableColumnBatchSaveCommand(100, null!));

        Assert.Empty(result.Columns);
    }

    /// <summary>
    /// 批量保存中混入其它表的列必须整批拒绝。
    /// </summary>
    [Fact]
    public async Task BatchSaveColumnsAsync_ForeignColumnShouldThrow()
    {
        GivenColumns(ExistingColumn(id: 1, tableId: 100), ExistingColumn(id: 2, tableId: 999));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.BatchSaveColumnsAsync(
                new CodeGenTableColumnBatchSaveCommand(100, [Command(basicId: 1), Command(basicId: 2)])));

        Assert.Equal("批量保存的列配置必须属于同一张表。", exception.Message, StringComparer.Ordinal);
        _columnRepository.Verify(
            repository => repository.UpdateRangeAsync(It.IsAny<IEnumerable<SysCodeGenTableColumn>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 批量保存中某一列不存在必须整批拒绝。
    /// </summary>
    [Fact]
    public async Task BatchSaveColumnsAsync_MissingColumnShouldThrow()
    {
        GivenColumns(ExistingColumn(id: 1, tableId: 100));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.BatchSaveColumnsAsync(
                new CodeGenTableColumnBatchSaveCommand(100, [Command(basicId: 1), Command(basicId: 2)])));

        Assert.Equal("列配置不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 批量保存必须与单列更新同口径记录"已人工修改"字段。
    /// </summary>
    /// <remarks>
    /// 列配置弹窗只走批量保存，这里不记录就等于冻结机制对 UI 从未生效，
    /// 同步表结构会把人工改过的控件类型整批冲掉。
    /// </remarks>
    [Fact]
    public async Task BatchSaveColumnsAsync_ShouldRecordUserModifiedFieldsLikeSingleUpdate()
    {
        var first = ExistingColumn(id: 1, tableId: 100);
        var second = ExistingColumn(id: 2, tableId: 100);
        GivenColumns(first, second);

        var result = await _service.BatchSaveColumnsAsync(new CodeGenTableColumnBatchSaveCommand(
            100,
            [Command(basicId: 1, htmlType: HtmlType.Textarea), Command(basicId: 2)]));

        Assert.Equal(2, result.Columns.Count);
        Assert.Contains(nameof(SysCodeGenTableColumn.HtmlType), UserModifiedFieldSet.Parse(first.UserModifiedFields));
        Assert.Null(second.UserModifiedFields);
    }

    /// <summary>
    /// 批量保存中的空列命令必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task BatchSaveColumnsAsync_NullColumnCommandShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.BatchSaveColumnsAsync(new CodeGenTableColumnBatchSaveCommand(100, [null!])));
    }

    /// <summary>
    /// 批量保存时令牌已取消必须在触库前抛出。
    /// </summary>
    [Fact]
    public async Task BatchSaveColumnsAsync_CancelledTokenShouldThrowBeforeRepository()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.BatchSaveColumnsAsync(new CodeGenTableColumnBatchSaveCommand(100, [Command()]), cts.Token));

        _columnRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

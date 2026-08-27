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
/// 表配置领域服务的不变量测试。
/// </summary>
/// <remarks>
/// 三条核心约定：
/// <list type="number">
/// <item>表配置的模板类型必须是单表/树表/主子表之一——<c>Universal</c> 是模板侧「适用全部类型」的取值，
/// 表若取到它，生成时按类型选模板会选出一个语义矛盾的集合。</item>
/// <item>表名全局唯一且排除自身，否则同一张目标表会有两份互相覆盖的配置。</item>
/// <item>更新走 dirty-tracking：值发生变化的字段并入"已人工修改"集合，供同步表结构时冻结；
/// 集合外的字段才允许被重新推断覆盖。只有 <c>TrackedTableFields</c> 里的字段参与，
/// 结构性字段（表名/主键列/数据源等）始终以库为准，不进集合。</item>
/// </list>
/// </remarks>
public sealed class CodeGenTableDomainServiceTests
{
    private readonly Mock<ICodeGenTableRepository> _tableRepository = new();
    private readonly Mock<ICodeGenTableColumnRepository> _columnRepository = new();
    private readonly CodeGenTableDomainService _service;

    /// <summary>
    /// 构造被测领域服务。
    /// </summary>
    public CodeGenTableDomainServiceTests()
    {
        _tableRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<SysCodeGenTable>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysCodeGenTable entity, CancellationToken _) => entity);
        _tableRepository
            .Setup(repository => repository.DeleteAsync(It.IsAny<SysCodeGenTable>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _service = new CodeGenTableDomainService(_tableRepository.Object, _columnRepository.Object);
    }

    /// <summary>
    /// 构造一条更新命令。
    /// </summary>
    /// <param name="basicId">主键</param>
    /// <param name="tableName">数据库表名</param>
    /// <param name="className">实体类名</param>
    /// <param name="templateType">模板类型</param>
    /// <param name="genType">生成方式</param>
    /// <param name="generationScope">生成范围</param>
    /// <param name="databaseType">数据库类型</param>
    /// <param name="status">状态</param>
    /// <param name="moduleName">模块名</param>
    /// <param name="remark">备注</param>
    private static CodeGenTableUpdateCommand UpdateCommand(
        long basicId = 1,
        string tableName = "sys_product",
        string className = "SysProduct",
        TemplateType templateType = TemplateType.Single,
        GenType genType = GenType.Zip,
        GenerationScope generationScope = GenerationScope.All,
        DatabaseType databaseType = DatabaseType.MySql,
        EnableStatus status = EnableStatus.Enabled,
        string? moduleName = "Catalog",
        string? remark = null)
    {
        return new CodeGenTableUpdateCommand(
            basicId,
            tableName,
            "产品表",
            className,
            "XiHan.BasicApp.Catalog",
            moduleName,
            "产品",
            "产品",
            "tester",
            templateType,
            genType,
            generationScope,
            "create,update",
            null,
            null,
            "BasicId",
            null,
            null,
            null,
            null,
            databaseType,
            null,
            null,
            status,
            remark);
    }

    /// <summary>
    /// 构造一条已落库的表配置实体（字段与默认更新命令一致，便于单独制造某一个字段的差异）。
    /// </summary>
    /// <param name="id">主键</param>
    private static SysCodeGenTable Existing(long id = 1)
    {
        return CodeGenerationTestHelper.WithId(
            new SysCodeGenTable
            {
                TableName = "sys_product",
                TableComment = "产品表",
                ClassName = "SysProduct",
                Namespace = "XiHan.BasicApp.Catalog",
                ModuleName = "Catalog",
                BusinessName = "产品",
                FunctionName = "产品",
                Author = "tester",
                TemplateType = TemplateType.Single,
                GenType = GenType.Zip,
                GenerationScope = GenerationScope.All,
                EnabledActions = "create,update",
                PrimaryKeyColumn = "BasicId",
                DatabaseType = DatabaseType.MySql,
                Status = EnableStatus.Enabled,
                Remark = "原备注"
            },
            id);
    }

    /// <summary>
    /// 把已存在的表配置挂到仓储上。
    /// </summary>
    /// <param name="table">表配置实体</param>
    private void GivenExisting(SysCodeGenTable table)
    {
        _tableRepository
            .Setup(repository => repository.GetByIdAsync(table.BasicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
    }

    /// <summary>
    /// 更新时命令为空必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_NullCommandShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateTableAsync(null!));
    }

    /// <summary>
    /// 更新时令牌已取消必须在触库前抛出。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_CancelledTokenShouldThrowBeforeRepository()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.UpdateTableAsync(UpdateCommand(), cts.Token));

        _tableRepository.Verify(
            repository => repository.ExistsTableNameAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 主键必须大于 0。
    /// </summary>
    /// <param name="basicId">非法主键</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateTableAsync_NonPositiveIdShouldThrow(long basicId)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateTableAsync(UpdateCommand(basicId: basicId)));

        Assert.Contains("表配置主键必须大于 0。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 表配置的模板类型取「通用」必须被拒绝——它只属于模板侧。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_UniversalTemplateTypeShouldBeRejected()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateTableAsync(UpdateCommand(templateType: TemplateType.Universal)));

        Assert.Contains("表配置的模板类型必须是单表/树表/主子表之一", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 三种具体模板类型都必须被接受。
    /// </summary>
    /// <param name="templateType">具体模板类型</param>
    [Theory]
    [InlineData(TemplateType.Single)]
    [InlineData(TemplateType.Tree)]
    [InlineData(TemplateType.MasterDetail)]
    public async Task UpdateTableAsync_ConcreteTemplateTypesShouldBeAccepted(TemplateType templateType)
    {
        GivenExisting(Existing());

        var result = await _service.UpdateTableAsync(UpdateCommand(templateType: templateType));

        Assert.Equal(templateType, result.Table.TemplateType);
    }

    /// <summary>
    /// 各枚举字段取未定义值都必须拒绝，并把参数名带出来。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_UndefinedTemplateTypeShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateTableAsync(UpdateCommand(templateType: (TemplateType)88)));

        Assert.Equal("TemplateType", exception.ParamName);
    }

    /// <summary>
    /// 生成方式取未定义值必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_UndefinedGenTypeShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateTableAsync(UpdateCommand(genType: (GenType)88)));

        Assert.Equal("GenType", exception.ParamName);
    }

    /// <summary>
    /// 生成范围取未定义值必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_UndefinedGenerationScopeShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateTableAsync(UpdateCommand(generationScope: (GenerationScope)88)));

        Assert.Equal("GenerationScope", exception.ParamName);
    }

    /// <summary>
    /// 数据库类型取未定义值必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_UndefinedDatabaseTypeShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateTableAsync(UpdateCommand(databaseType: (DatabaseType)88)));

        Assert.Equal("DatabaseType", exception.ParamName);
    }

    /// <summary>
    /// 状态取未定义值必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_UndefinedStatusShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateTableAsync(UpdateCommand(status: (EnableStatus)88)));

        Assert.Equal("Status", exception.ParamName);
    }

    /// <summary>
    /// 表名与类名为空必须拒绝。
    /// </summary>
    /// <param name="blank">空白入参</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task UpdateTableAsync_BlankTableNameShouldThrow(string? blank)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateTableAsync(UpdateCommand(tableName: blank!)));

        Assert.Equal("TableName", exception.ParamName);
    }

    /// <summary>
    /// 类名为空必须拒绝。
    /// </summary>
    /// <param name="blank">空白入参</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task UpdateTableAsync_BlankClassNameShouldThrow(string? blank)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateTableAsync(UpdateCommand(className: blank!)));

        Assert.Equal("ClassName", exception.ParamName);
    }

    /// <summary>
    /// 表名超过 200 字必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_TooLongTableNameShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateTableAsync(UpdateCommand(tableName: new string('t', 201))));

        Assert.Equal("TableName", exception.ParamName);
    }

    /// <summary>
    /// 模块名超过 100 字必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_TooLongModuleNameShouldThrow()
    {
        GivenExisting(Existing());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateTableAsync(UpdateCommand(moduleName: new string('m', 101))));

        Assert.Equal("ModuleName", exception.ParamName);
    }

    /// <summary>
    /// 表名重复必须拒绝，且唯一性判定要排除自身。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_DuplicateTableNameShouldThrowAndExcludeSelf()
    {
        _tableRepository
            .Setup(repository => repository.ExistsTableNameAsync("sys_product", 1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateTableAsync(UpdateCommand()));

        Assert.Equal("数据库表名已配置。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新不存在的表配置必须报"不存在"。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_MissingTableShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateTableAsync(UpdateCommand()));

        Assert.Equal("表配置不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 表名与类名两端空白必须被裁掉后落库。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_ShouldTrimRequiredFields()
    {
        GivenExisting(Existing());
        _tableRepository
            .Setup(repository => repository.ExistsTableNameAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.UpdateTableAsync(UpdateCommand(tableName: "  sys_order  ", className: "  SysOrder  "));

        Assert.Equal("sys_order", result.Table.TableName, StringComparer.Ordinal);
        Assert.Equal("SysOrder", result.Table.ClassName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 可选字段留空必须归 null，而不是空串。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_BlankOptionalFieldsShouldBecomeNull()
    {
        GivenExisting(Existing());

        var result = await _service.UpdateTableAsync(UpdateCommand(moduleName: "   ", remark: "   "));

        Assert.Null(result.Table.ModuleName);
        Assert.Null(result.Table.Remark);
    }

    /// <summary>
    /// 未发生变化时不得往"已人工修改"集合里塞字段。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_NoValueChangeShouldKeepUserModifiedFieldsNull()
    {
        var existing = Existing();
        GivenExisting(existing);

        var result = await _service.UpdateTableAsync(UpdateCommand());

        Assert.Null(result.Table.UserModifiedFields);
    }

    /// <summary>
    /// 被跟踪字段发生变化时必须并入"已人工修改"集合。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_ChangedTrackedFieldShouldBeRecorded()
    {
        var existing = Existing();
        GivenExisting(existing);

        var result = await _service.UpdateTableAsync(UpdateCommand(className: "SysGoods"));

        var recorded = UserModifiedFieldSet.Parse(result.Table.UserModifiedFields);
        Assert.Contains(nameof(SysCodeGenTable.ClassName), recorded);
    }

    /// <summary>
    /// 结构性字段（如表名）不在跟踪集合内：改了也不得进入"已人工修改"。
    /// </summary>
    /// <remarks>
    /// 表名跟随数据库结构，同步表结构时必须能被刷新；若被冻结，改名后的表将永远同步不上。
    /// </remarks>
    [Fact]
    public async Task UpdateTableAsync_ChangedStructuralFieldShouldNotBeRecorded()
    {
        var existing = Existing();
        GivenExisting(existing);

        var result = await _service.UpdateTableAsync(UpdateCommand(tableName: "sys_goods"));

        var recorded = UserModifiedFieldSet.Parse(result.Table.UserModifiedFields);
        Assert.DoesNotContain(nameof(SysCodeGenTable.TableName), recorded);
    }

    /// <summary>
    /// 已有的"已人工修改"记录必须与本次变化合并，而不是被覆盖。
    /// </summary>
    [Fact]
    public async Task UpdateTableAsync_ExistingUserModifiedFieldsShouldBeMergedNotReplaced()
    {
        var existing = Existing();
        existing.UserModifiedFields = "[\"Author\"]";
        GivenExisting(existing);

        var result = await _service.UpdateTableAsync(UpdateCommand(className: "SysGoods"));

        var recorded = UserModifiedFieldSet.Parse(result.Table.UserModifiedFields);
        Assert.Contains(nameof(SysCodeGenTable.Author), recorded);
        Assert.Contains(nameof(SysCodeGenTable.ClassName), recorded);
    }

    /// <summary>
    /// 状态变更只改状态与备注，其余字段一律不动。
    /// </summary>
    [Fact]
    public async Task UpdateTableStatusAsync_ShouldOnlyTouchStatusAndRemark()
    {
        var existing = Existing();
        GivenExisting(existing);

        var result = await _service.UpdateTableStatusAsync(
            new CodeGenTableStatusChangeCommand(1, EnableStatus.Disabled, "停用理由"));

        Assert.Equal(EnableStatus.Disabled, result.Table.Status);
        Assert.Equal("停用理由", result.Table.Remark, StringComparer.Ordinal);
        Assert.Equal("SysProduct", result.Table.ClassName, StringComparer.Ordinal);
        Assert.Equal("sys_product", result.Table.TableName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态变更时空白备注会把原备注清空（当前实现的真实口径，与数据源/模板的"留空即不改"不同）。
    /// </summary>
    [Fact]
    public async Task UpdateTableStatusAsync_BlankRemarkShouldClearExistingRemark()
    {
        var existing = Existing();
        GivenExisting(existing);

        var result = await _service.UpdateTableStatusAsync(
            new CodeGenTableStatusChangeCommand(1, EnableStatus.Disabled, "   "));

        Assert.Null(result.Table.Remark);
    }

    /// <summary>
    /// 状态变更时状态取未定义值必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTableStatusAsync_UndefinedStatusShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateTableStatusAsync(new CodeGenTableStatusChangeCommand(1, (EnableStatus)9, null)));

        Assert.Equal("Status", exception.ParamName);
    }

    /// <summary>
    /// 状态变更命令为空必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTableStatusAsync_NullCommandShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateTableStatusAsync(null!));
    }

    /// <summary>
    /// 删除表配置必须先级联软删列配置，再删表本身。
    /// </summary>
    /// <remarks>
    /// 顺序反了会留下一批孤儿列配置——表已不可见，列却还挂在原 TableId 上。
    /// </remarks>
    [Fact]
    public async Task DeleteTableAsync_ShouldCascadeColumnsBeforeDeletingTable()
    {
        var existing = Existing(id: 6);
        GivenExisting(existing);
        var sequence = new List<string>();
        _columnRepository
            .Setup(repository => repository.DeleteByTableIdAsync(6L, It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("columns"))
            .Returns(Task.CompletedTask);
        _tableRepository
            .Setup(repository => repository.DeleteAsync(existing, It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("table"))
            .ReturnsAsync(true);

        await _service.DeleteTableAsync(6);

        Assert.Equal(["columns", "table"], sequence);
    }

    /// <summary>
    /// 删除不存在的表配置必须报"不存在"。
    /// </summary>
    [Fact]
    public async Task DeleteTableAsync_MissingTableShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteTableAsync(1));

        Assert.Equal("表配置不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 仓储删除返回 false 时必须抛出。
    /// </summary>
    [Fact]
    public async Task DeleteTableAsync_RepositoryFailureShouldThrow()
    {
        GivenExisting(Existing());
        _tableRepository
            .Setup(repository => repository.DeleteAsync(It.IsAny<SysCodeGenTable>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteTableAsync(1));

        Assert.Equal("表配置删除失败。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 删除的主键必须大于 0。
    /// </summary>
    [Fact]
    public async Task DeleteTableAsync_NonPositiveIdShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.DeleteTableAsync(0));
    }

    /// <summary>
    /// 删除时令牌已取消必须在触库前抛出。
    /// </summary>
    [Fact]
    public async Task DeleteTableAsync_CancelledTokenShouldThrowBeforeRepository()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.DeleteTableAsync(1, cts.Token));

        _columnRepository.Verify(
            repository => repository.DeleteByTableIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

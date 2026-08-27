// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 模板领域服务的不变量测试。
/// </summary>
/// <remarks>
/// 内置模板是"随程序版本走的只读资产"：启动时由种子按嵌入资源整体回刷。
/// 因此实现对内置模板同时封了编辑与删除——改了也会被下次启动覆盖，删了会被下次启动重建，
/// 两种操作都只会制造困惑。这里把这条口径连同"模板编码全局唯一、创建后不可变"一起钉死。
/// </remarks>
public sealed class CodeGenTemplateDomainServiceTests
{
    private readonly Mock<ICodeGenTemplateRepository> _repository = new();
    private readonly CodeGenTemplateDomainService _service;

    /// <summary>
    /// 构造被测领域服务。
    /// </summary>
    public CodeGenTemplateDomainServiceTests()
    {
        _repository
            .Setup(repository => repository.AddAsync(It.IsAny<SysCodeGenTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysCodeGenTemplate entity, CancellationToken _) => entity);
        _repository
            .Setup(repository => repository.UpdateAsync(It.IsAny<SysCodeGenTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysCodeGenTemplate entity, CancellationToken _) => entity);
        _repository
            .Setup(repository => repository.DeleteAsync(It.IsAny<SysCodeGenTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _service = new CodeGenTemplateDomainService(_repository.Object);
    }

    /// <summary>
    /// 构造一条创建命令。
    /// </summary>
    /// <param name="templateCode">模板编码</param>
    /// <param name="templateName">模板名称</param>
    /// <param name="templateType">模板类型</param>
    /// <param name="templateEngine">模板引擎</param>
    /// <param name="writeMode">写入策略</param>
    /// <param name="status">状态</param>
    /// <param name="templateGroup">模板分组</param>
    /// <param name="fileExtension">文件扩展名</param>
    /// <param name="templateContent">模板内容</param>
    private static CodeGenTemplateCreateCommand CreateCommand(
        string templateCode = "custom.entity",
        string templateName = "自定义实体",
        TemplateType templateType = TemplateType.Universal,
        TemplateEngine templateEngine = TemplateEngine.Scriban,
        ArtifactWriteMode writeMode = ArtifactWriteMode.AlwaysOverwrite,
        EnableStatus status = EnableStatus.Enabled,
        string? templateGroup = "backend-crud",
        string? fileExtension = ".cs",
        string? templateContent = "{{ ClassName }}")
    {
        return new CodeGenTemplateCreateCommand(
            templateCode,
            templateName,
            null,
            templateGroup,
            templateType,
            templateEngine,
            writeMode,
            templateContent,
            "{{ ClassName }}.cs",
            "Domain/Entities",
            fileExtension,
            status,
            0,
            null);
    }

    /// <summary>
    /// 构造一条更新命令。
    /// </summary>
    /// <param name="basicId">主键</param>
    /// <param name="templateName">模板名称</param>
    /// <param name="templateType">模板类型</param>
    /// <param name="templateEngine">模板引擎</param>
    /// <param name="isEnabled">是否启用</param>
    /// <param name="templateContent">模板内容</param>
    /// <param name="remark">备注</param>
    private static CodeGenTemplateUpdateCommand UpdateCommand(
        long basicId = 1,
        string templateName = "改后的名字",
        TemplateType templateType = TemplateType.Single,
        TemplateEngine templateEngine = TemplateEngine.Scriban,
        bool isEnabled = true,
        string? templateContent = "{{ ClassName }}",
        string? remark = null)
    {
        return new CodeGenTemplateUpdateCommand(
            basicId,
            templateName,
            null,
            "backend-crud",
            templateType,
            templateEngine,
            ArtifactWriteMode.WriteOnce,
            templateContent,
            "{{ ClassName }}.cs",
            "Domain/Entities",
            ".cs",
            isEnabled,
            0,
            remark);
    }

    /// <summary>
    /// 构造一条已落库的模板实体。
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="isBuiltIn">是否内置</param>
    private static SysCodeGenTemplate Existing(long id = 1, bool isBuiltIn = false)
    {
        return CodeGenerationTestHelper.WithId(
            new SysCodeGenTemplate
            {
                TemplateCode = "custom.entity",
                TemplateName = "原名字",
                TemplateType = TemplateType.Universal,
                TemplateEngine = TemplateEngine.Scriban,
                IsBuiltIn = isBuiltIn,
                IsEnabled = true,
                Status = EnableStatus.Enabled,
                Remark = "原备注"
            },
            id);
    }

    /// <summary>
    /// 创建时命令为空必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_NullCommandShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateTemplateAsync(null!));
    }

    /// <summary>
    /// 创建时令牌已取消必须在触库前抛出。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_CancelledTokenShouldThrowBeforeRepository()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.CreateTemplateAsync(CreateCommand(), cts.Token));

        _repository.Verify(
            repository => repository.ExistsCodeAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 模板类型取未定义枚举值必须拒绝。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_UndefinedTemplateTypeShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.CreateTemplateAsync(CreateCommand(templateType: (TemplateType)55)));

        Assert.Equal("TemplateType", exception.ParamName);
    }

    /// <summary>
    /// 模板可以取「通用」类型——这是模板侧独有的取值，表配置侧才禁止。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_UniversalTemplateTypeShouldBeAllowedForTemplates()
    {
        var result = await _service.CreateTemplateAsync(CreateCommand(templateType: TemplateType.Universal));

        Assert.Equal(TemplateType.Universal, result.Template.TemplateType);
    }

    /// <summary>
    /// 模板引擎取未定义枚举值必须拒绝（Razor 已移除，0 不是合法引擎）。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_UndefinedTemplateEngineShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.CreateTemplateAsync(CreateCommand(templateEngine: (TemplateEngine)0)));

        Assert.Equal("TemplateEngine", exception.ParamName);
    }

    /// <summary>
    /// 状态取未定义枚举值必须拒绝。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_UndefinedStatusShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.CreateTemplateAsync(CreateCommand(status: (EnableStatus)55)));

        Assert.Equal("Status", exception.ParamName);
    }

    /// <summary>
    /// 模板编码为空必须拒绝。
    /// </summary>
    /// <param name="templateCode">空白编码</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTemplateAsync_BlankTemplateCodeShouldThrow(string? templateCode)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _service.CreateTemplateAsync(CreateCommand(templateCode: templateCode!)));
    }

    /// <summary>
    /// 模板编码含内部空白必须拒绝——编码要参与路径/引用拼接，带空格会一路带到产物里。
    /// </summary>
    /// <param name="templateCode">含内部空白的编码</param>
    [Theory]
    [InlineData("custom entity")]
    [InlineData("custom\tentity")]
    [InlineData("custom\nentity")]
    public async Task CreateTemplateAsync_TemplateCodeWithInnerWhitespaceShouldThrow(string templateCode)
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateTemplateAsync(CreateCommand(templateCode: templateCode)));

        Assert.Equal("模板编码不能包含空白字符。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 编码两端的空白先被裁掉再判定，不视为内部空白。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_TemplateCodeShouldBeTrimmedBeforeWhitespaceCheck()
    {
        var result = await _service.CreateTemplateAsync(CreateCommand(templateCode: "  custom.entity  "));

        Assert.Equal("custom.entity", result.Template.TemplateCode, StringComparer.Ordinal);
    }

    /// <summary>
    /// 模板编码超过 100 字必须拒绝。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_TooLongTemplateCodeShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.CreateTemplateAsync(CreateCommand(templateCode: new string('c', 101))));

        Assert.Contains("模板编码不能超过 100 个字符。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 模板名称超过 200 字必须拒绝。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_TooLongTemplateNameShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.CreateTemplateAsync(CreateCommand(templateName: new string('n', 201))));

        Assert.Contains("模板名称不能超过 200 个字符。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 文件扩展名超过 20 字必须拒绝。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_TooLongFileExtensionShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.CreateTemplateAsync(CreateCommand(fileExtension: new string('e', 21))));

        Assert.Contains("文件扩展名不能超过 20 个字符。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 模板分组超过 100 字必须拒绝。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_TooLongTemplateGroupShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.CreateTemplateAsync(CreateCommand(templateGroup: new string('g', 101))));

        Assert.Contains("模板分组不能超过 100 个字符。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 模板编码重复必须拒绝（创建时排除项传 null）。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_DuplicateTemplateCodeShouldThrow()
    {
        _repository
            .Setup(repository => repository.ExistsCodeAsync("custom.entity", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateTemplateAsync(CreateCommand()));

        Assert.Equal("模板编码已存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 用户新建的模板一律不是内置模板，且默认启用——内置身份只能由种子赋予。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_ShouldNeverProduceBuiltInTemplate()
    {
        var result = await _service.CreateTemplateAsync(CreateCommand());

        Assert.False(result.Template.IsBuiltIn);
        Assert.True(result.Template.IsEnabled);
    }

    /// <summary>
    /// 空白模板内容归 null，非空内容两端去空格。
    /// </summary>
    [Fact]
    public async Task CreateTemplateAsync_TemplateContentShouldBeNormalized()
    {
        var blank = await _service.CreateTemplateAsync(CreateCommand(templateContent: "   "));
        var filled = await _service.CreateTemplateAsync(CreateCommand(templateContent: "  {{ ClassName }}  "));

        Assert.Null(blank.Template.TemplateContent);
        Assert.Equal("{{ ClassName }}", filled.Template.TemplateContent, StringComparer.Ordinal);
    }

    /// <summary>
    /// 写入策略当前未做枚举合法性校验，未定义值会原样落库。
    /// </summary>
    /// <remarks>
    /// 这是"锁定当前真实行为"的回归锚点，不是对该行为的背书：
    /// 其余枚举（模板类型/引擎/状态）都过了 <c>Enum.IsDefined</c>，唯独 WriteMode 漏了。
    /// 一旦补上校验，本用例会红，届时应改为断言拒绝。
    /// </remarks>
    [Fact]
    public async Task CreateTemplateAsync_UndefinedWriteModeIsCurrentlyAccepted()
    {
        var result = await _service.CreateTemplateAsync(CreateCommand(writeMode: (ArtifactWriteMode)77));

        Assert.Equal((ArtifactWriteMode)77, result.Template.WriteMode);
    }

    /// <summary>
    /// 更新时主键必须大于 0。
    /// </summary>
    /// <param name="basicId">非法主键</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-9)]
    public async Task UpdateTemplateAsync_NonPositiveIdShouldThrow(long basicId)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateTemplateAsync(UpdateCommand(basicId: basicId)));

        Assert.Contains("模板主键必须大于 0。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 更新不存在的模板必须报"不存在"。
    /// </summary>
    [Fact]
    public async Task UpdateTemplateAsync_MissingTemplateShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateTemplateAsync(UpdateCommand()));

        Assert.Equal("模板不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 内置模板不能编辑：改了也会被下次启动的种子回刷掉，必须显式拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTemplateAsync_BuiltInTemplateShouldBeRejected()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Existing(isBuiltIn: true));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateTemplateAsync(UpdateCommand()));

        Assert.Equal("内置模板不能编辑，请复制为自有模板后修改。", exception.Message, StringComparer.Ordinal);
        _repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysCodeGenTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 模板编码创建后不可变：更新命令里根本没有编码字段，落库编码必须保持原值。
    /// </summary>
    [Fact]
    public async Task UpdateTemplateAsync_ShouldNeverChangeTemplateCode()
    {
        var existing = Existing();
        _repository.Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.UpdateTemplateAsync(UpdateCommand());

        Assert.Equal("custom.entity", result.Template.TemplateCode, StringComparer.Ordinal);
        Assert.Equal("改后的名字", result.Template.TemplateName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新时启用开关必须跟随命令。
    /// </summary>
    /// <param name="isEnabled">目标启用状态</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateTemplateAsync_ShouldApplyIsEnabled(bool isEnabled)
    {
        var existing = Existing();
        _repository.Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.UpdateTemplateAsync(UpdateCommand(isEnabled: isEnabled));

        Assert.Equal(isEnabled, result.Template.IsEnabled);
    }

    /// <summary>
    /// 更新时模板引擎取未定义枚举值必须拒绝，且在读库之前就拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTemplateAsync_UndefinedTemplateEngineShouldThrowBeforeLoading()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateTemplateAsync(UpdateCommand(templateEngine: (TemplateEngine)0)));

        Assert.Equal("TemplateEngine", exception.ParamName);
        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 更新命令为空必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTemplateAsync_NullCommandShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateTemplateAsync(null!));
    }

    /// <summary>
    /// 状态变更时空白备注表示"不修改"，原备注必须保留。
    /// </summary>
    [Fact]
    public async Task UpdateTemplateStatusAsync_BlankRemarkShouldKeepExisting()
    {
        var existing = Existing();
        _repository.Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.UpdateTemplateStatusAsync(
            new CodeGenTemplateStatusChangeCommand(1, EnableStatus.Disabled, "   "));

        Assert.Equal(EnableStatus.Disabled, result.Template.Status);
        Assert.Equal("原备注", result.Template.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态变更允许作用于内置模板：启停属运维决定，不在"内容只读"约束之内。
    /// </summary>
    [Fact]
    public async Task UpdateTemplateStatusAsync_BuiltInTemplateShouldStillBeToggleable()
    {
        var existing = Existing(isBuiltIn: true);
        _repository.Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.UpdateTemplateStatusAsync(
            new CodeGenTemplateStatusChangeCommand(1, EnableStatus.Disabled, null));

        Assert.Equal(EnableStatus.Disabled, result.Template.Status);
    }

    /// <summary>
    /// 状态变更时状态取未定义枚举值必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTemplateStatusAsync_UndefinedStatusShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateTemplateStatusAsync(new CodeGenTemplateStatusChangeCommand(1, (EnableStatus)44, null)));

        Assert.Equal("Status", exception.ParamName);
    }

    /// <summary>
    /// 状态变更命令为空必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateTemplateStatusAsync_NullCommandShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateTemplateStatusAsync(null!));
    }

    /// <summary>
    /// 内置模板不能删除：删了会被下次启动的种子重建。
    /// </summary>
    [Fact]
    public async Task DeleteTemplateAsync_BuiltInTemplateShouldBeRejected()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Existing(isBuiltIn: true));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteTemplateAsync(1));

        Assert.Equal("内置模板不能删除。", exception.Message, StringComparer.Ordinal);
        _repository.Verify(
            repository => repository.DeleteAsync(It.IsAny<SysCodeGenTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 删除不存在的模板必须报"不存在"。
    /// </summary>
    [Fact]
    public async Task DeleteTemplateAsync_MissingTemplateShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteTemplateAsync(1));

        Assert.Equal("模板不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 仓储删除返回 false 时必须抛出。
    /// </summary>
    [Fact]
    public async Task DeleteTemplateAsync_RepositoryFailureShouldThrow()
    {
        _repository.Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(Existing());
        _repository
            .Setup(repository => repository.DeleteAsync(It.IsAny<SysCodeGenTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteTemplateAsync(1));

        Assert.Equal("模板删除失败。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 自有模板可以正常删除。
    /// </summary>
    [Fact]
    public async Task DeleteTemplateAsync_CustomTemplateShouldBeDeleted()
    {
        var existing = Existing(id: 3);
        _repository.Setup(repository => repository.GetByIdAsync(3L, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _service.DeleteTemplateAsync(3);

        _repository.Verify(repository => repository.DeleteAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 删除的主键必须大于 0。
    /// </summary>
    [Fact]
    public async Task DeleteTemplateAsync_NonPositiveIdShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.DeleteTemplateAsync(0));
    }

    /// <summary>
    /// 删除时令牌已取消必须在触库前抛出。
    /// </summary>
    [Fact]
    public async Task DeleteTemplateAsync_CancelledTokenShouldThrowBeforeRepository()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.DeleteTemplateAsync(1, cts.Token));

        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

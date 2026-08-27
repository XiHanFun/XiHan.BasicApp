// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.DomainServices.Implementations;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// AI 提示词领域不变量测试：覆盖编码唯一且不可变、各字段长度上限与空白归一、
/// 正文非空且逐字保留（正文里的缩进是提示词语义的一部分），以及每条拒绝路径都不得留下写库副作用。
/// </summary>
public sealed class AiPromptDomainServiceTests
{
    /// <summary>
    /// 合法命令必须逐字段规范化落到实体：可选空白字段归一为 null，非空字段两端空白被裁掉。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_ValidCommandShouldNormalizeAllFields()
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreatePromptCommand() with
        {
            PromptCode = "  code-a  ",
            PromptName = "  名称  ",
            Category = "   ",
            Version = "  v9  ",
            Remark = "\r\n"
        };

        var result = await fixture.Service.CreatePromptAsync(command);

        Assert.Equal("code-a", result.Prompt.PromptCode, StringComparer.Ordinal);
        Assert.Equal("名称", result.Prompt.PromptName, StringComparer.Ordinal);
        Assert.Null(result.Prompt.Category);
        Assert.Equal("v9", result.Prompt.Version, StringComparer.Ordinal);
        Assert.Null(result.Prompt.Remark);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysAiPrompt>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 正文必须逐字保留、不做 Trim：提示词的首尾换行与缩进属于给模型的格式指令，裁掉即改变语义。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_ShouldKeepContentVerbatimWithoutTrimming()
    {
        var fixture = CreateFixture();
        const string Content = "\n  你是助手。\n  第二行。\n";
        var command = AiTestHelper.CreatePromptCommand() with { Content = Content };

        var result = await fixture.Service.CreatePromptAsync(command);

        Assert.Equal(Content, result.Prompt.Content, StringComparer.Ordinal);
    }

    /// <summary>
    /// 布尔开关、排序与状态必须原样落到实体，不得被默认值覆盖。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_ShouldCopyScalarFieldsAsIs()
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreatePromptCommand() with
        {
            IsEnabled = false,
            Sort = -3,
            Status = EnableStatus.Disabled
        };

        var result = await fixture.Service.CreatePromptAsync(command);

        Assert.False(result.Prompt.IsEnabled);
        Assert.Equal(-3, result.Prompt.Sort);
        Assert.Equal(EnableStatus.Disabled, result.Prompt.Status);
    }

    /// <summary>
    /// 空命令必须在任何仓储调用前被拒，否则空命令会一路写库。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_NullCommandShouldReject()
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.CreatePromptAsync(null!));

        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 已取消的请求不得产生任何落库副作用。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_CancelledTokenShouldRejectBeforeRepository()
    {
        var fixture = CreateFixture();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.CreatePromptAsync(AiTestHelper.CreatePromptCommand(), source.Token));

        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 非法枚举值必须拒绝，否则脏枚举落库会让前端状态筛选失效。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_UndefinedStatusShouldReject()
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreatePromptCommand() with { Status = (EnableStatus)99 };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreatePromptAsync(command));

        Assert.Contains("枚举值无效", exception.Message, StringComparison.Ordinal);
        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 枚举校验必须排在字段校验之前：命令同时非法时先报枚举，顺序变了会掩盖真正的首因。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_UndefinedStatusShouldBeCheckedBeforeCodeValidation()
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreatePromptCommand() with { Status = (EnableStatus)99, PromptCode = "  " };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreatePromptAsync(command));

        Assert.Contains("枚举值无效", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 提示词编码为空必须拒绝（编码是租户内唯一键与上层取用键）。
    /// </summary>
    /// <remarks>
    /// 领域服务走 <see cref="ArgumentException.ThrowIfNullOrWhiteSpace(string?, string?)"/>：
    /// null 抛派生类 ArgumentNullException、空白抛 ArgumentException，故用 ThrowsAny 锁"拒绝且不落库"。
    /// </remarks>
    /// <param name="promptCode">待校验的提示词编码。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("\t\r\n")]
    public async Task CreatePromptAsync_BlankCodeShouldReject(string? promptCode)
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreatePromptCommand() with { PromptCode = promptCode! };

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(() => fixture.Service.CreatePromptAsync(command));

        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 编码超出 SugarColumn Length=100 必须拒绝，否则落库截断或直接报错。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_TooLongCodeShouldReject()
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreatePromptCommand() with { PromptCode = new string('a', 101) };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreatePromptAsync(command));

        Assert.Contains("提示词编码不能超过 100 个字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 恰好 100 个字符是合法边界，不得误杀。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_CodeAtLengthBoundaryShouldPass()
    {
        var fixture = CreateFixture();
        var code = new string('a', 100);
        var command = AiTestHelper.CreatePromptCommand() with { PromptCode = code };

        var result = await fixture.Service.CreatePromptAsync(command);

        Assert.Equal(code, result.Prompt.PromptCode, StringComparer.Ordinal);
    }

    /// <summary>
    /// 编码内部含任意空白字符必须拒绝，否则不可见字符会让按编码解析永远查不到。
    /// </summary>
    /// <param name="promptCode">含内部空白字符的编码。</param>
    [Theory]
    [InlineData("co de")]
    [InlineData("co\tde")]
    [InlineData("co\nde")]
    [InlineData("co de")]
    [InlineData("co　de")]
    public async Task CreatePromptAsync_CodeWithInnerWhitespaceShouldReject(string promptCode)
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreatePromptCommand() with { PromptCode = promptCode };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreatePromptAsync(command));

        Assert.Equal("提示词编码不能包含空白字符。", exception.Message, StringComparer.Ordinal);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysAiPrompt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 租户内编码重复必须给出友好错误且一次都不写库，否则会撞唯一索引 UX_TeId_PrCd 报 500。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_DuplicateCodeShouldRejectBeforeWrite()
    {
        var fixture = CreateFixture();
        _ = fixture.Repository
            .Setup(repository => repository.ExistsCodeAsync("prompt-code", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreatePromptAsync(AiTestHelper.CreatePromptCommand()));

        Assert.Equal("提示词编码已存在。", exception.Message, StringComparer.Ordinal);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysAiPrompt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 唯一性校验必须用裁剪后的编码去查重，否则前后带空格的重复编码能绕过检查。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_ShouldCheckDuplicateWithTrimmedCode()
    {
        var fixture = CreateFixture();

        _ = await fixture.Service.CreatePromptAsync(
            AiTestHelper.CreatePromptCommand() with { PromptCode = "  prompt-code  " });

        fixture.Repository.Verify(
            repository => repository.ExistsCodeAsync("prompt-code", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 提示词名称为空必须拒绝（列声明 IsNullable=false）。
    /// </summary>
    /// <param name="promptName">待校验的名称。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePromptAsync_BlankNameShouldReject(string? promptName)
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreatePromptCommand() with { PromptName = promptName! };

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(() => fixture.Service.CreatePromptAsync(command));
    }

    /// <summary>
    /// 正文为空必须拒绝，且抛的是业务异常而非参数异常（正文是提示词的全部价值所在）。
    /// </summary>
    /// <param name="content">待校验的正文。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public async Task CreatePromptAsync_BlankContentShouldReject(string? content)
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreatePromptCommand() with { Content = content! };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreatePromptAsync(command));

        Assert.Equal("提示词正文不能为空。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 各字段的长度上限必须与实体列长度一一对应，越界即拒绝。
    /// </summary>
    /// <param name="fieldName">被测字段名。</param>
    /// <param name="maxLength">该字段的列长度上限。</param>
    [Theory]
    [InlineData("PromptName", 200)]
    [InlineData("Category", 100)]
    [InlineData("Version", 100)]
    [InlineData("Remark", 500)]
    public async Task CreatePromptAsync_FieldOverMaxLengthShouldReject(string fieldName, int maxLength)
    {
        var fixture = CreateFixture();
        var over = new string('x', maxLength + 1);
        var command = AiTestHelper.CreatePromptCommand();
        command = fieldName switch
        {
            "PromptName" => command with { PromptName = over },
            "Category" => command with { Category = over },
            "Version" => command with { Version = over },
            _ => command with { Remark = over }
        };

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.CreatePromptAsync(command));
    }

    /// <summary>
    /// 各字段恰好取到长度上限必须通过，边界不得少一格。
    /// </summary>
    /// <param name="fieldName">被测字段名。</param>
    /// <param name="maxLength">该字段的列长度上限。</param>
    [Theory]
    [InlineData("PromptName", 200)]
    [InlineData("Category", 100)]
    [InlineData("Version", 100)]
    [InlineData("Remark", 500)]
    public async Task CreatePromptAsync_FieldAtMaxLengthShouldPass(string fieldName, int maxLength)
    {
        var fixture = CreateFixture();
        var exact = new string('x', maxLength);
        var command = AiTestHelper.CreatePromptCommand();
        command = fieldName switch
        {
            "PromptName" => command with { PromptName = exact },
            "Category" => command with { Category = exact },
            "Version" => command with { Version = exact },
            _ => command with { Remark = exact }
        };

        var result = await fixture.Service.CreatePromptAsync(command);

        var actual = fieldName switch
        {
            "PromptName" => result.Prompt.PromptName,
            "Category" => result.Prompt.Category,
            "Version" => result.Prompt.Version,
            _ => result.Prompt.Remark
        };
        Assert.Equal(exact, actual, StringComparer.Ordinal);
    }

    /// <summary>
    /// 正文不受任何长度上限约束（列为 BigString），超长正文必须能落库。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_VeryLongContentShouldPass()
    {
        var fixture = CreateFixture();
        var content = new string('c', 50_000);
        var command = AiTestHelper.CreatePromptCommand() with { Content = content };

        var result = await fixture.Service.CreatePromptAsync(command);

        Assert.Equal(content, result.Prompt.Content, StringComparer.Ordinal);
    }

    /// <summary>
    /// 创建时传入的取消令牌必须原样透传给每一次仓储调用。
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_ShouldForwardCancellationTokenToRepository()
    {
        var fixture = CreateFixture();
        using var source = new CancellationTokenSource();

        _ = await fixture.Service.CreatePromptAsync(AiTestHelper.CreatePromptCommand(), source.Token);

        fixture.Repository.Verify(
            repository => repository.ExistsCodeAsync(It.IsAny<string>(), null, source.Token),
            Times.Once);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysAiPrompt>(), source.Token),
            Times.Once);
    }

    /// <summary>
    /// 更新时的主键必须为正，非法主键要在查库前被拒。
    /// </summary>
    /// <param name="basicId">非法主键值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public async Task UpdatePromptAsync_NonPositiveIdShouldReject(long basicId)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdatePromptAsync(AiTestHelper.UpdatePromptCommand(basicId)));

        Assert.Contains("提示词主键必须大于 0", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 目标提示词不存在必须给出明确错误而不是空引用。
    /// </summary>
    [Fact]
    public async Task UpdatePromptAsync_MissingPromptShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdatePromptAsync(AiTestHelper.UpdatePromptCommand(7)));

        Assert.Equal("提示词不存在。", exception.Message, StringComparer.Ordinal);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysAiPrompt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 提示词编码不可变：更新命令里根本没有该字段，更新后实体编码必须与原编码逐字相同。
    /// </summary>
    [Fact]
    public async Task UpdatePromptAsync_ShouldKeepPromptCodeImmutable()
    {
        var existing = AiTestHelper.CreatePrompt(7, "original-code");
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdatePromptAsync(AiTestHelper.UpdatePromptCommand(7));

        Assert.Equal("original-code", result.Prompt.PromptCode, StringComparer.Ordinal);
        Assert.DoesNotContain(
            "PromptCode",
            AiTestHelper.GetRecordParameterNames(typeof(AiPromptUpdateCommand)),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态不在更新命令里：更新走不到状态变更，原状态必须保持不变（状态有独立接口）。
    /// </summary>
    [Fact]
    public async Task UpdatePromptAsync_ShouldNotTouchStatus()
    {
        var existing = AiTestHelper.CreatePrompt(7);
        existing.Status = EnableStatus.Disabled;
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdatePromptAsync(AiTestHelper.UpdatePromptCommand(7));

        Assert.Equal(EnableStatus.Disabled, result.Prompt.Status);
        Assert.DoesNotContain(
            "Status",
            AiTestHelper.GetRecordParameterNames(typeof(AiPromptUpdateCommand)),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新必须把命令里的每一个可变字段整体回写到实体上。
    /// </summary>
    [Fact]
    public async Task UpdatePromptAsync_ShouldOverwriteAllMutableFields()
    {
        var existing = AiTestHelper.CreatePrompt(7);
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdatePromptAsync(AiTestHelper.UpdatePromptCommand(7));

        Assert.Equal("新提示词名", result.Prompt.PromptName, StringComparer.Ordinal);
        Assert.Equal("新分类", result.Prompt.Category, StringComparer.Ordinal);
        Assert.Equal("v2", result.Prompt.Version, StringComparer.Ordinal);
        Assert.Equal("新正文。", result.Prompt.Content, StringComparer.Ordinal);
        Assert.False(result.Prompt.IsEnabled);
        Assert.Equal(5, result.Prompt.Sort);
        Assert.Equal("新备注", result.Prompt.Remark, StringComparer.Ordinal);
        fixture.Repository.Verify(repository => repository.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 更新时把可选字段清空必须真正落成 null，否则前端"清空分类"永远无效。
    /// </summary>
    [Fact]
    public async Task UpdatePromptAsync_BlankOptionalFieldsShouldBecomeNull()
    {
        var existing = AiTestHelper.CreatePrompt(7);
        var fixture = CreateFixture(existing);
        var command = AiTestHelper.UpdatePromptCommand(7) with { Category = "  ", Version = "", Remark = null };

        var result = await fixture.Service.UpdatePromptAsync(command);

        Assert.Null(result.Prompt.Category);
        Assert.Null(result.Prompt.Version);
        Assert.Null(result.Prompt.Remark);
    }

    /// <summary>
    /// 更新时正文为空同样必须拒绝，不允许把已有提示词改成空壳。
    /// </summary>
    [Fact]
    public async Task UpdatePromptAsync_BlankContentShouldReject()
    {
        var existing = AiTestHelper.CreatePrompt(7);
        var fixture = CreateFixture(existing);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdatePromptAsync(AiTestHelper.UpdatePromptCommand(7) with { Content = "   " }));

        Assert.Equal("提示词正文不能为空。", exception.Message, StringComparer.Ordinal);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysAiPrompt>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 更新路径的字段长度上限必须与创建路径一致，不得只在创建时把关。
    /// </summary>
    /// <param name="fieldName">被测字段名。</param>
    /// <param name="maxLength">该字段的列长度上限。</param>
    [Theory]
    [InlineData("PromptName", 200)]
    [InlineData("Category", 100)]
    [InlineData("Version", 100)]
    [InlineData("Remark", 500)]
    public async Task UpdatePromptAsync_FieldOverMaxLengthShouldReject(string fieldName, int maxLength)
    {
        var existing = AiTestHelper.CreatePrompt(7);
        var fixture = CreateFixture(existing);
        var over = new string('x', maxLength + 1);
        var command = AiTestHelper.UpdatePromptCommand(7);
        command = fieldName switch
        {
            "PromptName" => command with { PromptName = over },
            "Category" => command with { Category = over },
            "Version" => command with { Version = over },
            _ => command with { Remark = over }
        };

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.UpdatePromptAsync(command));
    }

    /// <summary>
    /// 状态变更必须双校验主键与枚举。
    /// </summary>
    [Fact]
    public async Task UpdatePromptStatusAsync_UndefinedStatusShouldReject()
    {
        var fixture = CreateFixture(AiTestHelper.CreatePrompt(7));
        var command = new AiPromptStatusChangeCommand(7, (EnableStatus)99, null);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdatePromptStatusAsync(command));

        Assert.Contains("枚举值无效", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 状态变更时非法主键必须在查库前被拒。
    /// </summary>
    /// <param name="basicId">非法主键值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-9)]
    public async Task UpdatePromptStatusAsync_NonPositiveIdShouldReject(long basicId)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdatePromptStatusAsync(new AiPromptStatusChangeCommand(basicId, EnableStatus.Enabled, null)));

        Assert.Contains("提示词主键必须大于 0", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 状态变更时空白备注必须保留原备注，否则一次停用就把历史备注抹成 null。
    /// </summary>
    /// <param name="remark">空白备注输入。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdatePromptStatusAsync_BlankRemarkShouldKeepOriginal(string? remark)
    {
        var existing = AiTestHelper.CreatePrompt(7);
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdatePromptStatusAsync(
            new AiPromptStatusChangeCommand(7, EnableStatus.Disabled, remark));

        Assert.Equal(EnableStatus.Disabled, result.Prompt.Status);
        Assert.Equal("原备注", result.Prompt.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态变更时非空备注必须覆盖原值并裁掉两端空白。
    /// </summary>
    [Fact]
    public async Task UpdatePromptStatusAsync_NonBlankRemarkShouldOverwrite()
    {
        var existing = AiTestHelper.CreatePrompt(7);
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdatePromptStatusAsync(
            new AiPromptStatusChangeCommand(7, EnableStatus.Disabled, "  停用原因  "));

        Assert.Equal("停用原因", result.Prompt.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态变更的备注同样受 500 长度上限约束。
    /// </summary>
    [Fact]
    public async Task UpdatePromptStatusAsync_TooLongRemarkShouldReject()
    {
        var fixture = CreateFixture(AiTestHelper.CreatePrompt(7));

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdatePromptStatusAsync(
                new AiPromptStatusChangeCommand(7, EnableStatus.Disabled, new string('x', 501))));
    }

    /// <summary>
    /// 状态变更时目标不存在必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdatePromptStatusAsync_MissingPromptShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdatePromptStatusAsync(new AiPromptStatusChangeCommand(7, EnableStatus.Disabled, null)));

        Assert.Equal("提示词不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 删除时非法主键必须在查库前被拒。
    /// </summary>
    /// <param name="id">非法主键值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeletePromptAsync_NonPositiveIdShouldReject(long id)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.DeletePromptAsync(id));

        Assert.Contains("提示词主键必须大于 0", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 删除不存在的提示词必须拒绝。
    /// </summary>
    [Fact]
    public async Task DeletePromptAsync_MissingPromptShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeletePromptAsync(7));

        Assert.Equal("提示词不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 仓储删除返回 false 必须抛出，否则删除失败被当成成功会让前端列表与库不一致。
    /// </summary>
    [Fact]
    public async Task DeletePromptAsync_RepositoryFailureShouldReject()
    {
        var existing = AiTestHelper.CreatePrompt(7);
        var fixture = CreateFixture(existing);
        _ = fixture.Repository
            .Setup(repository => repository.DeleteAsync(existing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeletePromptAsync(7));

        Assert.Equal("提示词删除失败。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 删除成功路径必须把取消令牌透传给每一次仓储调用。
    /// </summary>
    [Fact]
    public async Task DeletePromptAsync_ShouldForwardCancellationToken()
    {
        var existing = AiTestHelper.CreatePrompt(7);
        var fixture = CreateFixture(existing);
        using var source = new CancellationTokenSource();

        await fixture.Service.DeletePromptAsync(7, source.Token);

        fixture.Repository.Verify(repository => repository.GetByIdAsync(7, source.Token), Times.Once);
        fixture.Repository.Verify(repository => repository.DeleteAsync(existing, source.Token), Times.Once);
    }

    /// <summary>
    /// 除创建外的三个写方法同样要拒绝空命令与已取消令牌。
    /// </summary>
    [Fact]
    public async Task AllWriteMethods_NullCommandAndCancelledTokenShouldReject()
    {
        var fixture = CreateFixture(AiTestHelper.CreatePrompt(7));
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.UpdatePromptAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.UpdatePromptStatusAsync(null!));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.UpdatePromptAsync(AiTestHelper.UpdatePromptCommand(7), source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.UpdatePromptStatusAsync(new AiPromptStatusChangeCommand(7, EnableStatus.Enabled, null), source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.DeletePromptAsync(7, source.Token));
    }

    /// <summary>
    /// 构造纯内存测试夹具：仓储 Add/Update 回传入参，编码不重复，删除成功。
    /// </summary>
    /// <param name="existing">GetByIdAsync 命中的既有提示词（null 表示查不到）。</param>
    /// <returns>被测服务与其仓储替身。</returns>
    private static PromptFixture CreateFixture(SysAiPrompt? existing = null)
    {
        var repository = new Mock<IAiPromptRepository>(MockBehavior.Strict);
        _ = repository
            .Setup(item => item.ExistsCodeAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ = repository
            .Setup(item => item.AddAsync(It.IsAny<SysAiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysAiPrompt entity, CancellationToken _) => AiTestHelper.SetBasicId(entity, 100));
        _ = repository
            .Setup(item => item.UpdateAsync(It.IsAny<SysAiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysAiPrompt entity, CancellationToken _) => entity);
        _ = repository
            .Setup(item => item.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _ = repository
            .Setup(item => item.DeleteAsync(It.IsAny<SysAiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return new PromptFixture(new AiPromptDomainService(repository.Object), repository);
    }

    /// <summary>
    /// 提示词领域服务测试夹具。
    /// </summary>
    /// <param name="Service">被测领域服务。</param>
    /// <param name="Repository">仓储替身。</param>
    private sealed record PromptFixture(AiPromptDomainService Service, Mock<IAiPromptRepository> Repository);
}

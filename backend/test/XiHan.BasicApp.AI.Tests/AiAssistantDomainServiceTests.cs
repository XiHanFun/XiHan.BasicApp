// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.AI.Domain.DomainServices.Implementations;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// AI 助手领域不变量测试，覆盖编码唯一性与不可变、字段长度与空白归一、
/// 检索片段数与历史条数的闭区间、租户内单默认互斥，以及每条拒绝路径不得留下写库副作用。
/// </summary>
public sealed class AiAssistantDomainServiceTests
{
    /// <summary>
    /// 合法命令必须逐字段规范化落到实体上：可选空白字段归一为 null，非空字段两端空白被裁掉。
    /// </summary>
    [Fact]
    public async Task CreateAssistantAsync_ValidCommandShouldNormalizeAllFields()
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreateAssistantCommand() with
        {
            AssistantCode = "  code-a  ",
            AssistantName = "  助手  ",
            Avatar = "   ",
            Description = "  简介  ",
            Greeting = "\t",
            PromptCode = "  p1  ",
            Remark = "\r\n"
        };

        var result = await fixture.Service.CreateAssistantAsync(command);

        Assert.Equal("code-a", result.Assistant.AssistantCode, StringComparer.Ordinal);
        Assert.Equal("助手", result.Assistant.AssistantName, StringComparer.Ordinal);
        Assert.Null(result.Assistant.Avatar);
        Assert.Equal("简介", result.Assistant.Description, StringComparer.Ordinal);
        Assert.Null(result.Assistant.Greeting);
        Assert.Equal("p1", result.Assistant.PromptCode, StringComparer.Ordinal);
        Assert.Null(result.Assistant.Remark);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysAiAssistant>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 空命令必须在任何仓储调用前被拒绝，否则空命令会一路写库。
    /// </summary>
    [Fact]
    public async Task CreateAssistantAsync_NullCommandShouldReject()
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.CreateAssistantAsync(null!));

        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 已取消的请求不得产生任何落库副作用。
    /// </summary>
    [Fact]
    public async Task CreateAssistantAsync_CancelledTokenShouldRejectBeforeRepository()
    {
        var fixture = CreateFixture();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.CreateAssistantAsync(AiTestHelper.CreateAssistantCommand(), source.Token));

        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 非法枚举值必须拒绝，否则脏枚举落库会让前端状态筛选失效。
    /// </summary>
    [Fact]
    public async Task CreateAssistantAsync_UndefinedStatusShouldReject()
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreateAssistantCommand() with { Status = (EnableStatus)99 };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateAssistantAsync(command));

        Assert.Contains("枚举值无效", exception.Message, StringComparison.Ordinal);
        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 助手编码为空必须拒绝（编码是租户内唯一键与人读标识）。
    /// </summary>
    /// <remarks>
    /// 用 ThrowsAny 而非 Throws：领域服务走 <see cref="ArgumentException.ThrowIfNullOrWhiteSpace(string?, string?)"/>，
    /// 按 .NET 约定 null 抛派生类 ArgumentNullException、空白抛 ArgumentException，
    /// 而 xUnit 的 Throws 要求类型精确匹配。这里要锁的契约是"拒绝且不落库"，不是具体派生类型。
    /// </remarks>
    /// <param name="assistantCode">待校验的助手编码。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public async Task CreateAssistantAsync_BlankCodeShouldReject(string? assistantCode)
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreateAssistantCommand() with { AssistantCode = assistantCode! };

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(() => fixture.Service.CreateAssistantAsync(command));

        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 助手编码超出 SugarColumn Length=100 必须拒绝，否则落库截断或直接报错。
    /// </summary>
    [Fact]
    public async Task CreateAssistantAsync_TooLongCodeShouldReject()
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreateAssistantCommand() with { AssistantCode = new string('a', 101) };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateAssistantAsync(command));

        Assert.Contains("助手编码不能超过 100 个字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 恰好 100 个字符是合法边界，不得误杀。
    /// </summary>
    [Fact]
    public async Task CreateAssistantAsync_CodeAtLengthBoundaryShouldPass()
    {
        var fixture = CreateFixture();
        var code = new string('a', 100);
        var command = AiTestHelper.CreateAssistantCommand() with { AssistantCode = code };

        var result = await fixture.Service.CreateAssistantAsync(command);

        Assert.Equal(code, result.Assistant.AssistantCode, StringComparer.Ordinal);
    }

    /// <summary>
    /// 编码内部含任意空白字符必须拒绝，否则不可见字符会让按编码解析永远查不到。
    /// </summary>
    /// <param name="assistantCode">含内部空白字符的编码。</param>
    [Theory]
    [InlineData("co de")]
    [InlineData("co\tde")]
    [InlineData("co\nde")]
    [InlineData("co　de")]
    public async Task CreateAssistantAsync_CodeWithInnerWhitespaceShouldReject(string assistantCode)
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreateAssistantCommand() with { AssistantCode = assistantCode };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAssistantAsync(command));

        Assert.Equal("助手编码不能包含空白字符。", exception.Message, StringComparer.Ordinal);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysAiAssistant>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 租户内编码重复必须给出友好错误且一次都不写库，否则会撞唯一索引 UX_TeId_AsCd 报 500。
    /// </summary>
    [Fact]
    public async Task CreateAssistantAsync_DuplicateCodeShouldRejectBeforeWrite()
    {
        var fixture = CreateFixture();
        _ = fixture.Repository
            .Setup(repository => repository.ExistsCodeAsync("assistant-code", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAssistantAsync(AiTestHelper.CreateAssistantCommand()));

        Assert.Equal("助手编码已存在。", exception.Message, StringComparer.Ordinal);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysAiAssistant>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 各可选字段的长度上限必须与实体列长度一一对应，越界即拒绝。
    /// </summary>
    /// <param name="fieldName">被测字段名。</param>
    /// <param name="maxLength">该字段的列长度上限。</param>
    [Theory]
    [InlineData("AssistantName", 100)]
    [InlineData("Avatar", 500)]
    [InlineData("Description", 500)]
    [InlineData("Greeting", 1000)]
    [InlineData("PromptCode", 100)]
    [InlineData("ProviderCode", 100)]
    [InlineData("KnowledgeProviderCode", 100)]
    [InlineData("Remark", 500)]
    public async Task CreateAssistantAsync_FieldOverMaxLengthShouldReject(string fieldName, int maxLength)
    {
        var fixture = CreateFixture();
        var over = new string('x', maxLength + 1);
        var command = AiTestHelper.CreateAssistantCommand();
        command = fieldName switch
        {
            "AssistantName" => command with { AssistantName = over },
            "Avatar" => command with { Avatar = over },
            "Description" => command with { Description = over },
            "Greeting" => command with { Greeting = over },
            "PromptCode" => command with { PromptCode = over },
            "ProviderCode" => command with { ProviderCode = over },
            "KnowledgeProviderCode" => command with { KnowledgeProviderCode = over },
            _ => command with { Remark = over }
        };

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.CreateAssistantAsync(command));
    }

    /// <summary>
    /// 检索片段数越界必须拒绝，否则检索片段会挤占模型上下文。
    /// </summary>
    /// <param name="topK">越界的检索片段数。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(1000)]
    public async Task CreateAssistantAsync_KnowledgeTopKOutOfRangeShouldReject(int topK)
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreateAssistantCommand() with { KnowledgeTopK = topK };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateAssistantAsync(command));

        Assert.Contains("1~20", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 检索片段数的闭区间端点必须原样通过。
    /// </summary>
    /// <param name="topK">闭区间端点值。</param>
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public async Task CreateAssistantAsync_KnowledgeTopKAtBoundaryShouldPass(int topK)
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreateAssistantCommand() with { KnowledgeTopK = topK };

        var result = await fixture.Service.CreateAssistantAsync(command);

        Assert.Equal(topK, result.Assistant.KnowledgeTopK);
    }

    /// <summary>
    /// 历史消息条数越界必须拒绝，否则历史无限带入会撑爆上下文。
    /// </summary>
    /// <param name="historyRounds">越界的历史条数。</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(51)]
    public async Task CreateAssistantAsync_HistoryRoundsOutOfRangeShouldReject(int historyRounds)
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreateAssistantCommand() with { HistoryRounds = historyRounds };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateAssistantAsync(command));

        Assert.Contains("0~50", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 历史消息条数的闭区间端点必须原样通过（0 表示不带历史）。
    /// </summary>
    /// <param name="historyRounds">闭区间端点值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    public async Task CreateAssistantAsync_HistoryRoundsAtBoundaryShouldPass(int historyRounds)
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreateAssistantCommand() with { HistoryRounds = historyRounds };

        var result = await fixture.Service.CreateAssistantAsync(command);

        Assert.Equal(historyRounds, result.Assistant.HistoryRounds);
    }

    /// <summary>
    /// 新建默认助手必须把租户内其它默认行逐条置为非默认，否则聊天页默认打开哪个不确定。
    /// </summary>
    [Fact]
    public async Task CreateAssistantAsync_DefaultAssistantShouldClearOtherDefaults()
    {
        var other = AiTestHelper.CreateAssistant(9, "other");
        other.IsDefault = true;
        var fixture = CreateFixture();
        _ = fixture.Repository
            .Setup(repository => repository.GetOtherDefaultsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([other]);

        var result = await fixture.Service.CreateAssistantAsync(
            AiTestHelper.CreateAssistantCommand() with { IsDefault = true });

        Assert.True(result.Assistant.IsDefault);
        Assert.False(other.IsDefault);
        fixture.Repository.Verify(
            repository => repository.GetOtherDefaultsAsync(result.Assistant.BasicId, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(other, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 非默认助手一次都不能触发默认互斥清理（避免无谓的全表扫与写）。
    /// </summary>
    [Fact]
    public async Task CreateAssistantAsync_NonDefaultShouldNotTouchOtherDefaults()
    {
        var fixture = CreateFixture();

        _ = await fixture.Service.CreateAssistantAsync(AiTestHelper.CreateAssistantCommand() with { IsDefault = false });

        fixture.Repository.Verify(
            repository => repository.GetOtherDefaultsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 创建时传入的取消令牌必须原样透传给每一次仓储调用。
    /// </summary>
    [Fact]
    public async Task CreateAssistantAsync_ShouldForwardCancellationTokenToRepository()
    {
        var fixture = CreateFixture();
        using var source = new CancellationTokenSource();

        _ = await fixture.Service.CreateAssistantAsync(AiTestHelper.CreateAssistantCommand(), source.Token);

        fixture.Repository.Verify(
            repository => repository.ExistsCodeAsync(It.IsAny<string>(), null, source.Token),
            Times.Once);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysAiAssistant>(), source.Token),
            Times.Once);
    }

    /// <summary>
    /// 更新时的主键必须为正，非法主键要在查库前被拒。
    /// </summary>
    /// <param name="basicId">非法主键值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateAssistantAsync_NonPositiveIdShouldReject(long basicId)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdateAssistantAsync(AiTestHelper.UpdateAssistantCommand(basicId)));

        Assert.Contains("助手主键必须大于 0", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 目标助手不存在必须给出明确错误而不是空引用。
    /// </summary>
    [Fact]
    public async Task UpdateAssistantAsync_MissingAssistantShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateAssistantAsync(AiTestHelper.UpdateAssistantCommand(7)));

        Assert.Equal("助手不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 助手编码不可变：更新命令里根本没有该字段，更新后实体编码必须与原编码逐字相同。
    /// </summary>
    [Fact]
    public async Task UpdateAssistantAsync_ShouldKeepAssistantCodeImmutable()
    {
        var existing = AiTestHelper.CreateAssistant(7, "original-code");
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdateAssistantAsync(AiTestHelper.UpdateAssistantCommand(7));

        Assert.Equal("original-code", result.Assistant.AssistantCode, StringComparer.Ordinal);
        Assert.DoesNotContain(
            "AssistantCode",
            AiTestHelper.GetRecordParameterNames(typeof(XiHan.BasicApp.AI.Domain.DomainServices.AiAssistantUpdateCommand)),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新必须把命令里的每一个可变字段整体回写到实体上。
    /// </summary>
    [Fact]
    public async Task UpdateAssistantAsync_ShouldOverwriteAllMutableFields()
    {
        var existing = AiTestHelper.CreateAssistant(7);
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdateAssistantAsync(AiTestHelper.UpdateAssistantCommand(7));

        Assert.Equal("新名称", result.Assistant.AssistantName, StringComparer.Ordinal);
        Assert.Equal("new-avatar.png", result.Assistant.Avatar, StringComparer.Ordinal);
        Assert.Equal("新简介", result.Assistant.Description, StringComparer.Ordinal);
        Assert.Equal("新开场白", result.Assistant.Greeting, StringComparer.Ordinal);
        Assert.Equal("new-prompt", result.Assistant.PromptCode, StringComparer.Ordinal);
        Assert.Equal("new-provider", result.Assistant.ProviderCode, StringComparer.Ordinal);
        Assert.False(result.Assistant.EnableKnowledge);
        Assert.Equal("new-embed", result.Assistant.KnowledgeProviderCode, StringComparer.Ordinal);
        Assert.Equal(8, result.Assistant.KnowledgeTopK);
        Assert.Equal(20, result.Assistant.HistoryRounds);
        Assert.Equal(3, result.Assistant.Sort);
        Assert.Equal("新备注", result.Assistant.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新时把助手设为默认同样要触发单默认互斥清理，避免改字段顺手把默认弄成多份。
    /// </summary>
    [Fact]
    public async Task UpdateAssistantAsync_DefaultAssistantShouldClearOtherDefaults()
    {
        var existing = AiTestHelper.CreateAssistant(7);
        var other = AiTestHelper.CreateAssistant(9, "other");
        other.IsDefault = true;
        var fixture = CreateFixture(existing);
        _ = fixture.Repository
            .Setup(repository => repository.GetOtherDefaultsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync([other]);

        _ = await fixture.Service.UpdateAssistantAsync(AiTestHelper.UpdateAssistantCommand(7) with { IsDefault = true });

        Assert.False(other.IsDefault);
        fixture.Repository.Verify(
            repository => repository.GetOtherDefaultsAsync(7, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 状态变更必须双校验主键与枚举。
    /// </summary>
    [Fact]
    public async Task UpdateAssistantStatusAsync_UndefinedStatusShouldReject()
    {
        var fixture = CreateFixture(AiTestHelper.CreateAssistant(7));
        var command = new XiHan.BasicApp.AI.Domain.DomainServices.AiAssistantStatusChangeCommand(7, (EnableStatus)99, null);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdateAssistantStatusAsync(command));

        Assert.Contains("枚举值无效", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 状态变更时空白备注必须保留原备注，否则一次停用就把历史备注抹成 null。
    /// </summary>
    /// <param name="remark">空白备注输入。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAssistantStatusAsync_BlankRemarkShouldKeepOriginal(string? remark)
    {
        var existing = AiTestHelper.CreateAssistant(7);
        var fixture = CreateFixture(existing);
        var command = new XiHan.BasicApp.AI.Domain.DomainServices.AiAssistantStatusChangeCommand(7, EnableStatus.Disabled, remark);

        var result = await fixture.Service.UpdateAssistantStatusAsync(command);

        Assert.Equal(EnableStatus.Disabled, result.Assistant.Status);
        Assert.Equal("原备注", result.Assistant.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态变更时非空备注必须覆盖原值并裁掉两端空白。
    /// </summary>
    [Fact]
    public async Task UpdateAssistantStatusAsync_NonBlankRemarkShouldOverwrite()
    {
        var existing = AiTestHelper.CreateAssistant(7);
        var fixture = CreateFixture(existing);
        var command = new XiHan.BasicApp.AI.Domain.DomainServices.AiAssistantStatusChangeCommand(7, EnableStatus.Disabled, "  停用原因  ");

        var result = await fixture.Service.UpdateAssistantStatusAsync(command);

        Assert.Equal("停用原因", result.Assistant.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 已禁用的助手不得设为默认，否则默认助手不可用会让聊天页开局即报错。
    /// </summary>
    [Fact]
    public async Task SetDefaultAsync_DisabledAssistantShouldReject()
    {
        var existing = AiTestHelper.CreateAssistant(7);
        existing.IsEnabled = false;
        var fixture = CreateFixture(existing);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SetDefaultAsync(7));

        Assert.Equal("已禁用的助手不能设为默认。", exception.Message, StringComparer.Ordinal);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysAiAssistant>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 目标本非默认时必须置为默认并写库，同时清理其它默认行。
    /// </summary>
    [Fact]
    public async Task SetDefaultAsync_NonDefaultAssistantShouldPromoteAndClearOthers()
    {
        var existing = AiTestHelper.CreateAssistant(7);
        var other = AiTestHelper.CreateAssistant(9, "other");
        other.IsDefault = true;
        var fixture = CreateFixture(existing);
        _ = fixture.Repository
            .Setup(repository => repository.GetOtherDefaultsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync([other]);

        var result = await fixture.Service.SetDefaultAsync(7);

        Assert.True(result.Assistant.IsDefault);
        Assert.False(other.IsDefault);
        fixture.Repository.Verify(repository => repository.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 目标本已是默认时只清理其它行、不再对自身发起一次无意义的写。
    /// </summary>
    [Fact]
    public async Task SetDefaultAsync_AlreadyDefaultShouldNotWriteItself()
    {
        var existing = AiTestHelper.CreateAssistant(7);
        existing.IsDefault = true;
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.SetDefaultAsync(7);

        Assert.True(result.Assistant.IsDefault);
        fixture.Repository.Verify(
            repository => repository.GetOtherDefaultsAsync(7, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(existing, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 设为默认时非法主键必须在查库前被拒。
    /// </summary>
    /// <param name="id">非法主键值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task SetDefaultAsync_NonPositiveIdShouldReject(long id)
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.SetDefaultAsync(id));

        fixture.Repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 删除不存在的助手必须拒绝。
    /// </summary>
    [Fact]
    public async Task DeleteAssistantAsync_MissingAssistantShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteAssistantAsync(7));

        Assert.Equal("助手不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 仓储删除返回 false 必须抛出，否则删除失败被当成成功会让前端列表与库不一致。
    /// </summary>
    [Fact]
    public async Task DeleteAssistantAsync_RepositoryFailureShouldReject()
    {
        var existing = AiTestHelper.CreateAssistant(7);
        var fixture = CreateFixture(existing);
        _ = fixture.Repository
            .Setup(repository => repository.DeleteAsync(existing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteAssistantAsync(7));

        Assert.Equal("助手删除失败。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 删除成功路径必须把取消令牌透传给仓储。
    /// </summary>
    [Fact]
    public async Task DeleteAssistantAsync_ShouldForwardCancellationToken()
    {
        var existing = AiTestHelper.CreateAssistant(7);
        var fixture = CreateFixture(existing);
        using var source = new CancellationTokenSource();
        _ = fixture.Repository
            .Setup(repository => repository.DeleteAsync(existing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await fixture.Service.DeleteAssistantAsync(7, source.Token);

        fixture.Repository.Verify(repository => repository.GetByIdAsync(7, source.Token), Times.Once);
        fixture.Repository.Verify(repository => repository.DeleteAsync(existing, source.Token), Times.Once);
    }

    /// <summary>
    /// 除创建外的四个写方法同样要拒绝空命令与已取消令牌。
    /// </summary>
    [Fact]
    public async Task AllWriteMethods_NullCommandAndCancelledTokenShouldReject()
    {
        var fixture = CreateFixture(AiTestHelper.CreateAssistant(7));
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.UpdateAssistantAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.UpdateAssistantStatusAsync(null!));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.UpdateAssistantAsync(AiTestHelper.UpdateAssistantCommand(7), source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.SetDefaultAsync(7, source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.DeleteAssistantAsync(7, source.Token));
    }

    /// <summary>
    /// 构造纯内存测试夹具：仓储 Add/Update 回传入参，编码不重复，无其它默认行。
    /// </summary>
    /// <param name="existing">GetByIdAsync 命中的既有助手（null 表示查不到）。</param>
    /// <returns>被测服务与其仓储替身。</returns>
    private static AssistantFixture CreateFixture(SysAiAssistant? existing = null)
    {
        var repository = new Mock<IAiAssistantRepository>(MockBehavior.Strict);
        _ = repository
            .Setup(item => item.ExistsCodeAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ = repository
            .Setup(item => item.AddAsync(It.IsAny<SysAiAssistant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysAiAssistant entity, CancellationToken _) => AiTestHelper.SetBasicId(entity, 100));
        _ = repository
            .Setup(item => item.UpdateAsync(It.IsAny<SysAiAssistant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysAiAssistant entity, CancellationToken _) => entity);
        _ = repository
            .Setup(item => item.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _ = repository
            .Setup(item => item.GetOtherDefaultsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _ = repository
            .Setup(item => item.DeleteAsync(It.IsAny<SysAiAssistant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return new AssistantFixture(new AiAssistantDomainService(repository.Object), repository);
    }

    /// <summary>
    /// 助手领域服务测试夹具。
    /// </summary>
    /// <param name="Service">被测领域服务。</param>
    /// <param name="Repository">仓储替身。</param>
    private sealed record AssistantFixture(AiAssistantDomainService Service, Mock<IAiAssistantRepository> Repository);
}

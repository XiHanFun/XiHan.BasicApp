// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.AI.Application.AppServices;
using XiHan.BasicApp.AI.Infrastructure.Configuration;
using XiHan.BasicApp.AI.Infrastructure.Skills;
using XiHan.Framework.AI.Abstractions.Chat;
using XiHan.Framework.AI.Abstractions.Rag;
using XiHan.Framework.AI.Abstractions.Rag.Models;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// 知识检索链路测试：检索条数的收敛口径、租户过滤的取值、"只检索不问答"的短路，
/// 以及检索技能对外暴露的工具名与说明。
/// </summary>
/// <remarks>检索器、增强器、会话服务全部用 Moq 替身，整套用例不连向量库、不发模型请求。</remarks>
public sealed class AiKnowledgeQueryTests
{
    /// <summary>
    /// 未指定条数时必须回落到 RAG 配置里的默认条数，而不是硬编码常量。
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithoutTopKShouldFallBackToConfiguredDefault()
    {
        var fixture = CreateFixture(defaultTopK: 9);

        _ = await fixture.Service.QueryAsync(new Application.Dtos.KnowledgeQueryDto { Query = "部署流程", Answer = false });

        fixture.Retriever.Verify(
            retriever => retriever.RetrieveAsync("部署流程", 9, It.IsAny<RetrievalFilter?>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 检索条数必须收敛到 1~20 的闭区间：过小取不到片段，过大会把整篇文档灌进上下文。
    /// </summary>
    /// <param name="requestedTopK">请求的条数。</param>
    /// <param name="expectedTopK">收敛后的条数。</param>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    [InlineData(20, 20)]
    [InlineData(21, 20)]
    [InlineData(int.MaxValue, 20)]
    public async Task QueryAsync_TopKShouldBeClampedIntoOneToTwenty(int requestedTopK, int expectedTopK)
    {
        var fixture = CreateFixture();

        _ = await fixture.Service.QueryAsync(new Application.Dtos.KnowledgeQueryDto
        {
            Query = "部署流程",
            TopK = requestedTopK,
            Answer = false
        });

        fixture.Retriever.Verify(
            retriever => retriever.RetrieveAsync(It.IsAny<string>(), expectedTopK, It.IsAny<RetrievalFilter?>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 查询词两端空白必须先裁掉再检索，否则同一个问题会因为多打一个空格而错过向量缓存与命中。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldTrimQueryBeforeRetrieval()
    {
        var fixture = CreateFixture();

        _ = await fixture.Service.QueryAsync(new Application.Dtos.KnowledgeQueryDto
        {
            Query = "  部署流程  ",
            Answer = false
        });

        fixture.Retriever.Verify(
            retriever => retriever.RetrieveAsync("部署流程", It.IsAny<int>(), It.IsAny<RetrievalFilter?>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 检索必须带当前租户过滤；无租户上下文时按平台全局（TenantId=0）检索，绝不能传 null 变成"不限租户"。
    /// </summary>
    /// <param name="currentTenantId">当前租户主键（null 表示平台上下文）。</param>
    /// <param name="expectedTenantId">检索过滤里应出现的租户主键。</param>
    [Theory]
    [InlineData(null, 0L)]
    [InlineData(0L, 0L)]
    [InlineData(88L, 88L)]
    public async Task QueryAsync_ShouldFilterByCurrentTenant(long? currentTenantId, long expectedTenantId)
    {
        var fixture = CreateFixture(currentTenantId: currentTenantId);
        RetrievalFilter? captured = null;
        _ = fixture.Retriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<RetrievalFilter?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, int _, RetrievalFilter? filter, string? _, CancellationToken _) => captured = filter)
            .ReturnsAsync([]);

        _ = await fixture.Service.QueryAsync(new Application.Dtos.KnowledgeQueryDto { Query = "部署流程", Answer = false });

        Assert.NotNull(captured);
        Assert.Equal(expectedTenantId, captured!.TenantId);
        Assert.Null(captured.DocumentId);
    }

    /// <summary>
    /// 指定 provider 时必须原样透传给检索器（嵌入与会话共用同一份 provider 配置）。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldForwardProviderToRetriever()
    {
        var fixture = CreateFixture();

        _ = await fixture.Service.QueryAsync(new Application.Dtos.KnowledgeQueryDto
        {
            Query = "部署流程",
            Provider = "prod-openai",
            Answer = false
        });

        fixture.Retriever.Verify(
            retriever => retriever.RetrieveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<RetrievalFilter?>(), "prod-openai", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 关闭问答时只返回命中片段，一次模型调用都不能发起（省钱也省时延）。
    /// </summary>
    [Fact]
    public async Task QueryAsync_AnswerDisabledShouldReturnCitationsOnly()
    {
        var fixture = CreateFixture();
        _ = fixture.Retriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<RetrievalFilter?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateChunk()]);

        var result = await fixture.Service.QueryAsync(new Application.Dtos.KnowledgeQueryDto { Query = "部署流程", Answer = false });

        Assert.Null(result.Answer);
        _ = Assert.Single(result.Citations);
        fixture.AiService.VerifyNoOtherCalls();
        fixture.Augmenter.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 一条片段都没检索到时不得发起问答：没有上下文的回答就是模型在编，且引用列表必然为空。
    /// </summary>
    [Fact]
    public async Task QueryAsync_NoCitationShouldSkipAnswerGeneration()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.QueryAsync(new Application.Dtos.KnowledgeQueryDto { Query = "部署流程", Answer = true });

        Assert.Null(result.Answer);
        Assert.Empty(result.Citations);
        fixture.AiService.VerifyNoOtherCalls();
        fixture.Augmenter.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 检索到片段且开启问答时，必须先注入上下文再走会话生成，答案与引用一并返回。
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithCitationsShouldAugmentThenAnswer()
    {
        var fixture = CreateFixture();
        _ = fixture.Retriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<RetrievalFilter?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateChunk()]);

        var result = await fixture.Service.QueryAsync(new Application.Dtos.KnowledgeQueryDto
        {
            Query = "  部署流程  ",
            Answer = true
        });

        Assert.Equal("这是答案。", result.Answer, StringComparer.Ordinal);
        _ = Assert.Single(result.Citations);
        Assert.Equal("4242", result.Citations[0].DocumentId, StringComparer.Ordinal);
        fixture.Augmenter.Verify(
            augmenter => augmenter.Augment("部署流程", It.IsAny<IReadOnlyList<RetrievedChunk>>()),
            Times.Once);
        fixture.AiService.Verify(
            service => service.ChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<XiHanChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 送进会话的必须是增强后的提示词，而不是用户原问题——否则检索结果根本没进上下文。
    /// </summary>
    [Fact]
    public async Task QueryAsync_ShouldSendAugmentedPromptRatherThanRawQuestion()
    {
        var fixture = CreateFixture();
        _ = fixture.Retriever
            .Setup(retriever => retriever.RetrieveAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<RetrievalFilter?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateChunk()]);
        IEnumerable<ChatMessage>? captured = null;
        XiHanChatOptions? capturedOptions = null;
        _ = fixture.AiService
            .Setup(service => service.ChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<XiHanChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<ChatMessage> messages, XiHanChatOptions? options, CancellationToken _) =>
            {
                captured = messages;
                capturedOptions = options;
            })
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "这是答案。")));

        _ = await fixture.Service.QueryAsync(new Application.Dtos.KnowledgeQueryDto
        {
            Query = "部署流程",
            Provider = "prod-openai",
            Answer = true
        });

        var messages = captured!.ToList();
        var single = Assert.Single(messages);
        Assert.Equal(ChatRole.User, single.Role);
        Assert.Equal("增强后的提示词", single.Text, StringComparer.Ordinal);
        Assert.Equal("prod-openai", capturedOptions!.Provider, StringComparer.Ordinal);
    }

    /// <summary>
    /// 空入参与空白查询必须 fail-fast，绝不能拿空串去做向量检索。
    /// </summary>
    /// <param name="query">空白查询词。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryAsync_BlankQueryShouldReject(string? query)
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => fixture.Service.QueryAsync(new Application.Dtos.KnowledgeQueryDto { Query = query! }));

        fixture.Retriever.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 空请求体必须抛出空引用异常，且不触碰检索器。
    /// </summary>
    [Fact]
    public async Task QueryAsync_NullInputShouldReject()
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.QueryAsync(null!));

        fixture.Retriever.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 已取消的请求必须在检索前直接抛出。
    /// </summary>
    [Fact]
    public async Task QueryAsync_CancelledTokenShouldRejectBeforeRetrieval()
    {
        var fixture = CreateFixture();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.QueryAsync(new Application.Dtos.KnowledgeQueryDto { Query = "部署流程" }, source.Token));

        fixture.Retriever.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 检索技能的工具名与说明是暴露给模型/MCP 的稳定契约，改名等于换了一个工具。
    /// </summary>
    [Fact]
    public void KnowledgeRetrieveSkill_NameAndDescriptionShouldBeStable()
    {
        var skill = new KnowledgeRetrieveSkill(new Mock<IKnowledgeRetriever>(MockBehavior.Strict).Object);

        Assert.Equal("knowledge_retrieve", skill.Name, StringComparer.Ordinal);
        Assert.Contains("知识库", skill.Description, StringComparison.Ordinal);
    }

    /// <summary>
    /// 技能转成的工具函数必须沿用同一套名称与说明，模型据此判断何时调用。
    /// </summary>
    [Fact]
    public void KnowledgeRetrieveSkill_AsFunctionShouldCarrySkillNameAndDescription()
    {
        var skill = new KnowledgeRetrieveSkill(new Mock<IKnowledgeRetriever>(MockBehavior.Strict).Object);

        var function = skill.AsFunction();

        Assert.Equal(skill.Name, function.Name, StringComparer.Ordinal);
        Assert.Equal(skill.Description, function.Description, StringComparer.Ordinal);
    }

    /// <summary>
    /// 检索技能只读无副作用，必须实现框架技能契约才能被技能注册表收纳并暴露为工具。
    /// </summary>
    [Fact]
    public void KnowledgeRetrieveSkill_ShouldImplementSkillContract()
    {
        Assert.True(typeof(KnowledgeRetrieveSkill).IsAssignableTo(typeof(XiHan.Framework.AI.Abstractions.Skills.IAiSkill)));
        Assert.True(typeof(KnowledgeRetrieveSkill).IsSealed);
    }

    /// <summary>
    /// 技能内部必须把 topK 收敛到 1~20：非正数回落默认 5，超过上限截到 20，
    /// 否则模型随手传一个大数就会把整个知识库灌进上下文。
    /// </summary>
    /// <param name="requestedTopK">模型传入的条数。</param>
    /// <param name="expectedTopK">收敛后交给检索器的条数。</param>
    [Theory]
    [InlineData(0, 5)]
    [InlineData(-3, 5)]
    [InlineData(1, 1)]
    [InlineData(11, 11)]
    [InlineData(20, 20)]
    [InlineData(21, 20)]
    [InlineData(int.MaxValue, 20)]
    public async Task KnowledgeRetrieveSkill_TopKShouldBeConvergedIntoOneToTwenty(int requestedTopK, int expectedTopK)
    {
        var retriever = new Mock<IKnowledgeRetriever>(MockBehavior.Strict);
        _ = retriever
            .Setup(item => item.RetrieveAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<RetrievalFilter?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateChunk()]);
        var function = new KnowledgeRetrieveSkill(retriever.Object).AsFunction();

        _ = await function.InvokeAsync(new AIFunctionArguments
        {
            ["query"] = "部署流程",
            ["topK"] = requestedTopK
        });

        retriever.Verify(
            item => item.RetrieveAsync("部署流程", expectedTopK, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 技能经 MCP 暴露时没有租户上下文，故检索不带任何过滤条件（知识库文档为平台级）；
    /// 这里锁的是"确实传了 null 过滤"而不是漏传了某个租户。
    /// </summary>
    [Fact]
    public async Task KnowledgeRetrieveSkill_ShouldRetrieveWithoutTenantFilter()
    {
        var retriever = new Mock<IKnowledgeRetriever>(MockBehavior.Strict);
        _ = retriever
            .Setup(item => item.RetrieveAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<RetrievalFilter?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var function = new KnowledgeRetrieveSkill(retriever.Object).AsFunction();

        _ = await function.InvokeAsync(new AIFunctionArguments { ["query"] = "部署流程" });

        retriever.Verify(
            item => item.RetrieveAsync("部署流程", 5, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 构造一条检索命中片段。
    /// </summary>
    /// <returns>检索片段。</returns>
    private static RetrievedChunk CreateChunk()
    {
        return new RetrievedChunk
        {
            DocumentId = "4242",
            Index = 0,
            Text = "部署步骤如下。",
            Title = "运维手册",
            Source = "manual.md",
            Score = 0.9
        };
    }

    /// <summary>
    /// 构造纯内存测试夹具：检索默认返回空集合，增强器返回固定提示词，会话服务返回固定答案。
    /// </summary>
    /// <param name="defaultTopK">RAG 配置里的默认检索条数。</param>
    /// <param name="currentTenantId">当前租户主键（null 表示平台上下文）。</param>
    /// <returns>被测服务与其依赖替身。</returns>
    private static KnowledgeQueryFixture CreateFixture(int defaultTopK = 5, long? currentTenantId = null)
    {
        var retriever = new Mock<IKnowledgeRetriever>(MockBehavior.Strict);
        _ = retriever
            .Setup(item => item.RetrieveAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<RetrievalFilter?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var augmenter = new Mock<IRagPromptAugmenter>(MockBehavior.Strict);
        _ = augmenter
            .Setup(item => item.Augment(It.IsAny<string>(), It.IsAny<IReadOnlyList<RetrievedChunk>>()))
            .Returns("增强后的提示词");

        var aiService = new Mock<IXiHanAiService>(MockBehavior.Strict);
        _ = aiService
            .Setup(item => item.ChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<XiHanChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "这是答案。")));

        var currentTenant = new Mock<ICurrentTenant>(MockBehavior.Strict);
        _ = currentTenant.SetupGet(item => item.Id).Returns(currentTenantId);

        var service = new KnowledgeQueryAppService(
            retriever.Object,
            augmenter.Object,
            aiService.Object,
            currentTenant.Object,
            Options.Create(new XiHanRagOptions { DefaultTopK = defaultTopK }));

        return new KnowledgeQueryFixture(service, retriever, augmenter, aiService);
    }

    /// <summary>
    /// 知识检索应用服务测试夹具。
    /// </summary>
    /// <param name="Service">被测应用服务。</param>
    /// <param name="Retriever">检索器替身。</param>
    /// <param name="Augmenter">提示增强器替身。</param>
    /// <param name="AiService">会话服务替身。</param>
    private sealed record KnowledgeQueryFixture(
        KnowledgeQueryAppService Service,
        Mock<IKnowledgeRetriever> Retriever,
        Mock<IRagPromptAugmenter> Augmenter,
        Mock<IXiHanAiService> AiService);
}

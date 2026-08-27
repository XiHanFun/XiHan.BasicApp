// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 消息模板领域服务测试：模板编码在「同租户 + 同渠道」维度唯一（租户可用同编码覆盖全局默认），
/// 而 TenantId=0 的平台级全局模板只有平台运维态才能改，租户态一律拒绝——这是跨租户写入的主要防线。
/// </summary>
public sealed class SaasDomainMessageTemplateTests
{
    /// <summary>
    /// 模板编码去空白后落库，且唯一性检查按「当前租户 + 渠道 + 编码」三元组进行。
    /// </summary>
    [Fact]
    public async Task Create_ShouldTrimCodeAndCheckUniquenessWithinCurrentTenantAndChannel()
    {
        var context = new TemplateTestContext(currentTenantId: 7);

        var result = await context.Service.CreateAsync(BuildCreateCommand(templateCode: "  auth-welcome  "));

        Assert.Equal("auth-welcome", result.Template.TemplateCode, StringComparer.Ordinal);
        Assert.NotNull(context.SavedTemplate);
        var predicate = context.CapturedUniquenessPredicate;
        Assert.NotNull(predicate);
        var compiled = predicate!.Compile();
        Assert.True(compiled(new SysMessageTemplate
        {
            TenantId = 7,
            Channel = MessageChannel.Email,
            TemplateCode = "auth-welcome"
        }));
        // 其它租户或其它渠道的同编码模板不应命中冲突判定
        Assert.False(compiled(new SysMessageTemplate
        {
            TenantId = 8,
            Channel = MessageChannel.Email,
            TemplateCode = "auth-welcome"
        }));
        Assert.False(compiled(new SysMessageTemplate
        {
            TenantId = 7,
            Channel = MessageChannel.Sms,
            TemplateCode = "auth-welcome"
        }));
    }

    /// <summary>
    /// 无租户上下文（平台运维态）时唯一性检查落在平台租户 0 上，不得误取 null。
    /// </summary>
    [Fact]
    public async Task Create_WithoutTenantContext_ShouldScopeUniquenessToPlatformTenantZero()
    {
        var context = new TemplateTestContext(currentTenantId: null);

        _ = await context.Service.CreateAsync(BuildCreateCommand());

        Assert.NotNull(context.CapturedUniquenessPredicate);
        var compiled = context.CapturedUniquenessPredicate!.Compile();
        Assert.True(compiled(new SysMessageTemplate
        {
            TenantId = 0,
            Channel = MessageChannel.Email,
            TemplateCode = "auth-welcome"
        }));
    }

    /// <summary>
    /// 同租户同渠道编码重复时拒绝创建。
    /// </summary>
    [Fact]
    public async Task Create_DuplicateCodeInSameChannel_ShouldThrowInvalidOperationException()
    {
        var context = new TemplateTestContext();
        context.SetupCodeAlreadyExists();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreateAsync(BuildCreateCommand()));

        Assert.Equal("同渠道下模板编码已存在。", exception.Message, StringComparer.Ordinal);
        context.Repository.Verify(
            repo => repo.AddAsync(It.IsAny<SysMessageTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 模板编码不得为空白，也不得含任何空白字符（编码是发送侧的精确匹配键）。
    /// </summary>
    [Fact]
    public async Task Create_CodeWithWhitespace_ShouldReject()
    {
        var context = new TemplateTestContext();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreateAsync(BuildCreateCommand(templateCode: "   ")));
        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreateAsync(BuildCreateCommand(templateCode: null!)));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreateAsync(BuildCreateCommand(templateCode: "auth welcome")));
        Assert.Equal("模板编码不能包含空白字符。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 模板编码长度上界为 100：100 字符通过，101 字符拒绝。
    /// </summary>
    [Fact]
    public async Task Create_CodeLengthBoundary_ShouldAccept100AndReject101()
    {
        var context = new TemplateTestContext();

        _ = await context.Service.CreateAsync(BuildCreateCommand(templateCode: new string('a', 100)));

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreateAsync(BuildCreateCommand(templateCode: new string('a', 101))));
        Assert.Contains("不能超过 100 个字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 模板名与正文必填，主题、描述、备注分别受 200 / 500 / 500 字符上限约束。
    /// </summary>
    [Fact]
    public async Task Create_TextConstraints_ShouldRejectBlankRequiredAndOverLongOptionalFields()
    {
        var context = new TemplateTestContext();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreateAsync(BuildCreateCommand(templateName: "   ")));
        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreateAsync(BuildCreateCommand(content: "   ")));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreateAsync(BuildCreateCommand(templateName: new string('n', 101))));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreateAsync(BuildCreateCommand(subject: new string('s', 201))));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreateAsync(BuildCreateCommand(description: new string('d', 501))));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreateAsync(BuildCreateCommand(remark: new string('r', 501))));
    }

    /// <summary>
    /// 渠道枚举取自位标记枚举（1/2/4/8），默认值 0 与任意未定义组合值都必须被拒绝。
    /// </summary>
    /// <param name="channelValue">渠道枚举的底层整数值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(16)]
    public async Task Create_UndefinedChannel_ShouldThrowArgumentOutOfRange(int channelValue)
    {
        var context = new TemplateTestContext();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreateAsync(BuildCreateCommand(channel: (MessageChannel)channelValue)));
    }

    /// <summary>
    /// 创建时可选文本的纯空白折叠为 null，模板名去空白，正文保持原样（正文空白是模板排版的一部分）。
    /// </summary>
    [Fact]
    public async Task Create_ShouldNormalizeOptionalTextButKeepContentIntact()
    {
        var context = new TemplateTestContext();

        _ = await context.Service.CreateAsync(BuildCreateCommand(
            templateName: "  欢迎邮件  ",
            subject: "   ",
            content: "  正文带缩进  ",
            description: "  说明  ",
            remark: "   "));

        Assert.NotNull(context.SavedTemplate);
        Assert.Equal("欢迎邮件", context.SavedTemplate!.TemplateName, StringComparer.Ordinal);
        Assert.Null(context.SavedTemplate.Subject);
        Assert.Equal("  正文带缩进  ", context.SavedTemplate.Content, StringComparer.Ordinal);
        Assert.Equal("说明", context.SavedTemplate.Description, StringComparer.Ordinal);
        Assert.Null(context.SavedTemplate.Remark);
    }

    /// <summary>
    /// 租户态下不得修改平台级全局模板（TenantId=0），否则一个租户的改动会波及所有租户。
    /// </summary>
    [Fact]
    public async Task Update_GlobalTemplateInTenantContext_ShouldReject()
    {
        var context = new TemplateTestContext(currentTenantId: 7);
        context.SetupExistingTemplate(tenantId: 0);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.UpdateAsync(BuildUpdateCommand()));

        Assert.Contains("仅平台运维态可维护", exception.Message, StringComparison.Ordinal);
        context.Repository.Verify(
            repo => repo.UpdateAsync(It.IsAny<SysMessageTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 平台运维态（当前租户为空或 0）下允许维护全局模板。
    /// </summary>
    /// <param name="currentTenantId">当前租户上下文标识。</param>
    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    public async Task Update_GlobalTemplateInPlatformContext_ShouldPass(long? currentTenantId)
    {
        var context = new TemplateTestContext(currentTenantId);
        var template = context.SetupExistingTemplate(tenantId: 0);

        var result = await context.Service.UpdateAsync(BuildUpdateCommand(templateName: "新名称"));

        Assert.Same(template, result.Template);
        Assert.Equal("新名称", template.TemplateName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 租户自有模板（TenantId 大于 0）在租户态下可正常维护。
    /// </summary>
    [Fact]
    public async Task Update_TenantOwnedTemplate_ShouldPassInTenantContext()
    {
        var context = new TemplateTestContext(currentTenantId: 7);
        var template = context.SetupExistingTemplate(tenantId: 7);

        _ = await context.Service.UpdateAsync(BuildUpdateCommand(templateName: "租户模板"));

        Assert.Equal("租户模板", template.TemplateName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新不允许改动模板编码与渠道：命令里根本没有这两个字段，实体上的值必须原样保留。
    /// </summary>
    [Fact]
    public async Task Update_ShouldNotChangeTemplateCodeOrChannel()
    {
        var context = new TemplateTestContext(currentTenantId: 7);
        var template = context.SetupExistingTemplate(tenantId: 7);
        template.TemplateCode = "auth-welcome";
        template.Channel = MessageChannel.Email;

        _ = await context.Service.UpdateAsync(BuildUpdateCommand());

        Assert.Equal("auth-welcome", template.TemplateCode, StringComparer.Ordinal);
        Assert.Equal(MessageChannel.Email, template.Channel);
    }

    /// <summary>
    /// 更新与状态变更、删除都要求正数主键，且目标不存在时给出明确拒绝。
    /// </summary>
    [Fact]
    public async Task TemplateCommands_InvalidIdOrMissingTarget_ShouldReject()
    {
        var context = new TemplateTestContext();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.UpdateAsync(BuildUpdateCommand(basicId: 0)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.DeleteAsync(0));

        _ = context.Repository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysMessageTemplate?)null);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.DeleteAsync(5));
        Assert.Equal("消息模板不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态变更命令的备注为空白时保留原备注，不得抹掉历史说明。
    /// </summary>
    [Fact]
    public async Task UpdateStatus_BlankRemark_ShouldKeepExistingRemark()
    {
        var context = new TemplateTestContext(currentTenantId: 7);
        var template = context.SetupExistingTemplate(tenantId: 7);
        template.Remark = "原备注";

        _ = await context.Service.UpdateStatusAsync(
            new MessageTemplateStatusChangeCommand(5, EnableStatus.Disabled, "   "));

        Assert.Equal(EnableStatus.Disabled, template.Status);
        Assert.Equal("原备注", template.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态变更同样受全局模板的平台运维态约束。
    /// </summary>
    [Fact]
    public async Task UpdateStatus_GlobalTemplateInTenantContext_ShouldReject()
    {
        var context = new TemplateTestContext(currentTenantId: 7);
        _ = context.SetupExistingTemplate(tenantId: 0);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.UpdateStatusAsync(
                new MessageTemplateStatusChangeCommand(5, EnableStatus.Disabled, null)));

        Assert.Contains("仅平台运维态可维护", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 状态变更拒绝未定义的启用状态枚举值。
    /// </summary>
    [Fact]
    public async Task UpdateStatus_UndefinedStatus_ShouldThrowArgumentOutOfRange()
    {
        var context = new TemplateTestContext();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.UpdateStatusAsync(
                new MessageTemplateStatusChangeCommand(5, (EnableStatus)9, null)));
    }

    /// <summary>
    /// 删除受全局模板约束，且仓储返回失败时必须显式抛出而不是静默成功。
    /// </summary>
    [Fact]
    public async Task Delete_ShouldRespectGlobalGuardAndSurfaceRepositoryFailure()
    {
        var tenantContext = new TemplateTestContext(currentTenantId: 7);
        _ = tenantContext.SetupExistingTemplate(tenantId: 0);
        var guardException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => tenantContext.Service.DeleteAsync(5));
        Assert.Contains("仅平台运维态可维护", guardException.Message, StringComparison.Ordinal);

        var platformContext = new TemplateTestContext(currentTenantId: null);
        var template = platformContext.SetupExistingTemplate(tenantId: 0);
        _ = platformContext.Repository
            .Setup(repo => repo.DeleteAsync(template, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var failureException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => platformContext.Service.DeleteAsync(5));
        Assert.Equal("消息模板删除失败。", failureException.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 命令对象为空必须抛空引用异常。
    /// </summary>
    [Fact]
    public async Task TemplateCommands_NullCommand_ShouldThrowArgumentNullException()
    {
        var context = new TemplateTestContext();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.CreateAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.UpdateAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.UpdateStatusAsync(null!));
    }

    /// <summary>
    /// 已取消的令牌必须在访问仓储之前抛出取消异常。
    /// </summary>
    [Fact]
    public async Task TemplateCommands_CancelledToken_ShouldThrowBeforeRepositoryCall()
    {
        var context = new TemplateTestContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.CreateAsync(BuildCreateCommand(), cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.UpdateAsync(BuildUpdateCommand(), cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.DeleteAsync(5, cancellation.Token));
        context.Repository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        context.Repository.Verify(
            repo => repo.AddAsync(It.IsAny<SysMessageTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static MessageTemplateCreateCommand BuildCreateCommand(
        string templateCode = "auth-welcome",
        MessageChannel channel = MessageChannel.Email,
        string templateName = "欢迎邮件",
        string? subject = "欢迎",
        string content = "正文",
        string? description = null,
        EnableStatus status = EnableStatus.Enabled,
        int sort = 0,
        string? remark = null)
    {
        return new MessageTemplateCreateCommand(
            templateCode,
            channel,
            templateName,
            subject,
            content,
            false,
            description,
            status,
            sort,
            remark);
    }

    private static MessageTemplateUpdateCommand BuildUpdateCommand(
        long basicId = 5,
        string templateName = "欢迎邮件",
        string? subject = "欢迎",
        string content = "正文",
        string? description = null,
        int sort = 0,
        string? remark = null)
    {
        return new MessageTemplateUpdateCommand(basicId, templateName, subject, content, false, description, sort, remark);
    }

    /// <summary>
    /// 消息模板领域服务的依赖装配夹具：默认编码不冲突、模板不存在，按用例逐条打开。
    /// </summary>
    private sealed class TemplateTestContext
    {
        internal TemplateTestContext(long? currentTenantId = null)
        {
            Repository = new Mock<IMessageTemplateRepository>();
            CurrentTenant = new Mock<ICurrentTenant>();
            _ = CurrentTenant.SetupGet(tenant => tenant.Id).Returns(currentTenantId);

            _ = Repository
                .Setup(repo => repo.AnyAsync(
                    It.IsAny<Expression<Func<SysMessageTemplate, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<SysMessageTemplate, bool>>, CancellationToken>(
                    (predicate, _) => CapturedUniquenessPredicate = predicate)
                .ReturnsAsync(false);
            _ = Repository
                .Setup(repo => repo.AddAsync(It.IsAny<SysMessageTemplate>(), It.IsAny<CancellationToken>()))
                .Callback<SysMessageTemplate, CancellationToken>((entity, _) => SavedTemplate = entity)
                .ReturnsAsync((SysMessageTemplate entity, CancellationToken _) => entity);
            _ = Repository
                .Setup(repo => repo.UpdateAsync(It.IsAny<SysMessageTemplate>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysMessageTemplate entity, CancellationToken _) => entity);
            _ = Repository
                .Setup(repo => repo.DeleteAsync(It.IsAny<SysMessageTemplate>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            Service = new MessageTemplateDomainService(Repository.Object, CurrentTenant.Object);
        }

        internal Expression<Func<SysMessageTemplate, bool>>? CapturedUniquenessPredicate { get; private set; }

        internal Mock<ICurrentTenant> CurrentTenant { get; }

        internal Mock<IMessageTemplateRepository> Repository { get; }

        internal SysMessageTemplate? SavedTemplate { get; private set; }

        internal MessageTemplateDomainService Service { get; }

        internal SysMessageTemplate SetupExistingTemplate(long tenantId)
        {
            var template = new SysMessageTemplate
            {
                TenantId = tenantId,
                TemplateCode = "auth-welcome",
                Channel = MessageChannel.Email,
                TemplateName = "欢迎邮件",
                Content = "正文"
            };
            SaasTestHelper.SetBasicId(template, 5);
            _ = Repository
                .Setup(repo => repo.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(template);
            return template;
        }

        internal void SetupCodeAlreadyExists()
        {
            _ = Repository
                .Setup(repo => repo.AnyAsync(
                    It.IsAny<Expression<Func<SysMessageTemplate, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }
    }
}

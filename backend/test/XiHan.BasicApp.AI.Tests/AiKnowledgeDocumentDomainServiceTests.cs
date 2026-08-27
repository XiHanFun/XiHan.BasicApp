// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.AI.Domain.DomainServices.Implementations;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Enums;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.Framework.AI.Abstractions.Rag;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// 知识文档领域不变量测试：覆盖摄取的字段规范化与状态机（Pending → Indexed/Failed）、
/// 索引失败不抛而落 Failed 的降级口径，以及"DB 与向量库必须一致"这条最贵的补偿约定。
/// </summary>
/// <remarks>摄取器与仓储全部用 Moq 替身，整套用例不连数据库、不连向量库、不发嵌入请求。</remarks>
public sealed class AiKnowledgeDocumentDomainServiceTests
{
    /// <summary>
    /// 合法命令必须逐字段规范化落到实体：可选空白字段归一为 null，原文两端空白被裁掉。
    /// </summary>
    [Fact]
    public async Task IngestAsync_ValidCommandShouldNormalizeAllFields()
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreateIngestCommand() with
        {
            Title = "  运维手册  ",
            Source = "   ",
            Text = "  第一章：部署。  ",
            EmbeddingProviderCode = "  embed  ",
            Remark = "\r\n"
        };

        var result = await fixture.Service.IngestAsync(command);

        Assert.Equal("运维手册", result.Document.Title, StringComparer.Ordinal);
        Assert.Null(result.Document.Source);
        Assert.Equal("第一章：部署。", result.Document.RawContent, StringComparer.Ordinal);
        Assert.Equal("embed", result.Document.EmbeddingProviderCode, StringComparer.Ordinal);
        Assert.Null(result.Document.Remark);
    }

    /// <summary>
    /// 落库瞬间必须是 Pending + 切片数 0：外部 I/O 未完成前不得先声称已索引。
    /// </summary>
    [Fact]
    public async Task IngestAsync_ShouldPersistPendingBeforeIndexing()
    {
        var fixture = CreateFixture();
        KnowledgeIndexStatus? statusAtInsert = null;
        int? chunkCountAtInsert = null;
        _ = fixture.Repository
            .Setup(repository => repository.AddAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysKnowledgeDocument entity, CancellationToken _) =>
            {
                statusAtInsert = entity.Status;
                chunkCountAtInsert = entity.ChunkCount;
                return AiTestHelper.SetBasicId(entity, 100);
            });

        _ = await fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand());

        Assert.Equal(KnowledgeIndexStatus.Pending, statusAtInsert);
        Assert.Equal(0, chunkCountAtInsert);
    }

    /// <summary>
    /// 索引成功必须回写实际切片数并转为 Indexed，同时清空历史失败原因。
    /// </summary>
    [Fact]
    public async Task IngestAsync_SuccessfulIndexShouldWriteChunkCountAndClearError()
    {
        var fixture = CreateFixture(chunkCount: 7);

        var result = await fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand());

        Assert.Equal(KnowledgeIndexStatus.Indexed, result.Document.Status);
        Assert.Equal(7, result.Document.ChunkCount);
        Assert.Null(result.Document.ErrorMessage);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 切片数为 0 必须判定为失败：向量库里没有任何内容，检索命中率为零，不能显示"已索引"。
    /// </summary>
    [Fact]
    public async Task IngestAsync_ZeroChunkShouldFallToFailedWithReason()
    {
        var fixture = CreateFixture(chunkCount: 0);

        var result = await fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand());

        Assert.Equal(KnowledgeIndexStatus.Failed, result.Document.Status);
        Assert.Equal(0, result.Document.ChunkCount);
        Assert.Equal("未产生任何切片。", result.Document.ErrorMessage, StringComparer.Ordinal);
    }

    /// <summary>
    /// 嵌入/向量库异常必须落 Failed 并记原因，绝不能把异常抛给调用方——摄取失败是可重建的业务状态而非 500。
    /// </summary>
    [Fact]
    public async Task IngestAsync_IngestorFailureShouldRecordFailedStatusInsteadOfThrowing()
    {
        var fixture = CreateFixture();
        _ = fixture.Ingestor
            .Setup(ingestor => ingestor.IngestAsync(It.IsAny<KnowledgeIngestRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("向量库不可达"));

        var result = await fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand());

        Assert.Equal(KnowledgeIndexStatus.Failed, result.Document.Status);
        Assert.Equal(0, result.Document.ChunkCount);
        Assert.Equal("向量库不可达", result.Document.ErrorMessage, StringComparer.Ordinal);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 失败原因必须截断到 1000 字符，否则超长堆栈会撑破 Error_Message 列。
    /// </summary>
    [Fact]
    public async Task IngestAsync_LongFailureMessageShouldBeTruncatedToColumnLength()
    {
        var fixture = CreateFixture();
        _ = fixture.Ingestor
            .Setup(ingestor => ingestor.IngestAsync(It.IsAny<KnowledgeIngestRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(new string('e', 2000)));

        var result = await fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand());

        Assert.Equal(1000, result.Document.ErrorMessage!.Length);
        Assert.Equal(new string('e', 1000), result.Document.ErrorMessage, StringComparer.Ordinal);
    }

    /// <summary>
    /// 恰好 1000 字符的失败原因不得被截断，边界不能少一格。
    /// </summary>
    [Fact]
    public async Task IngestAsync_FailureMessageAtColumnLengthShouldNotBeTruncated()
    {
        var fixture = CreateFixture();
        var message = new string('e', 1000);
        _ = fixture.Ingestor
            .Setup(ingestor => ingestor.IngestAsync(It.IsAny<KnowledgeIngestRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(message));

        var result = await fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand());

        Assert.Equal(message, result.Document.ErrorMessage, StringComparer.Ordinal);
    }

    /// <summary>
    /// 摄取途中被取消必须向上抛出，且不得把取消当成"索引失败"写进状态。
    /// </summary>
    [Fact]
    public async Task IngestAsync_CancelledDuringIndexingShouldRethrowWithoutStatusWrite()
    {
        var fixture = CreateFixture();
        _ = fixture.Ingestor
            .Setup(ingestor => ingestor.IngestAsync(It.IsAny<KnowledgeIngestRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand()));

        fixture.Repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 摄取请求必须带齐溯源信息：文档 id 用主键字符串、租户 id 用文档租户、标题/来源/嵌入 provider 原样透传。
    /// </summary>
    [Fact]
    public async Task IngestAsync_ShouldPassDocumentIdentityAndTenantToIngestor()
    {
        var fixture = CreateFixture();
        _ = fixture.Repository
            .Setup(repository => repository.AddAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysKnowledgeDocument entity, CancellationToken _) =>
                AiTestHelper.SetTenantId(AiTestHelper.SetBasicId(entity, 4242), 88));
        KnowledgeIngestRequest? captured = null;
        _ = fixture.Ingestor
            .Setup(ingestor => ingestor.IngestAsync(It.IsAny<KnowledgeIngestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeIngestRequest request, CancellationToken _) =>
            {
                captured = request;
                return 3;
            });

        _ = await fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand());

        Assert.NotNull(captured);
        Assert.Equal("4242", captured!.DocumentId, StringComparer.Ordinal);
        Assert.Equal(88, captured.TenantId);
        Assert.Equal("运维手册", captured.Title, StringComparer.Ordinal);
        Assert.Equal("manual.md", captured.Source, StringComparer.Ordinal);
        Assert.Equal("embed-code", captured.Provider, StringComparer.Ordinal);
        Assert.Equal("第一章：部署。", captured.Text, StringComparer.Ordinal);
    }

    /// <summary>
    /// 回写切片数失败时必须补偿清理刚写入的向量，否则 DB 记 0、向量库有 N，孤儿向量会持续污染检索。
    /// </summary>
    /// <remarks>这条是源码注释里点名的坑，补偿一旦被删除本用例立刻变红。</remarks>
    [Fact]
    public async Task IngestAsync_ChunkCountWriteFailureShouldCompensateVectorCleanup()
    {
        var fixture = CreateFixture(chunkCount: 5);
        _ = fixture.Repository
            .Setup(repository => repository.UpdateAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("并发冲突"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand()));

        Assert.Equal("并发冲突", exception.Message, StringComparer.Ordinal);
        fixture.Ingestor.Verify(
            ingestor => ingestor.RemoveDocumentAsync("100", 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 补偿清理必须用 <see cref="CancellationToken.None"/>：调用方已取消时仍要把孤儿向量清干净。
    /// </summary>
    [Fact]
    public async Task IngestAsync_CompensationShouldNotHonourCallerCancellation()
    {
        var fixture = CreateFixture(chunkCount: 5);
        _ = fixture.Repository
            .Setup(repository => repository.UpdateAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("并发冲突"));
        using var source = new CancellationTokenSource();

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand(), source.Token));

        fixture.Ingestor.Verify(
            ingestor => ingestor.RemoveDocumentAsync("100", 5, CancellationToken.None),
            Times.Once);
    }

    /// <summary>
    /// 一条向量都没写进去时不得发起补偿清理（无谓地按 0 条去删只会白跑一趟外部调用）。
    /// </summary>
    [Fact]
    public async Task IngestAsync_WriteFailureWithoutWrittenChunksShouldNotCompensate()
    {
        var fixture = CreateFixture(chunkCount: 0);
        _ = fixture.Repository
            .Setup(repository => repository.UpdateAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("并发冲突"));

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand()));

        fixture.Ingestor.Verify(
            ingestor => ingestor.RemoveDocumentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 补偿清理自身再失败也必须把原始的回写异常抛给调用方，不得被清理异常掩盖。
    /// </summary>
    [Fact]
    public async Task IngestAsync_CompensationFailureShouldStillSurfaceOriginalWriteError()
    {
        var fixture = CreateFixture(chunkCount: 5);
        _ = fixture.Repository
            .Setup(repository => repository.UpdateAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("并发冲突"));
        _ = fixture.Ingestor
            .Setup(ingestor => ingestor.RemoveDocumentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("清理超时"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand()));

        Assert.Equal("并发冲突", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 空命令必须在任何仓储调用前被拒。
    /// </summary>
    [Fact]
    public async Task IngestAsync_NullCommandShouldReject()
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.IngestAsync(null!));

        fixture.Repository.VerifyNoOtherCalls();
        fixture.Ingestor.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 已取消的摄取请求不得产生任何落库或向量库副作用。
    /// </summary>
    [Fact]
    public async Task IngestAsync_CancelledTokenShouldRejectBeforeRepository()
    {
        var fixture = CreateFixture();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand(), source.Token));

        fixture.Repository.VerifyNoOtherCalls();
        fixture.Ingestor.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 非法来源类型必须拒绝，否则脏枚举落库会让前端来源筛选失效。
    /// </summary>
    [Fact]
    public async Task IngestAsync_UndefinedSourceTypeShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand() with { SourceType = (KnowledgeSourceType)99 }));

        Assert.Contains("枚举值无效", exception.Message, StringComparison.Ordinal);
        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 原文为空必须拒绝，且抛业务异常——空文档进库只会产出零切片的垃圾记录。
    /// </summary>
    /// <param name="text">空白原文输入。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task IngestAsync_BlankTextShouldReject(string? text)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand() with { Text = text! }));

        Assert.Equal("文档内容不能为空。", exception.Message, StringComparer.Ordinal);
        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 标题为空必须拒绝（列声明 IsNullable=false，且引用溯源要靠标题展示）。
    /// </summary>
    /// <param name="title">空白标题输入。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IngestAsync_BlankTitleShouldReject(string? title)
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand() with { Title = title! }));
    }

    /// <summary>
    /// 各字段的长度上限必须与实体列长度一一对应，越界即拒绝。
    /// </summary>
    /// <param name="fieldName">被测字段名。</param>
    /// <param name="maxLength">该字段的列长度上限。</param>
    [Theory]
    [InlineData("Title", 200)]
    [InlineData("Source", 500)]
    [InlineData("EmbeddingProviderCode", 100)]
    [InlineData("Remark", 500)]
    public async Task IngestAsync_FieldOverMaxLengthShouldReject(string fieldName, int maxLength)
    {
        var fixture = CreateFixture();
        var over = new string('x', maxLength + 1);
        var command = AiTestHelper.CreateIngestCommand();
        command = fieldName switch
        {
            "Title" => command with { Title = over },
            "Source" => command with { Source = over },
            "EmbeddingProviderCode" => command with { EmbeddingProviderCode = over },
            _ => command with { Remark = over }
        };

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.IngestAsync(command));
    }

    /// <summary>
    /// 原文不受长度上限约束（列为 BigString），超长文档必须能落库。
    /// </summary>
    [Fact]
    public async Task IngestAsync_VeryLongTextShouldPass()
    {
        var fixture = CreateFixture();
        var text = new string('t', 100_000);

        var result = await fixture.Service.IngestAsync(AiTestHelper.CreateIngestCommand() with { Text = text });

        Assert.Equal(text, result.Document.RawContent, StringComparer.Ordinal);
    }

    /// <summary>
    /// 重建索引必须先按当前切片数清旧向量再重新摄取，顺序反了会把新写的向量一起删掉。
    /// </summary>
    [Fact]
    public async Task ReindexAsync_ShouldRemoveOldVectorsBeforeReingesting()
    {
        var existing = AiTestHelper.CreateDocument(7, chunkCount: 4);
        var fixture = CreateFixture(existing, chunkCount: 6);
        var callOrder = new List<string>();
        _ = fixture.Ingestor
            .Setup(ingestor => ingestor.RemoveDocumentAsync("7", 4, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("remove"))
            .Returns(Task.CompletedTask);
        _ = fixture.Ingestor
            .Setup(ingestor => ingestor.IngestAsync(It.IsAny<KnowledgeIngestRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("ingest"))
            .ReturnsAsync(6);

        var result = await fixture.Service.ReindexAsync(7);

        Assert.Equal(["remove", "ingest"], callOrder, StringComparer.Ordinal);
        Assert.Equal(6, result.Document.ChunkCount);
        Assert.Equal(KnowledgeIndexStatus.Indexed, result.Document.Status);
    }

    /// <summary>
    /// 重建索引必须用库里的原文，而不是要求调用方重新上传。
    /// </summary>
    [Fact]
    public async Task ReindexAsync_ShouldReuseStoredRawContent()
    {
        var existing = AiTestHelper.CreateDocument(7);
        existing.RawContent = "库里的原文。";
        var fixture = CreateFixture(existing);
        KnowledgeIngestRequest? captured = null;
        _ = fixture.Ingestor
            .Setup(ingestor => ingestor.IngestAsync(It.IsAny<KnowledgeIngestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeIngestRequest request, CancellationToken _) =>
            {
                captured = request;
                return 2;
            });

        _ = await fixture.Service.ReindexAsync(7);

        Assert.Equal("库里的原文。", captured!.Text, StringComparer.Ordinal);
    }

    /// <summary>
    /// 重建索引失败同样落 Failed 而不抛出，让文档停在可再次重建的状态。
    /// </summary>
    [Fact]
    public async Task ReindexAsync_IngestorFailureShouldRecordFailedStatus()
    {
        var existing = AiTestHelper.CreateDocument(7);
        var fixture = CreateFixture(existing);
        _ = fixture.Ingestor
            .Setup(ingestor => ingestor.IngestAsync(It.IsAny<KnowledgeIngestRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("嵌入服务 500"));

        var result = await fixture.Service.ReindexAsync(7);

        Assert.Equal(KnowledgeIndexStatus.Failed, result.Document.Status);
        Assert.Equal("嵌入服务 500", result.Document.ErrorMessage, StringComparer.Ordinal);
    }

    /// <summary>
    /// 重建索引的非法主键必须在查库前被拒。
    /// </summary>
    /// <param name="id">非法主键值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReindexAsync_NonPositiveIdShouldReject(long id)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.ReindexAsync(id));

        Assert.Contains("知识文档主键必须大于 0", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.Ingestor.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 重建不存在的文档必须拒绝，且不得对向量库发起任何清理。
    /// </summary>
    [Fact]
    public async Task ReindexAsync_MissingDocumentShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ReindexAsync(7));

        Assert.Equal("知识文档不存在。", exception.Message, StringComparer.Ordinal);
        fixture.Ingestor.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 删除必须先清向量再软删元信息，否则元信息没了就再也拿不到切片数去清向量。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldRemoveVectorsBeforeSoftDeletingDocument()
    {
        var existing = AiTestHelper.CreateDocument(7, chunkCount: 4);
        var fixture = CreateFixture(existing);
        var callOrder = new List<string>();
        _ = fixture.Ingestor
            .Setup(ingestor => ingestor.RemoveDocumentAsync("7", 4, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("remove"))
            .Returns(Task.CompletedTask);
        _ = fixture.Repository
            .Setup(repository => repository.DeleteAsync(existing, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("delete"))
            .ReturnsAsync(true);

        await fixture.Service.DeleteAsync(7);

        Assert.Equal(["remove", "delete"], callOrder, StringComparer.Ordinal);
    }

    /// <summary>
    /// 仓储删除返回 false 必须抛出，否则删除失败被当成成功会让前端列表与库不一致。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_RepositoryFailureShouldReject()
    {
        var existing = AiTestHelper.CreateDocument(7);
        var fixture = CreateFixture(existing);
        _ = fixture.Repository
            .Setup(repository => repository.DeleteAsync(existing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteAsync(7));

        Assert.Equal("知识文档删除失败。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 删除不存在的文档必须拒绝，且不得对向量库发起任何清理。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_MissingDocumentShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteAsync(7));

        Assert.Equal("知识文档不存在。", exception.Message, StringComparer.Ordinal);
        fixture.Ingestor.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 删除的非法主键必须在查库前被拒。
    /// </summary>
    /// <param name="id">非法主键值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task DeleteAsync_NonPositiveIdShouldReject(long id)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.DeleteAsync(id));

        Assert.Contains("知识文档主键必须大于 0", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 重建与删除同样要在已取消时立即抛出，不得触碰仓储与向量库。
    /// </summary>
    [Fact]
    public async Task ReindexAndDelete_CancelledTokenShouldRejectBeforeAnyIo()
    {
        var fixture = CreateFixture(AiTestHelper.CreateDocument(7));
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.ReindexAsync(7, source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.DeleteAsync(7, source.Token));

        fixture.Repository.VerifyNoOtherCalls();
        fixture.Ingestor.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 取消令牌必须原样透传给仓储与摄取器的每一次调用。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldForwardCancellationToken()
    {
        var existing = AiTestHelper.CreateDocument(7, chunkCount: 4);
        var fixture = CreateFixture(existing);
        using var source = new CancellationTokenSource();

        await fixture.Service.DeleteAsync(7, source.Token);

        fixture.Repository.Verify(repository => repository.GetByIdAsync(7, source.Token), Times.Once);
        fixture.Ingestor.Verify(ingestor => ingestor.RemoveDocumentAsync("7", 4, source.Token), Times.Once);
        fixture.Repository.Verify(repository => repository.DeleteAsync(existing, source.Token), Times.Once);
    }

    /// <summary>
    /// 构造纯内存测试夹具：仓储与摄取器均为替身，摄取默认成功并返回指定切片数。
    /// </summary>
    /// <param name="existing">GetByIdAsync 命中的既有文档（null 表示查不到）。</param>
    /// <param name="chunkCount">摄取器默认返回的切片数。</param>
    /// <returns>被测服务与其依赖替身。</returns>
    private static KnowledgeFixture CreateFixture(SysKnowledgeDocument? existing = null, int chunkCount = 3)
    {
        var repository = new Mock<IKnowledgeDocumentRepository>(MockBehavior.Strict);
        _ = repository
            .Setup(item => item.AddAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysKnowledgeDocument entity, CancellationToken _) => AiTestHelper.SetBasicId(entity, 100));
        _ = repository
            .Setup(item => item.UpdateAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysKnowledgeDocument entity, CancellationToken _) => entity);
        _ = repository
            .Setup(item => item.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _ = repository
            .Setup(item => item.DeleteAsync(It.IsAny<SysKnowledgeDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ingestor = new Mock<IKnowledgeIngestor>(MockBehavior.Strict);
        _ = ingestor
            .Setup(item => item.IngestAsync(It.IsAny<KnowledgeIngestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunkCount);
        _ = ingestor
            .Setup(item => item.RemoveDocumentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new KnowledgeDocumentDomainService(
            repository.Object,
            ingestor.Object,
            NullLogger<KnowledgeDocumentDomainService>.Instance);

        return new KnowledgeFixture(service, repository, ingestor);
    }

    /// <summary>
    /// 知识文档领域服务测试夹具。
    /// </summary>
    /// <param name="Service">被测领域服务。</param>
    /// <param name="Repository">仓储替身。</param>
    /// <param name="Ingestor">知识摄取器替身。</param>
    private sealed record KnowledgeFixture(
        KnowledgeDocumentDomainService Service,
        Mock<IKnowledgeDocumentRepository> Repository,
        Mock<IKnowledgeIngestor> Ingestor);
}

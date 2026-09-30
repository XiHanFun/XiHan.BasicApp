// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Exporting;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Security.Claims;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 导出快照解析失败测试：查询条件或导出列解析不了，任务按失败收口并写明是哪一份快照坏了。
/// </summary>
/// <remarks>
/// 回归锚点：
/// <list type="bullet">
/// <item>基类曾在 <c>JsonException</c> 时返回空查询，空查询等于不加筛选，按条件导出的任务会悄悄导出全量，且显示成功；</item>
/// <item>执行器曾把解析不了的列快照当成空列表，失败原因写成「导出列为空」，排查时找错方向。</item>
/// </list>
/// </remarks>
public sealed class ExportSnapshotFailureTests
{
    private const long UserId = 42;

    private const string ValidFields = """[{ "Key": "name", "Title": "名称" }]""";

    /// <summary>
    /// 解析不了的快照：抛出「查询条件无法解析」，一次查询都不发
    /// </summary>
    /// <param name="snapshot">查询快照</param>
    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "ownerId": "abc" }""")]
    [InlineData("null")]
    public async Task ReadRows_UnparsableSnapshot_ShouldThrowWithoutQuerying(string snapshot)
    {
        var provider = new RecordingProvider();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DrainAsync(provider.ReadRowsAsync(Context(snapshot))));

        Assert.Contains("查询条件无法解析", exception.Message);
        Assert.Empty(provider.Queries);
    }

    /// <summary>
    /// 字段类型对不上时指出出错位置，便于对照快照排查
    /// </summary>
    [Fact]
    public async Task ReadRows_TypeMismatch_ShouldReportJsonPath()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DrainAsync(new RecordingProvider().ReadRowsAsync(Context("""{ "keyword": "甲", "ownerId": "abc" }"""))));

        Assert.Contains("$.ownerId", exception.Message);
    }

    /// <summary>
    /// 未带快照即不加筛选：照常查询
    /// </summary>
    /// <param name="snapshot">查询快照</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReadRows_NoSnapshot_ShouldQueryWithoutFilters(string? snapshot)
    {
        var provider = new RecordingProvider();

        await DrainAsync(provider.ReadRowsAsync(Context(snapshot)));

        var query = Assert.Single(provider.Queries);
        Assert.Null(query.Keyword);
        Assert.Null(query.OwnerId);
    }

    /// <summary>
    /// 能解析的快照原样带进查询
    /// </summary>
    [Fact]
    public async Task ReadRows_ValidSnapshot_ShouldPassFiltersThrough()
    {
        var provider = new RecordingProvider();

        await DrainAsync(provider.ReadRowsAsync(Context("""{ "keyword": "甲", "ownerId": "1234567890123456789" }""")));

        var query = Assert.Single(provider.Queries);
        Assert.Equal("甲", query.Keyword);
        Assert.Equal(1234567890123456789L, query.OwnerId);
    }

    /// <summary>
    /// 经执行器跑：查询条件解析不了，任务按失败收口并写明原因，不产出文件、不标成功
    /// </summary>
    [Fact]
    public async Task Execute_UnparsableQuerySnapshot_ShouldMarkTaskFailed()
    {
        var repository = new Mock<IExportTaskRepository>();
        var fileTransfer = new Mock<IFileTransferService>();
        var executor = CreateExecutor(new RecordingProvider(), repository, fileTransfer);

        await executor.ExecuteAsync(CreateTask("{ not json", ValidFields));

        VerifyFailedOnly(repository, fileTransfer, "查询条件无法解析");
    }

    /// <summary>
    /// 经执行器跑：导出列解析不了，失败原因是「导出列无法解析」而不是「导出列为空」，也不去拉数
    /// </summary>
    /// <param name="fieldsSnapshot">导出列快照</param>
    [Theory]
    [InlineData("{ not json")]
    [InlineData("""[{ "Key": 1, "Title": "名称" }]""")]
    [InlineData("null")]
    public async Task Execute_UnparsableFieldsSnapshot_ShouldMarkTaskFailed(string fieldsSnapshot)
    {
        var repository = new Mock<IExportTaskRepository>();
        var fileTransfer = new Mock<IFileTransferService>();
        var provider = new RecordingProvider();
        var executor = CreateExecutor(provider, repository, fileTransfer);

        await executor.ExecuteAsync(CreateTask(null, fieldsSnapshot));

        VerifyFailedOnly(repository, fileTransfer, "导出列无法解析");
        Assert.Empty(provider.Queries);
    }

    /// <summary>
    /// 列快照本身是空列表：仍按「导出列为空」失败
    /// </summary>
    [Fact]
    public async Task Execute_EmptyFieldsSnapshot_ShouldReportEmptyColumns()
    {
        var repository = new Mock<IExportTaskRepository>();
        var fileTransfer = new Mock<IFileTransferService>();
        var executor = CreateExecutor(new RecordingProvider(), repository, fileTransfer);

        await executor.ExecuteAsync(CreateTask(null, "[]"));

        VerifyFailedOnly(repository, fileTransfer, "导出列为空");
    }

    private static void VerifyFailedOnly(Mock<IExportTaskRepository> repository, Mock<IFileTransferService> fileTransfer, string reason)
    {
        repository.Verify(
            repo => repo.MarkFailedAsync(5, It.Is<string>(message => message.Contains(reason)), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            repo => repo.MarkSuccessAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fileTransfer.Verify(service => service.UploadAsync(It.IsAny<FileUploadDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ExportContext Context(string? snapshot)
    {
        return new ExportContext
        {
            FieldSecurity = new Mock<IFieldSecurityService>().Object,
            BusinessType = RecordingProvider.Type,
            Scope = ExportScope.SearchResult,
            QuerySnapshot = snapshot,
            Columns = [new ExportColumnDto { Key = "name", Title = "名称" }],
            UserId = UserId
        };
    }

    private static async Task DrainAsync(IAsyncEnumerable<IReadOnlyList<string>> rows)
    {
        await foreach (var _ in rows)
        {
        }
    }

    private static ExportExecutor CreateExecutor(IExportProvider provider, Mock<IExportTaskRepository> repository, Mock<IFileTransferService> fileTransfer)
    {
        var snapshot = new Mock<IAuthorizationSnapshotQueryService>();
        _ = snapshot
            .Setup(service => service.BuildAsync(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationSnapshot([], [], [], []));

        var permissionChecker = new Mock<IPermissionChecker>();
        _ = permissionChecker
            .Setup(checker => checker.IsGrantedAsync(UserId.ToString(), RecordingProvider.Permission, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return new ExportExecutor(
            [provider],
            [new CsvExportWriter()],
            repository.Object,
            fileTransfer.Object,
            new TestCurrentTenant(),
            new Mock<ICurrentPrincipalAccessor>().Object,
            permissionChecker.Object,
            new Mock<IUserTaskProgressNotifier>().Object,
            NullLogger<ExportExecutor>.Instance,
            snapshot.Object,
            new Mock<ITenantRepository>().Object,
            new Mock<IFieldSecurityService>().Object);
    }

    private static SysExportTask CreateTask(string? querySnapshot, string fieldsSnapshot)
    {
        // 平台发起（TenantId = 0）不走租户可用性检查，直接到权限与拉数
        var task = new SysExportTask
        {
            TenantId = 0,
            BusinessType = RecordingProvider.Type,
            TaskName = "示例导出",
            Scope = ExportScope.SearchResult,
            Format = ExportFormat.Csv,
            QuerySnapshot = querySnapshot,
            FieldsSnapshot = fieldsSnapshot
        };
        SaasTestHelper.SetBasicId(task, 5);
        typeof(SysExportTask).GetProperty(nameof(SysExportTask.CreatedId))!.SetValue(task, UserId);
        return task;
    }

    /// <summary>
    /// 示例分页查询入参
    /// </summary>
    public sealed class SampleQueryDto : BasicAppPRDto
    {
        /// <summary>关键词。</summary>
        public string? Keyword { get; set; }

        /// <summary>负责人。</summary>
        public long? OwnerId { get; set; }
    }

    /// <summary>
    /// 示例列表行
    /// </summary>
    public sealed class SampleRowDto
    {
        /// <summary>名称。</summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 记下每次查询入参的示例 Provider（查询恒返回空页）
    /// </summary>
    private sealed class RecordingProvider : QueryServiceExportProviderBase<SampleQueryDto, SampleRowDto>
    {
        public const string Type = "test.sample";

        public const string Permission = "test_sample:export";

        public List<SampleQueryDto> Queries { get; } = [];

        public override string BusinessType => Type;

        public override string RequiredPermission => Permission;

        protected override Task<PageResultDtoBase<SampleRowDto>> QueryPageAsync(SampleQueryDto query, CancellationToken cancellationToken)
        {
            Queries.Add(query);
            return Task.FromResult(new PageResultDtoBase<SampleRowDto>([], query.Page.PageIndex, query.Page.PageSize, 0));
        }
    }
}

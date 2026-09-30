// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Exporting;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 导出查询快照反序列化测试。
/// </summary>
/// <remarks>
/// 快照是页面查询入参原样序列化的结果，与线上报文同形：枚举按成员名传、long 按字符串传。
/// 反序列化失败时导出任务会以「查询条件无法解析」失败，所以这里把报文形态逐项钉住，正常报文都要能还原。
/// </remarks>
public sealed class SaasAppExportQuerySnapshotTests
{
    /// <summary>
    /// 示例状态枚举。
    /// </summary>
    public enum SampleStatus
    {
        /// <summary>停用。</summary>
        Disabled = 0,

        /// <summary>启用。</summary>
        Enabled = 1
    }

    /// <summary>
    /// 示例分页查询入参。
    /// </summary>
    public sealed class SampleQueryDto : BasicAppPRDto
    {
        /// <summary>关键词。</summary>
        public string? Keyword { get; set; }

        /// <summary>状态。</summary>
        public SampleStatus? Status { get; set; }

        /// <summary>负责人。</summary>
        public long? OwnerId { get; set; }
    }

    /// <summary>
    /// 暴露反序列化入口的示例 Provider。
    /// </summary>
    private sealed class SampleProvider : QueryServiceExportProviderBase<SampleQueryDto, SampleQueryDto>
    {
        public override string BusinessType => "test.sample";

        public override string RequiredPermission => "test_sample:export";

        public SampleQueryDto Parse(string? snapshot) => Deserialize(snapshot);

        protected override Task<PageResultDtoBase<SampleQueryDto>> QueryPageAsync(SampleQueryDto query, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 枚举按成员名、long 按字符串的快照原样还原，分页与排序一并保留。
    /// </summary>
    [Fact]
    public void Deserialize_WireShapedSnapshot_KeepsEveryFilter()
    {
        const string snapshot = """
            {
              "conditions": { "sorts": [{ "field": "createdTime", "direction": 1001, "priority": 0 }], "filters": [] },
              "page": { "pageIndex": 3, "pageSize": 50 },
              "keyword": "甲",
              "status": "Enabled",
              "ownerId": "1234567890123456789"
            }
            """;

        var query = new SampleProvider().Parse(snapshot);

        Assert.Equal("甲", query.Keyword);
        Assert.Equal(SampleStatus.Enabled, query.Status);
        Assert.Equal(1234567890123456789L, query.OwnerId);
        Assert.Equal(3, query.Page.PageIndex);
        Assert.Equal(50, query.Page.PageSize);
        var sort = Assert.Single(query.Conditions.Sorts);
        Assert.Equal(SortDirection.Descending, sort.Direction);
    }

    /// <summary>
    /// 旧快照里的数值枚举照样认。
    /// </summary>
    [Fact]
    public void Deserialize_NumericEnum_StillAccepted()
    {
        var query = new SampleProvider().Parse("""{ "status": 0 }""");

        Assert.Equal(SampleStatus.Disabled, query.Status);
    }
}

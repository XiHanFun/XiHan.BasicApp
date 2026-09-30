// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Moq;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 按字典编码取下拉选项的测试。
/// </summary>
/// <remarks>
/// 这是业务表单下拉的选项来源（代码生成的字典下拉、列表显示与导入都靠它）：
/// 值取字典项编码、树形字典按深度优先展平并带上级编码、停用项照样回来但标成停用；
/// 字典停用时不暴露任何项，字典不存在时如实报错而不是给个空下拉。
/// </remarks>
public sealed class DictOptionsQueryTests
{
    private readonly Mock<IDictRepository> _dictRepository = new();

    private readonly Mock<IDictItemRepository> _dictItemRepository = new();

    private readonly Mock<IDistributedCache<SaasDictItemTreeCacheItem, string>> _cache = new();

    /// <summary>
    /// 构造函数：缓存直接回调工厂，走真实的取数与组树
    /// </summary>
    public DictOptionsQueryTests()
    {
        _cache
            .Setup(cache => cache.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<SaasDictItemTreeCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Func<Task<SaasDictItemTreeCacheItem>> factory, Func<DistributedCacheEntryOptions>? _, bool? _, bool _, CancellationToken _) => factory()!);
    }

    /// <summary>
    /// 树形字典展平：值取编码、文本取名称、按排序深度优先、子项带上级编码，停用项标成停用。
    /// </summary>
    [Fact]
    public async Task GetDictOptionsAsync_ShouldFlattenTreeWithParentAndDisabledFlags()
    {
        GivenDict("demo_region", EnableStatus.Enabled);
        GivenItems(
            Item(11, "shanghai", "上海", parentId: 1, sort: 1),
            Item(1, "east", "华东", sort: 1, isDefault: true),
            Item(2, "north", "华北", sort: 2),
            Item(21, "beijing", "北京", parentId: 2, sort: 1, status: EnableStatus.Disabled));

        var options = await CreateService().GetDictOptionsAsync(" demo_region ");

        Assert.Equal(["east", "shanghai", "north", "beijing"], options.Select(option => option.Value));
        Assert.Equal(["华东", "上海", "华北", "北京"], options.Select(option => option.Label));
        Assert.Equal([null, "east", null, "north"], options.Select(option => option.ParentValue));
        Assert.True(options[0].IsDefault);
        Assert.Equal([false, false, false, true], options.Select(option => option.Disabled));
    }

    /// <summary>
    /// 停用的字典不暴露任何项，也不去读字典项。
    /// </summary>
    [Fact]
    public async Task GetDictOptionsAsync_DisabledDictShouldExposeNothing()
    {
        GivenDict("demo_customer_level", EnableStatus.Disabled);

        var options = await CreateService().GetDictOptionsAsync("demo_customer_level");

        Assert.Empty(options);
        _dictItemRepository.Verify(
            repository => repository.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 字典不存在时如实报错：空下拉会让配置写错的字典码无声无息。
    /// </summary>
    [Fact]
    public async Task GetDictOptionsAsync_UnknownDictShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().GetDictOptionsAsync("no_such_dict"));

        Assert.Contains("no_such_dict", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 空字典码直接拒绝。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetDictOptionsAsync_BlankCodeShouldThrow(string dictCode)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService().GetDictOptionsAsync(dictCode));
    }

    private DictQueryService CreateService()
    {
        var currentTenant = new Mock<ICurrentTenant>();
        return new DictQueryService(
            _dictRepository.Object,
            _dictItemRepository.Object,
            currentTenant.Object,
            _cache.Object,
            Mock.Of<IFieldSecurityService>());
    }

    private void GivenDict(string code, EnableStatus status)
    {
        var dict = new SysDict { DictCode = code, DictName = code, DictType = "business", Status = status };
        SaasTestHelper.SetBasicId(dict, 7);
        _dictRepository
            .Setup(repository => repository.GetByCodeAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dict);
    }

    private void GivenItems(params SysDictItem[] items)
    {
        _dictItemRepository
            .Setup(repository => repository.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageResultDtoBase<SysDictItem>([.. items], 1, 5000, items.Length));
    }

    private static SysDictItem Item(long id, string code, string name, long? parentId = null, int sort = 0, bool isDefault = false, EnableStatus status = EnableStatus.Enabled)
    {
        var item = new SysDictItem { DictId = 7, ParentId = parentId, ItemCode = code, ItemName = name, Sort = sort, IsDefault = isDefault, Status = status };
        SaasTestHelper.SetBasicId(item, id);
        return item;
    }
}

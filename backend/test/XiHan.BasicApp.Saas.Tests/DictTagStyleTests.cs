// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Moq;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>字典样式保留元数据、拒绝CSS注入；派生字段遵守原元数据权限。</summary>
public sealed class DictTagStyleTests
{
    /// <summary>新增样式属性不能绕过原Metadata字段只读规则；校验失败前不调用领域写操作。</summary>
    [Theory]
    [InlineData("tone")]
    [InlineData("icon")]
    [InlineData("hidden")]
    public async Task TagStyleMustRespectMetadataFieldPermission(string change)
    {
        var fields = new Mock<IFieldSecurityService>();
        fields.Setup(x => x.ResolveAsync(typeof(SysDictItem), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, EffectiveFieldRule> { [nameof(SysDictItem.Metadata)] = new() { FieldName = nameof(SysDictItem.Metadata), IsEditable = false } });
        var rows = new Mock<IDictItemRepository>();
        rows.Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new SysDictItem { Metadata = "{\"tagStyle\":{\"tone\":\"info\"}}" });
        var domain = new Mock<IDictDomainService>();
        var service = new DictAppService(domain.Object, Mock.Of<ISaasCacheInvalidator>(), fields.Object, rows.Object);
        var style = change switch { "icon" => new DictTagStyle { Tone = "info", Icon = "ep:check" }, "hidden" => new() { Tone = "info", ShowIcon = false }, _ => new() { Tone = "warning" } };
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDictItemAsync(new DictItemUpdateDto { BasicId = 2, TagStyle = style }));
        domain.Verify(x => x.UpdateDictItemAsync(It.IsAny<DictItemUpdateCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal("{\"tagStyle\":{\"tone\":\"info\"}}", DictTagStyles.Merge("{\"tagStyle\":{\"tone\":\"info\"}}", new() { Tone = "info" }));
    }

    /// <summary>样式只占独立节点，清除样式不清空用户原有扩展信息。</summary>
    [Fact]
    public void MergeAndResetPreserveUnrelatedMetadata()
    {
        var raw = DictTagStyles.Merge("{\"external\":{\"id\":9007199254740993}}", new() { Tone = "warning", BackgroundColor = "#aabbcc", TextColor = "#112233" });
        Assert.Equal("#AABBCC", DictTagStyles.Read(raw)!.BackgroundColor);
        using var parsed = JsonDocument.Parse(raw!);
        Assert.Equal(9007199254740993, parsed.RootElement.GetProperty("external").GetProperty("id").GetInt64());
        var reset = DictTagStyles.Merge(raw, new());
        Assert.Null(DictTagStyles.Read(reset));
        Assert.Contains("9007199254740993", reset);
        Assert.Equal(raw, DictTagStyles.Merge(raw, null));
    }

    /// <summary>任意CSS和无效语义色不能持久化；历史非对象元数据不会被强行清空。</summary>
    [Theory]
    [InlineData("#123456\n")]
    [InlineData("red")]
    [InlineData("#fff")]
    [InlineData("url(https://example.test/x)")]
    [InlineData("var(--x)")]
    [InlineData("#123456;display:none")]
    public void RejectsNonLiteralColors(string input) => Assert.Throws<ValidationException>(() => DictTagStyles.Merge(null, new() { BackgroundColor = input }));

    /// <summary>图标及隐藏开关可独立保存，关闭不丢选择；清除样式仍保留其他元数据。</summary>
    [Theory]
    [InlineData(null, "ep:check")]
    [InlineData(false, null)]
    [InlineData(false, "carbon:checkmark")]
    [InlineData(true, "lucide:check")]
    public void IconPreferencesRoundTripWithoutColors(bool? showIcon, string? icon)
    {
        var style = new DictTagStyle { ShowIcon = showIcon, Icon = icon };
        var metadata = DictTagStyles.Merge("{\"other\":9007199254740993}", style);
        Assert.Equal(style, DictTagStyles.Read(metadata));
        Assert.Equal(metadata, DictTagStyles.Merge(metadata, style));
        Assert.Contains("9007199254740993", DictTagStyles.Merge(metadata, new()));
        Assert.Null(DictTagStyles.Read(DictTagStyles.Merge(metadata, new())));
    }

    /// <summary>限制到本地选择器图标ID，不存储远程URL、SVG、提供者或过长内容。</summary>
    [Theory]
    [InlineData("")]
    [InlineData("lucide:check\n")]
    [InlineData("check")]
    [InlineData("lucide:check ")]
    [InlineData("https://example.test/icon.svg")]
    [InlineData("<svg onload='alert(1)'/>")]
    [InlineData("@remote:lucide:check")]
    [InlineData("unknown:check")]
    [InlineData("lucide:CHECK")]
    public void RejectsInvalidIconIdentifiers(string icon) => Assert.Throws<ValidationException>(() => DictTagStyles.Merge(null, new() { Icon = icon }));

    /// <summary>旧数据仍采用默认图标；显式null不会变成隐藏；超长输入与非布尔开关被拒绝。</summary>
    [Fact]
    public void OldMetadataAndInputBoundariesRemainExplicit()
    {
        var old = DictTagStyles.Read("{\"tagStyle\":{\"tone\":\"success\"}}")!;
        Assert.Null(old.Icon);
        Assert.Null(old.ShowIcon);
        Assert.Throws<ValidationException>(() => DictTagStyles.Validate(new() { Icon = "lucide:" + new string('a', 160) }));
        Assert.Throws<JsonException>(() => DictTagStyles.Read("{\"tagStyle\":{\"showIcon\":\"false\"}}"));
    }

    /// <summary>列表、树、详情均提供同一显示样式，其他元数据仅留在原详情权限中。</summary>
    [Fact]
    public void AllDisplayDtosShareValidatedStyle()
    {
        var item = new SysDictItem { Metadata = DictTagStyles.Merge("{\"privateExtension\":42}", new() { Tone = "info", ShowIcon = false, Icon = "ep:check" }) };
        Assert.Equal("info", DictApplicationMapper.ToItemListItemDto(item).TagStyle!.Tone);
        Assert.Equal("info", DictApplicationMapper.ToItemTreeNodeDto(item).TagStyle!.Tone);
        Assert.Equal("info", DictApplicationMapper.ToItemDetailDto(item).TagStyle!.Tone);
        foreach (var style in new[] { DictApplicationMapper.ToItemListItemDto(item).TagStyle, DictApplicationMapper.ToItemTreeNodeDto(item).TagStyle, DictApplicationMapper.ToItemDetailDto(item).TagStyle })
        {
            Assert.False(style!.ShowIcon);
            Assert.Equal("ep:check", style.Icon);
        }
        Assert.Throws<ValidationException>(() => DictTagStyles.Merge("[]", new() { Tone = "success" }));
        Assert.Throws<ValidationException>(() => DictTagStyles.Validate(new() { Tone = "rainbow" }));
    }


    /// <summary>拒绝陈旧或缺失的编辑版本，正确版本合并样式时保留无关大整数节点。</summary>
    [Fact]
    public async Task StyleUpdateRequiresCurrentVersionAndPreservesMetadata()
    {
        var dict = new SysDict { TenantId = 0, Status = EnableStatus.Enabled };
        var item = new SysDictItem { DictId = 1, TenantId = 0, RowVersion = 7, ItemName = "示例", Metadata = "{\"external\":9007199254740993}" };
        SaasTestHelper.SetBasicId(dict, 1);
        SaasTestHelper.SetBasicId(item, 2);
        var dictionaries = new Mock<IDictRepository>();
        var items = new Mock<IDictItemRepository>();
        dictionaries.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dict);
        items.Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        items.Setup(x => x.UpdateWithVersionAsync(item, 7, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        var service = new DictDomainService(dictionaries.Object, items.Object, Mock.Of<ICurrentTenant>());
        var command = new DictItemUpdateCommand(2, null, "示例", null, null, null, false, 0, null, new() { Icon = "ep:check" }, "6");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDictItemAsync(command));
        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateDictItemAsync(command with { ExpectedVersion = null }));
        items.Verify(x => x.UpdateWithVersionAsync(It.IsAny<SysDictItem>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        await service.UpdateDictItemAsync(command with { ExpectedVersion = "7" });
        Assert.Contains("9007199254740993", item.Metadata);
        Assert.Equal("ep:check", DictTagStyles.Read(item.Metadata)!.Icon);
        dictionaries.Verify(x => x.TouchItemsVersionAsync(dict, It.IsAny<CancellationToken>()), Times.Once);
    }
}

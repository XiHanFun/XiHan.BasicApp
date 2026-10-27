// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Identity;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 契约 DTO 经源生成上下文往返：缺省值、camelCase、字符串枚举、派生属性不进 JSON。
/// </summary>
public sealed class ContractSerializationTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("""{"page":{"index":2,"size":10}}""")]
    [InlineData("""{"keyword":"x","filters":[],"sorts":[]}""")]
    public void PageRequest_Deserializes_WithNonNullCollections(string json)
    {
        var request = JsonSerializer.Deserialize(json, IdentityJsonContext.Default.PageRequest);
        Assert.NotNull(request);
        Assert.NotNull(request.Page);
        Assert.NotNull(request.Filters);
        Assert.NotNull(request.Sorts);
        Assert.True(request.Page.Index >= 1);
    }

    [Fact]
    public void PageInfo_Skip_IsDerived_AndNotSerialized()
    {
        var json = JsonSerializer.Serialize(new PageInfo { Index = 3, Size = 20 }, IdentityJsonContext.Default.PageInfo);
        Assert.Equal("""{"index":3,"size":20}""", json);
        Assert.Equal(40, new PageInfo { Index = 3, Size = 20 }.Skip);
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Identity;
using XiHan.Framework.Persistence.Abstractions.Queries;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证身份包配置的取值范围。
/// </summary>
/// <remarks>
/// 密码历史按一次查询取出比对，框架一次查询最多取 <see cref="EntityQuery.DefaultMaxTake"/> 条：
/// 深度配到这个上限为止，再大在读配置时就失败，不等到改密码时才抛。
/// </remarks>
public sealed class IdentityOptionsTests
{
    private static IdentityOptions Read(string depth)
        => IdentityOptions.FromConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection([new($"{IdentityOptions.ConfigurationSection}:PasswordHistoryDepth", depth)])
            .Build());

    [Theory]
    [InlineData(0)]
    [InlineData(EntityQuery.DefaultMaxTake)]
    public void PasswordHistoryDepth_UpToOneQueryPage_IsAccepted(int depth)
        => Assert.Equal(depth, Read(depth.ToString(System.Globalization.CultureInfo.InvariantCulture)).PasswordHistoryDepth);

    [Fact]
    public void PasswordHistoryDepth_BeyondOneQueryPage_FailsWhenReadingConfiguration()
    {
        var error = Assert.Throws<ArgumentException>(() => Read((EntityQuery.DefaultMaxTake + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Assert.Contains("PasswordHistoryDepth", error.Message, StringComparison.Ordinal);
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.DomainServices;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 默认密码策略测试：内置/配置种子密码的命中判定（强制改密的前置判定）。
/// </summary>
public sealed class DefaultPasswordPolicyTests
{
    /// <summary>
    /// 空密码不判定为默认密码。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsDefaultPassword_WithBlankPassword_ShouldReturnFalse(string? password)
    {
        Assert.False(DefaultPasswordPolicy.IsDefaultPassword(password, null));
    }

    /// <summary>
    /// 命中内置默认密码时必须判定为默认密码。
    /// </summary>
    [Fact]
    public void IsDefaultPassword_WithBuiltInDefault_ShouldReturnTrue()
    {
        Assert.True(DefaultPasswordPolicy.IsDefaultPassword("SuperAdmin@123", null));
    }

    /// <summary>
    /// 命中配置的种子密码时必须判定为默认密码。
    /// </summary>
    [Fact]
    public void IsDefaultPassword_WithConfiguredSeedPassword_ShouldReturnTrue()
    {
        Assert.True(DefaultPasswordPolicy.IsDefaultPassword("Prod@Seed#2026", "Prod@Seed#2026"));
    }

    /// <summary>
    /// 未配置种子密码时仅内置默认命中。
    /// </summary>
    [Fact]
    public void IsDefaultPassword_WithoutConfiguredSeed_ShouldOnlyMatchBuiltIn()
    {
        Assert.False(DefaultPasswordPolicy.IsDefaultPassword("Prod@Seed#2026", null));
        Assert.False(DefaultPasswordPolicy.IsDefaultPassword("other-password", string.Empty));
    }

    /// <summary>
    /// 普通密码不判定为默认密码。
    /// </summary>
    [Fact]
    public void IsDefaultPassword_WithNormalPassword_ShouldReturnFalse()
    {
        Assert.False(DefaultPasswordPolicy.IsDefaultPassword("MyStrongP@ssw0rd", "Prod@Seed#2026"));
    }

    /// <summary>
    /// 判定大小写敏感：形似默认密码但大小写不同不得误判。
    /// </summary>
    [Fact]
    public void IsDefaultPassword_ShouldBeCaseSensitive()
    {
        Assert.False(DefaultPasswordPolicy.IsDefaultPassword("superadmin@123", null));
        Assert.False(DefaultPasswordPolicy.IsDefaultPassword("SUPERADMIN@123", null));
    }

    /// <summary>
    /// 配置键与内置默认保持一致。
    /// </summary>
    [Fact]
    public void Constants_ShouldMatchSeederConventions()
    {
        Assert.Equal("SuperAdmin@123", DefaultPasswordPolicy.BuiltInDefaultPassword);
        Assert.Equal("Saas:Seed:SuperAdminPassword", DefaultPasswordPolicy.SeedPasswordConfigKey);
    }
}

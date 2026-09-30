// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 字段安全实体目录测试：显示名取表/列说明，只列可配置的业务字段。
/// </summary>
public sealed class FieldSecurityEntityCatalogTests
{
    private readonly FieldSecurityEntityCatalog _catalog = new(Options.Create(new FieldSecurityEntityOptions().Add<SysUser>()));

    /// <summary>
    /// 实体显示名取表说明，字段显示名取列说明，文本与非文本区分开。
    /// </summary>
    [Fact]
    public void Find_ShouldDescribeEntityWithColumnDescriptions()
    {
        var entity = _catalog.Find(nameof(SysUser));

        Assert.NotNull(entity);
        Assert.Equal("系统用户表", entity.DisplayName);
        var userName = entity.FindField(nameof(SysUser.UserName));
        Assert.NotNull(userName);
        Assert.True(userName.IsText);
        Assert.NotEqual(nameof(SysUser.UserName), userName.DisplayName);
        Assert.False(entity.FindField(nameof(SysUser.Gender))!.IsText);
    }

    /// <summary>
    /// 主键、租户、并发版本、软删除等框架列不可配置。
    /// </summary>
    [Theory]
    [InlineData("BasicId")]
    [InlineData("TenantId")]
    [InlineData("RowVersion")]
    [InlineData("IsDeleted")]
    [InlineData("DeletedTime")]
    public void Find_ShouldExcludeFrameworkColumns(string fieldName)
    {
        Assert.Null(_catalog.Find(nameof(SysUser))!.FindField(fieldName));
    }

    /// <summary>
    /// 字段名区分大小写，与打码时按属性名匹配一致。
    /// </summary>
    [Fact]
    public void FindField_ShouldBeCaseSensitive()
    {
        Assert.Null(_catalog.Find(nameof(SysUser))!.FindField("phone"));
    }

    /// <summary>
    /// 未登记实体按名字查不到，按类型取名直接报错。
    /// </summary>
    [Fact]
    public void UnregisteredEntity_ShouldNotBeFoundAndShouldThrowByType()
    {
        Assert.Null(_catalog.Find(nameof(SysRole)));
        _ = Assert.Throws<InvalidOperationException>(() => _catalog.GetEntityName(typeof(SysRole)));
    }

    /// <summary>
    /// 重复登记同一类型没有副作用。
    /// </summary>
    [Fact]
    public void Add_SameTypeTwice_ShouldBeIdempotent()
    {
        var options = new FieldSecurityEntityOptions().Add<SysUser>().Add<SysUser>();

        Assert.Single(options.Entities);
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>实际字段安全过滤器必须同时遮蔽详情、列表、树和选项中的派生样式。</summary>
public sealed class DictTagStyleReadSecurityTests
{
    /// <summary>共享缓存只保存未过滤数据，各请求在输出边界依照 Metadata 权限过滤。</summary>
    [Fact]
    public async Task MetadataReadProtectionCoversEveryStyleProjection()
    {
        var user = new Mock<ICurrentUser>();
        user.SetupGet(x => x.UserId).Returns(42);
        var options = Options.Create(new FieldSecurityEntityOptions().Add<SysDictItem>());
        var rules = new Mock<IFieldLevelSecurityRepository>();
        rules.Setup(x => x.GetEnabledByEntityAsync(nameof(SysDictItem), It.IsAny<CancellationToken>())).ReturnsAsync([
            new SysFieldLevelSecurity { EntityName = nameof(SysDictItem), FieldName = nameof(SysDictItem.Metadata), TargetType = FieldSecurityTargetType.User, TargetId = 42, MaskStrategy = FieldMaskStrategy.Hidden, IsEditable = false, Status = EnableStatus.Enabled }
        ]);
        var roles = new Mock<IUserRoleRepository>();
        roles.Setup(x => x.GetValidByUserIdAsync(42, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var departments = new Mock<IUserDepartmentRepository>();
        departments.Setup(x => x.GetValidByUserIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var service = new FieldSecurityService(new FieldSecurityEntityCatalog(options), new FieldSecurityDtoCatalog(options), rules.Object,
            Mock.Of<IFieldSecurityEntityReader>(), roles.Object, Mock.Of<IRoleRepository>(), departments.Object, Mock.Of<IDepartmentRepository>(), user.Object);
        var entity = new SysDictItem { ItemName = "示例", Metadata = "{\"tagStyle\":{\"icon\":\"ep:check\"}}" };
        var detail = DictApplicationMapper.ToItemDetailDto(entity);
        var list = DictApplicationMapper.ToItemListItemDto(entity);
        var tree = DictApplicationMapper.ToItemTreeNodeDto(entity);
        var option = new DictOptionDto { Label = "示例", TagStyle = DictTagStyles.Read(entity.Metadata) };
        await service.MaskAsync(new object[] { detail, list, tree, option });
        Assert.Null(detail.TagStyle);
        Assert.Null(list.TagStyle);
        Assert.Null(tree.TagStyle);
        Assert.Null(option.TagStyle);
        Assert.Equal("示例", option.Label);
    }
}

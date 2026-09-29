// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限目录分组显示名：只给真正的名字，解析不出时留空交给前端按权限名命名。
/// </summary>
/// <remarks>
/// 回归锚点：聊天、打印当时只有功能权限、不挂资源，分组名曾回填成组码，授权面板的分组标题显示成 chat、print-template。
/// 种子里的权限现已全部挂上资源，但管理端仍可建不挂资源的功能权限，这条规则照样适用。
/// </remarks>
public sealed class PermissionGroupNameMappingTests
{
    /// <summary>
    /// Saas 自己的权限取定义表里的组名。
    /// </summary>
    [Fact]
    public void SaasPermission_ShouldUseDefinedGroupName()
    {
        var dto = PermissionApplicationMapper.ToListItemDto(Permission(SaasPermissionCodes.Tenant.Read), null, null);

        Assert.Equal("租户", dto.GroupName);
    }

    /// <summary>
    /// 其它模块挂了资源的权限取资源中文名。
    /// </summary>
    [Fact]
    public void ModulePermissionWithResource_ShouldUseResourceName()
    {
        var dto = PermissionApplicationMapper.ToListItemDto(Permission("workflow:read"), new SysResource { ResourceCode = "workflow", ResourceName = "工作流" }, null);

        Assert.Equal("workflow", dto.GroupCode);
        Assert.Equal("工作流", dto.GroupName);
    }

    /// <summary>
    /// 不挂资源的模块功能权限不把组码当名字。
    /// </summary>
    [Theory]
    [InlineData("chat:read", "chat")]
    [InlineData("print-template:read", "print-template")]
    public void ModuleFunctionalPermission_ShouldLeaveGroupNameEmpty(string permissionCode, string groupCode)
    {
        var dto = PermissionApplicationMapper.ToListItemDto(Permission(permissionCode), null, null);

        Assert.Equal(groupCode, dto.GroupCode);
        Assert.Equal(string.Empty, dto.GroupName);
    }

    private static SysPermission Permission(string code) =>
        new() { PermissionCode = code, PermissionName = code, ModuleCode = code.Split(':')[0] };
}

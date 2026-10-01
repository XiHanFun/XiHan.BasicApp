// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Pages;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 业务模块登记的权限目录
/// </summary>
/// <remarks>
/// 实现类按约定注册（<c>[ExposeServices(typeof(IPermissionCatalogContribution))]</c> + 生命周期接口），
/// 由 <see cref="ContributedPermissionCatalogSeeder"/> 在权限目录阶段最后统一写入，业务模块不需要自己的种子、也不占种子顺序号。
/// 代码生成「生成到项目」产出的就是这种登记。
/// </remarks>
public interface IPermissionCatalogContribution
{
    /// <summary>
    /// 登记名（日志与出错信息里标明来源）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 模块编码（权限的模块归属，权限页按模块筛选）
    /// </summary>
    string ModuleCode { get; }

    /// <summary>
    /// 资源（只有资源型权限需要）
    /// </summary>
    IReadOnlyList<ResourceSeed> Resources { get; }

    /// <summary>
    /// 权限
    /// </summary>
    IReadOnlyList<PermissionSeed> Permissions { get; }
}

/// <summary>
/// 业务模块登记的菜单页与按钮
/// </summary>
/// <remarks>
/// 实现类按约定注册（<c>[ExposeServices(typeof(IMenuPageContribution))]</c> + 生命周期接口），
/// 由 <see cref="ContributedMenuSeeder"/> 在菜单阶段最后统一写入：此时平台各模块的目录都已就位，页面可以挂到它们下面。
/// 页面绑定的权限须已登记（平台权限或 <see cref="IPermissionCatalogContribution"/>），父菜单须已存在，缺了直接报错。
/// </remarks>
public interface IMenuPageContribution
{
    /// <summary>
    /// 登记名（日志与出错信息里标明来源）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 页面（父目录须排在子项之前）
    /// </summary>
    IReadOnlyList<PageDescriptor> Pages { get; }

    /// <summary>
    /// 页面内按钮
    /// </summary>
    IReadOnlyList<ButtonDescriptor> Buttons { get; }
}

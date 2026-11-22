// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Primitives;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 运行期增减的权限描述
/// </summary>
/// <remarks>
/// 启动期之外贡献权限码的一方（插件模块）实现它，<see cref="RegisteredPermissions"/> 把它与各包启动期登记的片段合在一起
/// 片段变了之后触发 <see cref="GetChangeToken"/> 给出的令牌；片段里的权限码不与已登记的重复（插件宿主加载时已按权限登记表核对）
/// </remarks>
public interface IRuntimePermissionSource
{
    /// <summary>
    /// 当前的全部片段
    /// </summary>
    IReadOnlyList<PermissionCatalogSection> Sections { get; }

    /// <summary>
    /// 片段变动的令牌
    /// </summary>
    /// <returns>下一次变动时触发的令牌</returns>
    IChangeToken GetChangeToken();
}

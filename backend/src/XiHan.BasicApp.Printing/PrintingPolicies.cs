// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing;

/// <summary>
/// 打印模块登记的授权策略名
/// </summary>
public static class PrintingPolicies
{
    /// <summary>
    /// 读数据源目录：持有模板的读取或使用权限其一
    /// </summary>
    /// <remarks>
    /// 目录同时服务模板管理（读取）与业务打印（使用）两条路径（4.x 在方法里判定），由 <c>Infrastructure/PrintDataSourceCatalogPolicy</c> 实现
    /// </remarks>
    public const string DataSourceCatalog = "printing.data-source-catalog";
}

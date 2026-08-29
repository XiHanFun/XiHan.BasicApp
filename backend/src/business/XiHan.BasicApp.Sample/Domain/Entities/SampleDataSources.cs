// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Sample.Domain.Entities;

/// <summary>
/// 示例用到的逻辑数据源名。
/// </summary>
/// <remarks>
/// <para>
/// 这里是<b>逻辑名</b>，不是连接标识。框架按「数据源名 + 当前租户」解析到实际连接：
/// 平台态与字段隔离租户落共享的 <c>Erp</c> 库；库隔离租户落各自的 <c>Erp_Tenant_{租户Id}</c> 库
/// （或由业务实现 <c>ISqlSugarTenantDataSourceProvider</c> 动态给出）。
/// 所以「模块分库」与「租户分库」是两条独立维度，可任意组合。
/// </para>
/// <para>
/// 集中成常量而不是各处写字符串字面量：改名时一处改完，漏改会在编译期暴露。
/// </para>
/// </remarks>
public static class SampleDataSources
{
    /// <summary>
    /// Erp 逻辑数据源
    /// </summary>
    public const string Erp = "Erp";
}

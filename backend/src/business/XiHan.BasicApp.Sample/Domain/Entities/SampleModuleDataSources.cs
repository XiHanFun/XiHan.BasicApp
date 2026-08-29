// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Sample.Domain.Entities;

/// <summary>
/// 示例用到的模块数据源名。
/// </summary>
/// <remarks>
/// <para>
/// 模块数据源是<b>库的分组名</b>，不是模块工程名，也不是连接标识。一个模块工程可以不分库、
/// 也可以把不同实体分到多个模块数据源；反过来多个模块工程共用一个模块数据源也成立。
/// </para>
/// <para>
/// 实际连接由「模块名 + 当前布局」派生：平台态与字段隔离租户落 <c>Default_Erp</c>；
/// 库隔离租户若自带该模块库则落 <c>Tenant_{租户Id}_Erp</c>，否则回落共享的 <c>Default_Erp</c>。
/// 所以「模块分库」与「租户分库」是两条独立维度，可任意组合。
/// </para>
/// <para>
/// 集中成常量而不是各处写字符串字面量：改名时一处改完，漏改会在编译期暴露。
/// </para>
/// </remarks>
public static class SampleModuleDataSources
{
    /// <summary>
    /// Erp 模块数据源
    /// </summary>
    public const string Erp = "Erp";
}

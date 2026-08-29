// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Sample.Domain.Entities;

/// <summary>
/// 示例用到的数据源标识，与 appsettings 的 <c>ConnectionConfigs[].ConfigId</c> 一一对应。
/// </summary>
/// <remarks>
/// 集中成常量而不是各处写字符串字面量：改名时一处改完，漏改会在编译期暴露。
/// </remarks>
public static class SampleDataSources
{
    /// <summary>
    /// Erp 模块库
    /// </summary>
    public const string Erp = "Erp";
}

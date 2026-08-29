// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Sample.Infrastructure.MultiTenancy;

/// <summary>
/// 示例的租户级数据源配置
/// </summary>
/// <remarks>
/// <para>
/// 形状是「逻辑数据源名 → 租户 Id → 连接串」两层字典，对应配置：
/// </para>
/// <code>
/// "Sample": {
///   "TenantDataSources": {
///     "Erp": {
///       "1962xxxxxxxxxxxxx": "Server=...;Database=XiHanBasicAppErp_T1;..."
///     }
///   }
/// }
/// </code>
/// <para>
/// 不配置即为空——所有租户共用同一个 <c>Erp</c> 库，与不启用本特性时行为一致。
/// 只有真的要给某个租户单独开模块库时才往里加一条。
/// </para>
/// <para>
/// 租户 Id 是雪花值、由种子在运行期生成，没有编译期常量可用，
/// 所以这里用配置而不是代码硬编码——这也是 BasicApp 里演示二维路由的实际做法。
/// </para>
/// </remarks>
public class SampleTenantDataSourceOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Sample";

    /// <summary>
    /// 逻辑数据源名 → （租户 Id → 连接串）
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> TenantDataSources { get; set; } = [];
}

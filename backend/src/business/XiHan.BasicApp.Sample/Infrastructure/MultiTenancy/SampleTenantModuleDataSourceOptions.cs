// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Sample.Infrastructure.MultiTenancy;

/// <summary>
/// 示例的租户级模块库配置
/// </summary>
/// <remarks>
/// <para>
/// 形状是「租户 Id → 模块名 → 连接串」两层字典，对应配置：
/// </para>
/// <code>
/// "Sample": {
///   "TenantModuleDataSources": {
///     "1962xxxxxxxxxxxxx": {
///       "Erp": "Server=...;Database=XiHanBasicAppErp_T1;..."
///     }
///   }
/// }
/// </code>
/// <para>
/// 不配置即为空——库隔离租户的模块表落回共享模块库（<c>Default_Erp</c>），与不启用本特性时行为一致。
/// 只有真的要给某个租户单独开模块库时才往里加一条。
/// </para>
/// <para>
/// 租户 Id 是雪花值、由种子在运行期生成，没有编译期常量可用，所以这里用配置而不是代码硬编码。
/// </para>
/// </remarks>
public class SampleTenantModuleDataSourceOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Sample";

    /// <summary>
    /// 租户 Id → （模块数据源名 → 连接串）
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> TenantModuleDataSources { get; set; } = [];
}

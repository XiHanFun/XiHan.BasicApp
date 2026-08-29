// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.Framework.Data.SqlSugar.Entities;
using XiHan.Framework.Data.SqlSugar.Routing;

namespace XiHan.BasicApp.DataSourceSample;

/// <summary>
/// 主应用实体：未声明数据源，跟随当前租户上下文解析连接（落默认库）
/// </summary>
[SugarTable("sample_user")]
public class SampleUser : SugarEntity<long>
{
    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnName = "user_name", Length = 64)]
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// Erp 模块实体：经框架特性声明落 ConfigId 为 Erp 的库
/// </summary>
[SugarTable("erp_order")]
[DataSource("Erp")]
public class ErpOrder : SugarEntity<long>
{
    /// <summary>
    /// 订单号
    /// </summary>
    [SugarColumn(ColumnName = "order_no", Length = 64)]
    public string OrderNo { get; set; } = string.Empty;
}

/// <summary>
/// Mes 模块实体：用 SqlSugar 原生租户特性声明数据源，验证兼容识别
/// </summary>
[SugarTable("mes_task")]
[Tenant("Mes")]
public class MesTask : SugarEntity<long>
{
    /// <summary>
    /// 工单号
    /// </summary>
    [SugarColumn(ColumnName = "task_no", Length = 64)]
    public string TaskNo { get; set; } = string.Empty;
}

/// <summary>
/// 声明了未配置连接的数据源，用于验证 fail-closed：解析时应抛异常而非回落默认库
/// </summary>
/// <remarks>
/// 刻意不标 <see cref="SugarTable"/>，避免被建表扫描收录。
/// </remarks>
[DataSource("NotConfigured")]
public class OrphanEntity : SugarEntity<long>
{
}

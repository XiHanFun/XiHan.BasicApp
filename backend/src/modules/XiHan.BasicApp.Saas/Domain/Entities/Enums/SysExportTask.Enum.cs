// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 导出任务状态
/// </summary>
public enum ExportTaskStatus
{
    /// <summary>
    /// 待执行（已提交，等待后台 worker 领取）
    /// </summary>
    [Description("待执行")]
    Pending = 0,

    /// <summary>
    /// 执行中（已被 worker 领取，正在拉取数据 / 写出 / 上传）
    /// </summary>
    [Description("执行中")]
    Processing = 1,

    /// <summary>
    /// 成功（产物已生成并落 SysFile，可下载）
    /// </summary>
    [Description("成功")]
    Success = 2,

    /// <summary>
    /// 失败（错误见 ErrorMessage）
    /// </summary>
    [Description("失败")]
    Failed = 3
}

/// <summary>
/// 导出范围
/// </summary>
/// <remarks>
/// 仅控制「单页 / 全部匹配」的分页行为；具体过滤条件由前端写入 QuerySnapshot：
/// - CurrentPage：仅导出快照中指定的那一页；
/// - SearchResult：导出当前查询条件下的全部匹配行（按快照过滤翻页）；
/// - All：导出全部行（前端提交清空过滤后的快照）。
/// SearchResult 与 All 的执行路径一致，差异仅在快照里携带的过滤条件。
/// </remarks>
public enum ExportScope
{
    /// <summary>
    /// 当前页
    /// </summary>
    [Description("当前页")]
    CurrentPage = 0,

    /// <summary>
    /// 查询结果（全部匹配）
    /// </summary>
    [Description("查询结果")]
    SearchResult = 1,

    /// <summary>
    /// 全部
    /// </summary>
    [Description("全部")]
    All = 2
}

/// <summary>
/// 导出文件格式
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// CSV（UTF-8 BOM，零依赖流式写出，支撑百万级行）
    /// </summary>
    [Description("CSV")]
    Csv = 0,

    /// <summary>
    /// Excel（.xlsx，P2 增强）
    /// </summary>
    [Description("Excel")]
    Xlsx = 1
}

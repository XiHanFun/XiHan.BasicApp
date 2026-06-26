#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExportTaskDtos
// Guid:b1a8c6e4-9f2d-4e7a-83b5-5d4e6f708192
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 导出列定义（前端按当前列设置派生后随提交快照上送）
/// </summary>
public sealed class ExportColumnDto
{
    /// <summary>
    /// 字段键（对应资源 DTO 的属性名，大小写不敏感匹配）
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 列标题（写入表头）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 取值映射（枚举/字典列：原始值 → 展示 label；为空则原样输出）。
    /// 由前端已加载的字典/枚举选项构建，规避「字段语义在前端」导致服务端只能导出原始码值。
    /// </summary>
    public Dictionary<string, string>? ValueMap { get; set; }
}

/// <summary>
/// 提交导出任务入参
/// </summary>
public sealed class ExportTaskSubmitDto
{
    /// <summary>
    /// 业务类型（= 前端 pageCode，匹配后端 IExportProvider.BusinessType）
    /// </summary>
    public string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称（可空，缺省由后端按「业务类型_时间戳」生成）
    /// </summary>
    public string? TaskName { get; set; }

    /// <summary>
    /// 导出范围
    /// </summary>
    public ExportScope Scope { get; set; } = ExportScope.SearchResult;

    /// <summary>
    /// 导出格式
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.Csv;

    /// <summary>
    /// 查询条件快照（资源自身分页查询 DTO 的 JSON；由对应 Provider 反序列化为具体类型）
    /// </summary>
    public string? QuerySnapshot { get; set; }

    /// <summary>
    /// 导出列（按顺序写出；至少一列）
    /// </summary>
    public List<ExportColumnDto> Columns { get; set; } = [];
}

/// <summary>
/// 导出任务 DTO（列表 / 详情）
/// </summary>
public sealed class ExportTaskDto
{
    /// <summary>
    /// 主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 导出范围
    /// </summary>
    public ExportScope Scope { get; set; }

    /// <summary>
    /// 导出格式
    /// </summary>
    public ExportFormat Format { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public ExportTaskStatus Status { get; set; }

    /// <summary>
    /// 进度（0-100）
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 数据总行数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 已处理行数
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// 产物文件ID（成功后可用于下载）
    /// </summary>
    public long? FileId { get; set; }

    /// <summary>
    /// 产物文件名
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 产物文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 错误信息（失败时）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 开始执行时间
    /// </summary>
    public DateTimeOffset? StartedTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTimeOffset? FinishedTime { get; set; }
}

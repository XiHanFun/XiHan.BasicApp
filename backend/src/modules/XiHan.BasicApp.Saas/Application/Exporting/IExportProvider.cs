// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// 导出数据提供者（按业务类型注册，逐资源登记）
/// </summary>
/// <remarks>
/// 每个 Provider 负责一种业务类型：内部调既有 QueryService 流式分页拉取，
/// 并按上下文列定义投影为字符串行（已应用 ValueMap）。执行器据 BusinessType 分发。
/// </remarks>
public interface IExportProvider
{
    /// <summary>
    /// 业务类型（= 导出按钮所属页面码，见 PageRegistry）
    /// </summary>
    /// <remarks>
    /// 前端页面 schema 的 pageCode 与 <c>resource.export.businessType</c> 都取同一个值。
    /// </remarks>
    string BusinessType { get; }

    /// <summary>
    /// 导出所需权限码（与页面导出按钮绑定的权限一致，不是资源的读权限）
    /// </summary>
    /// <remarks>
    /// 提交导出任务时按它拦截；后台执行时执行器按发起人再校验一次，覆盖提交后被收回权限的情况。
    /// </remarks>
    string RequiredPermission { get; }

    /// <summary>
    /// 流式读取导出行（按 context.Columns 顺序投影为字符串；遵循 context.Scope 单页/全量）
    /// </summary>
    IAsyncEnumerable<IReadOnlyList<string>> ReadRowsAsync(ExportContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// 导出默认参数
/// </summary>
public static class ExportDefaults
{
    /// <summary>
    /// 全量导出的分页批大小
    /// </summary>
    public const int BatchSize = 1000;

    /// <summary>
    /// 单任务最大导出行数（安全上限，防止失控）
    /// </summary>
    public const int MaxRows = 1_000_000;

    /// <summary>
    /// 当前页范围的单页最大条数
    /// </summary>
    public const int CurrentPageMaxSize = 1000;
}

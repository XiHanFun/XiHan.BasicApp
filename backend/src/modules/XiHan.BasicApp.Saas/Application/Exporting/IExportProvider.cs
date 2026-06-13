#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IExportProvider
// Guid:b7a4d2f0-5c8e-4f3b-82d6-2e3f40516273
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    /// 业务类型（= 前端 pageCode）
    /// </summary>
    string BusinessType { get; }

    /// <summary>
    /// 导出所需权限码（执行器进程内显式校验，补 [PermissionAuthorize] 不触发的缺口）
    /// </summary>
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

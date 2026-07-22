// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 导入历史应用服务接口（写侧：导入执行完毕后由前端上报留痕）
/// </summary>
public interface IImportHistoryAppService : IApplicationService
{
    /// <summary>
    /// 新增导入历史（追加留痕，日志型只写不改）
    /// </summary>
    Task<ImportHistoryDto> CreateAsync(ImportHistoryCreateDto input, CancellationToken cancellationToken = default);
}

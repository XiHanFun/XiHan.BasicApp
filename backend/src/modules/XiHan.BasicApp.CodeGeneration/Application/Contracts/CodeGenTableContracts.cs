#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableContracts
// Guid:c0de9e00-0602-4a00-9000-000000000602
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.CodeGeneration.Application.Contracts;

/// <summary>
/// 代码生成表配置命令应用服务接口
/// </summary>
/// <remarks>表配置的创建由"导入数据库表"流程完成（见 <see cref="ICodeGenerationAppService"/>），此处仅维护与删除。</remarks>
public interface ICodeGenTableAppService : IApplicationService
{
    /// <summary>
    /// 更新表配置
    /// </summary>
    Task<CodeGenTableDetailDto> UpdateAsync(CodeGenTableUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新表配置状态
    /// </summary>
    Task<CodeGenTableDetailDto> UpdateStatusAsync(CodeGenTableStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除表配置
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 代码生成表配置查询应用服务接口
/// </summary>
public interface ICodeGenTableQueryService : IApplicationService
{
    /// <summary>
    /// 获取表配置分页列表
    /// </summary>
    Task<PageResultDtoBase<CodeGenTableListItemDto>> GetPageAsync(CodeGenTablePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取表配置详情
    /// </summary>
    Task<CodeGenTableDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}

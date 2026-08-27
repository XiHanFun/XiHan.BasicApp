// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Printing.Application.Dtos;
using XiHan.BasicApp.Printing.Domain.Enums;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Printing.Application.Contracts;

/// <summary>
/// 打印模板命令 Dynamic API 契约。
/// </summary>
public interface IPrintTemplateAppService : IApplicationService
{
    /// <summary>
    /// 在当前可写作用域创建打印模板。
    /// </summary>
    /// <param name="input">创建参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的模板详情。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">作用域、权限、JSON 或唯一性校验失败。</exception>
    Task<PrintTemplateDetailDto> CreatePrintTemplateAsync(PrintTemplateCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用客户端行版本更新打印模板。
    /// </summary>
    /// <param name="input">更新参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新后的模板详情。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">作用域、权限、JSON 或并发校验失败。</exception>
    Task<PrintTemplateDetailDto> UpdatePrintTemplateAsync(PrintTemplateUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用客户端行版本启用或停用打印模板。
    /// </summary>
    /// <param name="input">状态变更参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>变更后的模板详情。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">作用域、权限、状态或并发校验失败。</exception>
    Task<PrintTemplateDetailDto> UpdatePrintTemplateStatusAsync(PrintTemplateStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用客户端行版本软删除已经停用的打印模板。
    /// </summary>
    /// <param name="input">删除参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">模板仍启用、无权操作或并发冲突。</exception>
    Task DeletePrintTemplateAsync(PrintTemplateDeleteDto input, CancellationToken cancellationToken = default);
}

/// <summary>
/// 打印模板查询与按编码解析 Dynamic API 契约。
/// </summary>
public interface IPrintTemplateQueryService : IApplicationService
{
    /// <summary>
    /// 分页查询指定作用域的打印模板。
    /// </summary>
    /// <param name="input">分页和过滤参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>模板分页。</returns>
    Task<PageResultDtoBase<PrintTemplateListItemDto>> GetPrintTemplatePageAsync(
        PrintTemplatePageQueryDto input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询当前租户可使用的全局打印模板。
    /// </summary>
    /// <param name="input">分页和过滤参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已启用且开放的全局模板分页。</returns>
    Task<PageResultDtoBase<PrintTemplateListItemDto>> GetAvailableGlobalPrintTemplatePageAsync(
        PrintTemplatePageQueryDto input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在指定作用域查询打印模板详情。
    /// </summary>
    /// <param name="id">模板主键。</param>
    /// <param name="scope">查询作用域。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>模板详情；不存在时返回 <see langword="null"/>。</returns>
    Task<PrintTemplateDetailDto?> GetPrintTemplateDetailAsync(
        long id,
        PrintTemplateScope scope = PrintTemplateScope.Auto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按模板编码和作用域解析一个可用打印模板。
    /// </summary>
    /// <param name="templateCode">模板编码。</param>
    /// <param name="scope">解析作用域。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>命中的模板及实际作用域。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">模板不存在、未启用或未向当前租户开放。</exception>
    Task<ResolvedPrintTemplateDto> GetResolvedPrintTemplateByCodeAsync(
        string templateCode,
        PrintTemplateScope scope = PrintTemplateScope.Auto,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 应用内部可复用的打印模板解析器契约。
/// </summary>
public interface IPrintTemplateResolver
{
    /// <summary>
    /// 按当前租户、请求作用域和模板编码解析启用模板。
    /// </summary>
    /// <param name="templateCode">模板编码。</param>
    /// <param name="scope">解析作用域。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>命中的模板；不存在时返回 <see langword="null"/>。</returns>
    Task<ResolvedPrintTemplateDto?> ResolveAsync(
        string templateCode,
        PrintTemplateScope scope = PrintTemplateScope.Auto,
        CancellationToken cancellationToken = default);
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileQueryService
// Guid:e8f9a0b1-c2d3-4e5f-4a5b-7c8d9e0f1a2b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 文件查询服务（处理文件的读操作 - CQRS）
/// </summary>
public class FileQueryService : ApplicationServiceBase
{
    private readonly IFileRepository _fileRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileQueryService(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    /// <summary>
    /// 根据ID获取文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件DTO</returns>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        return file?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据文件哈希查询文件（用于文件去重）
    /// </summary>
    /// <param name="fileHash">文件哈希</param>
    /// <returns>文件DTO</returns>
    public async Task<RbacDtoBase?> GetByFileHashAsync(string fileHash)
    {
        var file = await _fileRepository.GetByFileHashAsync(fileHash);
        return file?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据存储路径查询文件
    /// </summary>
    /// <param name="storagePath">存储路径</param>
    /// <returns>文件DTO</returns>
    public async Task<RbacDtoBase?> GetByStoragePathAsync(string storagePath)
    {
        var file = await _fileRepository.GetByStoragePathAsync(storagePath);
        return file?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取用户上传的文件列表
    /// </summary>
    /// <param name="uploadUserId">上传用户ID</param>
    /// <returns>文件DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByUploadUserIdAsync(long uploadUserId)
    {
        var files = await _fileRepository.GetByUploadUserIdAsync(uploadUserId);
        return files.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 根据文件类型获取文件列表
    /// </summary>
    /// <param name="fileType">文件类型</param>
    /// <returns>文件DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByFileTypeAsync(string fileType)
    {
        var files = await _fileRepository.GetByFileTypeAsync(fileType);
        return files.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取指定时间段内的文件列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>文件DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var files = await _fileRepository.GetByTimeRangeAsync(startTime, endTime);
        return files.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取文件总大小（字节）
    /// </summary>
    /// <returns>文件总大小</returns>
    public async Task<long> GetTotalFileSizeAsync()
    {
        return await _fileRepository.GetTotalFileSizeAsync();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    /// <param name="input">分页查询参数</param>
    /// <returns>分页响应</returns>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _fileRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}

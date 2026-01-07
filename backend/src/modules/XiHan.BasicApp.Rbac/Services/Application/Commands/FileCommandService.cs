#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileCommandService
// Guid:d7e8f9a0-b1c2-4d5e-3f4a-6b7c8d9e0f1a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// 文件命令服务（处理文件的写操作）
/// </summary>
public class FileCommandService : CrudApplicationServiceBase<SysFile, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IFileRepository _fileRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileCommandService(IFileRepository fileRepository)
        : base(fileRepository)
    {
        _fileRepository = fileRepository;
    }

    /// <summary>
    /// 创建文件记录（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        // 1. 检查文件哈希是否已存在（用于文件去重）
        if (!string.IsNullOrEmpty(input.FileHash))
        {
            var exists = await _fileRepository.ExistsByFileHashAsync(input.FileHash);
            if (exists)
            {
                // 文件已存在，可以考虑直接返回已有文件记录
                // 或者根据业务需求决定是否允许重复上传
                throw new InvalidOperationException($"文件哈希 {input.FileHash} 已存在，文件可能已上传");
            }
        }

        // 2. 映射并创建
        var file = input.Adapt<SysFile>();

        // 3. 保存
        file = await _fileRepository.AddAsync(file);

        return await MapToEntityDtoAsync(file);
    }

    /// <summary>
    /// 删除文件记录（重写以添加物理文件删除）
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        // 1. 获取文件记录
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null)
        {
            return false;
        }

        // 2. 删除数据库记录
        var result = await _fileRepository.DeleteByIdAsync(id);

        // 3. TODO: 删除物理文件（根据存储策略）
        // await _storageService.DeleteFileAsync(file.StoragePath);

        return result;
    }

    /// <summary>
    /// 批量删除文件
    /// </summary>
    /// <param name="fileIds">文件ID列表</param>
    /// <returns>是否成功</returns>
    public async Task<bool> BatchDeleteAsync(List<long> fileIds)
    {
        // 批量删除文件记录
        return await _fileRepository.DeleteRangeAsync(fileIds);

        // TODO: 批量删除物理文件
    }
}

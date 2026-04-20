#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileDomainService
// Guid:6e7f8091-0213-2345-ef01-23456789ab04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 文件领域服务
/// </summary>
public class FileDomainService : IFileDomainService, IScopedDependency
{
    private readonly IFileRepository _fileRepository;
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileDomainService(IFileRepository fileRepository, ILocalEventBus localEventBus)
    {
        _fileRepository = fileRepository;
        _localEventBus = localEventBus;
    }

    /// <inheritdoc />
    public async Task<SysFile> CreateAsync(SysFile file)
    {
        var created = await _fileRepository.AddAsync(file);
        await _localEventBus.PublishAsync(new FileChangedDomainEvent(created.BasicId));
        return created;
    }

    /// <inheritdoc />
    public async Task<SysFile> UpdateAsync(SysFile file)
    {
        var updated = await _fileRepository.UpdateAsync(file);
        await _localEventBus.PublishAsync(new FileChangedDomainEvent(updated.BasicId));
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null)
        {
            return false;
        }

        var result = await _fileRepository.DeleteAsync(file);
        if (result)
        {
            await _localEventBus.PublishAsync(new FileChangedDomainEvent(id));
        }
        return result;
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 字典项仓储接口
/// </summary>
public interface IDictItemRepository : ISaasRepository<SysDictItem>
{
    /// <summary>
    /// 根据字典ID获取字典项列表
    /// </summary>
    Task<IReadOnlyList<SysDictItem>> GetByDictIdAsync(long dictId, CancellationToken cancellationToken = default);

    /// <summary>按编辑时版本更新整份显示配置，防止多个编辑窗口互相覆盖元数据。</summary>
    /// <param name="item">已校验且属于当前租户的字典项。</param><param name="expectedVersion">编辑快照版本。</param><param name="cancellationToken">取消。</param>
    /// <returns>更新后的最新实体。</returns><exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">数据库版本冲突。</exception>
    /// <exception cref="InvalidOperationException">更新后实体不可读取。</exception>
    Task<SysDictItem> UpdateWithVersionAsync(SysDictItem item, long expectedVersion, CancellationToken cancellationToken = default);
}

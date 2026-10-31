// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 字典项仓储实现
/// </summary>
public sealed class DictItemRepository : SaasRepository<SysDictItem>, IDictItemRepository
{
    private readonly ISqlSugarClientResolver _resolver;
    /// <summary>复用所属平台数据源。</summary><param name="resolver">数据源解析器。</param>
    public DictItemRepository(ISqlSugarClientResolver resolver) : base(resolver) => _resolver = resolver;
    /// <summary>
    /// 根据字典ID获取字典项列表
    /// </summary>
    public async Task<IReadOnlyList<SysDictItem>> GetByDictIdAsync(long dictId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(item => item.DictId == dictId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysDictItem> UpdateWithVersionAsync(SysDictItem item, long expectedVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = _resolver.GetClientForEntity<SysDictItem>();
        // 单行数据库CAS才是跨进程边界；页面loading仅用于防连点，不能防止另一管理员覆盖。
        item.RowVersion = checked(expectedVersion + 1);
        var affected = await db.Updateable(item).Where(row => row.BasicId == item.BasicId && row.TenantId == item.TenantId
            && !row.IsDeleted && row.RowVersion == expectedVersion).ExecuteCommandAsync(cancellationToken);
        if (affected != 1) throw new UserFriendlyException("字典项已被其他操作修改，请重新打开后再保存。");
        return await GetByIdAsync(item.BasicId, cancellationToken) ?? throw new InvalidOperationException("字典项更新后不可读取。");
    }
}

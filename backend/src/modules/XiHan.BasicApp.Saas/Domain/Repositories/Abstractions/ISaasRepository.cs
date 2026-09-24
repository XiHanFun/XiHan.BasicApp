// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// SaaS 实体仓储接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface ISaasRepository<TEntity> : IRepositoryBase<TEntity, long>
    where TEntity : class, IEntityBase<long>
{
    /// <summary>
    /// 跨租户判断是否存在满足条件的行
    /// </summary>
    /// <remarks>
    /// 平台维护全局模板时用：平台只看得见 0 号行，而租户的行可能引用着全局行（授权、字典项、文件等），
    /// 删除或占用编码前要看所有租户，不能只看自己这一侧。
    /// </remarks>
    /// <param name="predicate">条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> AnyIgnoreTenantAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
}

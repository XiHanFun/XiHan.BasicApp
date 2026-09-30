// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 按实体类型读取当前行：字段安全写校验要拿修改前的值比对只读字段，实体来自各个模块，不能逐个注入仓储
/// </summary>
public interface IFieldSecurityEntityReader
{
    /// <summary>
    /// 按主键读取当前上下文可见的实体（带租户与软删过滤）；不存在返回空
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="id">主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<object?> FindAsync(Type entityType, long id, CancellationToken cancellationToken = default);
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysDictRepository
// Guid:a8b2c3d4-e5f6-7890-abcd-ef1234567897
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstractions;

/// <summary>
/// 系统字典仓储接口
/// </summary>
public interface ISysDictRepository : IRepositoryBase<SysDict, XiHanBasicAppIdType>
{
    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <returns></returns>
    Task<SysDict?> GetByCodeAsync(string dictCode);

    /// <summary>
    /// 根据字典类型获取字典列表
    /// </summary>
    /// <param name="dictType">字典类型</param>
    /// <returns></returns>
    Task<List<SysDict>> GetByTypeAsync(string dictType);

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <param name="excludeId">排除的字典ID</param>
    /// <returns></returns>
    Task<bool> ExistsByCodeAsync(string dictCode, XiHanBasicAppIdType? excludeId = null);
}

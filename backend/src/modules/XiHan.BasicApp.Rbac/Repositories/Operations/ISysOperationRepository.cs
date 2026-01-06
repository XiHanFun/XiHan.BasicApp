#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOperationRepository
// Guid:3a2b3c4d-5e6f-7890-abcd-ef1234567802
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 15:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Operations;

/// <summary>
/// 系统操作仓储接口
/// </summary>
public interface ISysOperationRepository : IRepositoryBase<SysOperation, long>
{
    /// <summary>
    /// 根据操作编码获取操作
    /// </summary>
    /// <param name="operationCode">操作编码</param>
    /// <returns></returns>
    Task<SysOperation?> GetByOperationCodeAsync(string operationCode);

    /// <summary>
    /// 检查操作编码是否存在
    /// </summary>
    /// <param name="operationCode">操作编码</param>
    /// <param name="excludeId">排除的操作ID</param>
    /// <returns></returns>
    Task<bool> ExistsByOperationCodeAsync(string operationCode, long? excludeId = null);

    /// <summary>
    /// 根据操作类型获取操作列表
    /// </summary>
    /// <param name="operationType">操作类型</param>
    /// <returns></returns>
    Task<List<SysOperation>> GetByTypeAsync(OperationTypeCode operationType);

    /// <summary>
    /// 根据操作分类获取操作列表
    /// </summary>
    /// <param name="category">操作分类</param>
    /// <returns></returns>
    Task<List<SysOperation>> GetByCategoryAsync(OperationCategory category);

    /// <summary>
    /// 获取危险操作列表
    /// </summary>
    /// <returns></returns>
    Task<List<SysOperation>> GetDangerousOperationsAsync();

    /// <summary>
    /// 获取需要审计的操作列表
    /// </summary>
    /// <returns></returns>
    Task<List<SysOperation>> GetAuditRequiredOperationsAsync();

    /// <summary>
    /// 根据HTTP方法获取操作列表
    /// </summary>
    /// <param name="httpMethod">HTTP方法</param>
    /// <returns></returns>
    Task<List<SysOperation>> GetByHttpMethodAsync(HttpMethodType httpMethod);
}

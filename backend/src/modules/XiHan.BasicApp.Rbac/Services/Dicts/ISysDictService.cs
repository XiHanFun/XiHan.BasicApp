#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysDictService
// Guid:i1j2k3l4-m5n6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 16:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Services.Dicts.Dtos;
using XiHan.Framework.Application.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Services.Dicts;

/// <summary>
/// 系统字典服务接口
/// </summary>
public interface ISysDictService : ICrudApplicationService<DictDto, XiHanBasicAppIdType, CreateDictDto, UpdateDictDto>
{
    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <returns></returns>
    Task<DictDto?> GetByCodeAsync(string dictCode);

    /// <summary>
    /// 根据字典类型获取字典列表
    /// </summary>
    /// <param name="dictType">字典类型</param>
    /// <returns></returns>
    Task<List<DictDto>> GetByTypeAsync(string dictType);

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <param name="excludeId">排除的字典ID</param>
    /// <returns></returns>
    Task<bool> ExistsByCodeAsync(string dictCode, XiHanBasicAppIdType? excludeId = null);
}

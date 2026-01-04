#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthCodeService
// Guid:h1i2j3k4-l5m6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 18:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.OAuthCodes;
using XiHan.BasicApp.Rbac.Services.OAuthCodes.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.OAuthCodes;

/// <summary>
/// 系统OAuth授权码服务实现
/// </summary>
public class SysOAuthCodeService : CrudApplicationServiceBase<SysOAuthCode, OAuthCodeDto, long, CreateOAuthCodeDto, UpdateOAuthCodeDto>, ISysOAuthCodeService
{
    private readonly ISysOAuthCodeRepository _oauthCodeRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysOAuthCodeService(ISysOAuthCodeRepository oauthCodeRepository) : base(oauthCodeRepository)
    {
        _oauthCodeRepository = oauthCodeRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据授权码获取
    /// </summary>
    public async Task<OAuthCodeDto?> GetByCodeAsync(string code)
    {
        var oauthCode = await _oauthCodeRepository.GetByCodeAsync(code);
        return oauthCode?.Adapt<OAuthCodeDto>();
    }

    /// <summary>
    /// 根据客户端ID和用户ID获取授权码列表
    /// </summary>
    public async Task<List<OAuthCodeDto>> GetByClientAndUserAsync(string clientId, long userId)
    {
        var codes = await _oauthCodeRepository.GetByClientAndUserAsync(clientId, userId);
        return codes.Adapt<List<OAuthCodeDto>>();
    }

    /// <summary>
    /// 删除过期的授权码
    /// </summary>
    public async Task<int> DeleteExpiredCodesAsync()
    {
        return await _oauthCodeRepository.DeleteExpiredCodesAsync();
    }

    #endregion 业务特定方法
}

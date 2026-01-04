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
public class SysOAuthCodeService : CrudApplicationServiceBase<SysOAuthCode, OAuthCodeDto, XiHanBasicAppIdType, CreateOAuthCodeDto, UpdateOAuthCodeDto>, ISysOAuthCodeService
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
        return oauthCode?.ToDto();
    }

    /// <summary>
    /// 根据客户端ID和用户ID获取授权码列表
    /// </summary>
    public async Task<List<OAuthCodeDto>> GetByClientAndUserAsync(string clientId, XiHanBasicAppIdType userId)
    {
        var codes = await _oauthCodeRepository.GetByClientAndUserAsync(clientId, userId);
        return codes.ToDto();
    }

    /// <summary>
    /// 删除过期的授权码
    /// </summary>
    public async Task<int> DeleteExpiredCodesAsync()
    {
        return await _oauthCodeRepository.DeleteExpiredCodesAsync();
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<OAuthCodeDto> MapToEntityDtoAsync(SysOAuthCode entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 OAuthCodeDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysOAuthCode> MapToEntityAsync(OAuthCodeDto dto)
    {
        var entity = new SysOAuthCode
        {
            Code = dto.Code,
            ClientId = dto.ClientId,
            UserId = dto.UserId,
            RedirectUri = dto.RedirectUri,
            Scopes = dto.Scopes,
            State = dto.State,
            CodeChallenge = dto.CodeChallenge,
            CodeChallengeMethod = dto.CodeChallengeMethod,
            ExpiresAt = dto.ExpiresAt,
            IsUsed = dto.IsUsed,
            UsedAt = dto.UsedAt
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 OAuthCodeDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(OAuthCodeDto dto, SysOAuthCode entity)
    {
        entity.Code = dto.Code;
        entity.ClientId = dto.ClientId;
        entity.UserId = dto.UserId;
        entity.RedirectUri = dto.RedirectUri;
        entity.Scopes = dto.Scopes;
        entity.State = dto.State;
        entity.CodeChallenge = dto.CodeChallenge;
        entity.CodeChallengeMethod = dto.CodeChallengeMethod;
        entity.ExpiresAt = dto.ExpiresAt;
        entity.IsUsed = dto.IsUsed;
        entity.UsedAt = dto.UsedAt;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysOAuthCode> MapToEntityAsync(CreateOAuthCodeDto createDto)
    {
        var entity = new SysOAuthCode
        {
            Code = createDto.Code,
            ClientId = createDto.ClientId,
            UserId = createDto.UserId,
            RedirectUri = createDto.RedirectUri,
            Scopes = createDto.Scopes,
            State = createDto.State,
            CodeChallenge = createDto.CodeChallenge,
            CodeChallengeMethod = createDto.CodeChallengeMethod,
            ExpiresAt = createDto.ExpiresAt
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateOAuthCodeDto updateDto, SysOAuthCode entity)
    {
        if (updateDto.IsUsed.HasValue) entity.IsUsed = updateDto.IsUsed.Value;
        if (updateDto.UsedAt.HasValue) entity.UsedAt = updateDto.UsedAt;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using XiHan.BasicApp.Saas.Application.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统字典命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统字典")]
public sealed class DictAppService
    : SaasApplicationService, IDictAppService
{
    private readonly IDictDomainService _dictDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    private readonly IFieldSecurityService _fieldSecurity;
    private readonly IDictItemRepository _dictItems;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DictAppService(IDictDomainService dictDomainService, ISaasCacheInvalidator cacheInvalidator, IFieldSecurityService fieldSecurity, IDictItemRepository dictItems)
    {
        _dictDomainService = dictDomainService;
        _cacheInvalidator = cacheInvalidator;
        _fieldSecurity = fieldSecurity;
        _dictItems = dictItems;
    }

    /// <summary>
    /// 创建系统字典
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Create)]
    public async Task<DictDetailDto> CreateDictAsync(DictCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能填写
        await _fieldSecurity.EnsureCreatableAsync(typeof(SysDict), input, cancellationToken);

        var result = await _dictDomainService.CreateDictAsync(DictApplicationMapper.ToCreateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateDictionaryAsync(cancellationToken);
        return DictApplicationMapper.ToDetailDto(result.Dict);
    }

    /// <summary>
    /// 创建系统字典项
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Create)]
    public async Task<DictItemDetailDto> CreateDictItemAsync(DictItemCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 样式最终存储于Metadata，先投影到真实字段，再执行原字段安全，避免派生属性绕过只读规则。
        input.Metadata = DictTagStyles.Merge(input.Metadata, input.TagStyle);
        // 字段安全：只读字段不能填写
        await _fieldSecurity.EnsureCreatableAsync(typeof(SysDictItem), input, cancellationToken);

        var result = await _dictDomainService.CreateDictItemAsync(DictApplicationMapper.ToItemCreateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateDictionaryAsync(cancellationToken);
        return DictApplicationMapper.ToItemDetailDto(result.DictItem);
    }

    /// <summary>
    /// 删除系统字典
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Delete)]
    public async Task DeleteDictAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _dictDomainService.DeleteDictAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateDictionaryAsync(cancellationToken);
    }

    /// <summary>
    /// 删除系统字典项
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Delete)]
    public async Task DeleteDictItemAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _dictDomainService.DeleteDictItemAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateDictionaryAsync(cancellationToken);
    }

    /// <summary>
    /// 更新系统字典
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Update)]
    public async Task<DictDetailDto> UpdateDictAsync(DictUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysDict), input.BasicId, input, cancellationToken);

        var result = await _dictDomainService.UpdateDictAsync(DictApplicationMapper.ToUpdateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateDictionaryAsync(cancellationToken);
        return DictApplicationMapper.ToDetailDto(result.Dict);
    }

    /// <summary>
    /// 更新系统字典项
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Update)]
    public async Task<DictItemDetailDto> UpdateDictItemAsync(DictItemUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureTagStyleWritableAsync(input, cancellationToken);
        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysDictItem), input.BasicId, input, cancellationToken);

        var result = await _dictDomainService.UpdateDictItemAsync(DictApplicationMapper.ToItemUpdateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateDictionaryAsync(cancellationToken);
        return DictApplicationMapper.ToItemDetailDto(result.DictItem);
    }

    // TagStyle是Metadata的派生写入。只读元数据可原样回交，但不能通过新属性改变颜色。
    private async Task EnsureTagStyleWritableAsync(DictItemUpdateDto input, CancellationToken token)
    {
        if (input.TagStyle is null) return;
        var rules = await _fieldSecurity.ResolveAsync(typeof(SysDictItem), token);
        if (!rules.TryGetValue(nameof(SysDictItem.Metadata), out var rule) || rule.IsEditable) return;
        var stored = await _dictItems.GetByIdAsync(input.BasicId, token)
            ?? throw new InvalidOperationException("字典项不存在或当前范围不可读取。");
        if (DictTagStyles.Merge(stored.Metadata, input.TagStyle) != stored.Metadata)
            throw new InvalidOperationException("当前账号无权修改字典项元数据中的标签样式。");
    }

    /// <summary>
    /// 更新系统字典项状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Status)]
    public async Task<DictItemDetailDto> UpdateDictItemStatusAsync(DictItemStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysDictItem), input.BasicId, input, cancellationToken);

        var result = await _dictDomainService.UpdateDictItemStatusAsync(DictApplicationMapper.ToItemStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateDictionaryAsync(cancellationToken);
        return DictApplicationMapper.ToItemDetailDto(result.DictItem);
    }

    /// <summary>
    /// 更新系统字典状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Status)]
    public async Task<DictDetailDto> UpdateDictStatusAsync(DictStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysDict), input.BasicId, input, cancellationToken);

        var result = await _dictDomainService.UpdateDictStatusAsync(DictApplicationMapper.ToStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateDictionaryAsync(cancellationToken);
        return DictApplicationMapper.ToDetailDto(result.Dict);
    }
}

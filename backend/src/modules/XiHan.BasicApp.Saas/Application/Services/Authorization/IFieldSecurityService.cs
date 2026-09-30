// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 当前用户在某个字段上合并后的有效规则
/// </summary>
public sealed class EffectiveFieldRule
{
    /// <summary>
    /// 字段名（实体属性名）
    /// </summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>
    /// 读取方式（命中规则里最严的一种）
    /// </summary>
    public FieldMaskStrategy MaskStrategy { get; init; }

    /// <summary>
    /// 部分脱敏保留前几位（多条部分脱敏取最少）
    /// </summary>
    public int? MaskKeepHead { get; init; }

    /// <summary>
    /// 部分脱敏保留后几位（多条部分脱敏取最少）
    /// </summary>
    public int? MaskKeepTail { get; init; }

    /// <summary>
    /// 固定文本
    /// </summary>
    public string? MaskReplacement { get; init; }

    /// <summary>
    /// 是否可编辑（任一命中规则只读即只读；脱敏且可编辑为只写：看不到原值但能填新值）
    /// </summary>
    public bool IsEditable { get; init; }

    /// <summary>
    /// 读受保护：不再明文返回，也不能拿来排序、过滤、关键字搜索（否则可按结果反推原值）
    /// </summary>
    public bool IsReadProtected => MaskStrategy != FieldMaskStrategy.None;
}

/// <summary>
/// 字段级安全（FLS）落地：查询门控、读脱敏、写校验
/// </summary>
/// <remarks>
/// 实体一律以类型传入（<c>typeof(SysUser)</c>），必须在字段安全实体目录里登记过，否则当场报错。
/// 查询服务：构建完查询条件后 <see cref="GuardQueryAsync"/>；内部强制约束（租户、归属等）放在门控之后追加，门控只处理此前的条件。
/// 应用服务：新建前 <see cref="EnsureCreatableAsync{TInput}"/>，修改（含状态）前 <see cref="EnsureUpdatableAsync{TInput}"/>。
/// 读脱敏不逐个接口接线：HTTP 响应经 <c>FieldSecurityResponseFilter</c>、后台导出经导出基类，统一调 <see cref="MaskAsync"/>。
/// </remarks>
public interface IFieldSecurityService
{
    /// <summary>
    /// 当前用户在实体上的有效规则（字段名 → 规则）；未登录或无规则返回空。同一请求内按用户与实体缓存。
    /// </summary>
    Task<IReadOnlyDictionary<string, EffectiveFieldRule>> ResolveAsync(Type entityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询门控：就地剔除读受保护字段上的排序、过滤与关键字搜索字段。字段名大小写不敏感（前端列键多为 camelCase）。
    /// </summary>
    Task GuardQueryAsync(QueryConditions conditions, Type entityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 对一份返回数据就地脱敏：沿对象图（含集合、分页、嵌套 DTO）找出属于登记实体的 DTO，按该实体的规则打码。
    /// </summary>
    /// <remarks>
    /// DTO 属于哪个实体由应用层映射器认定（见 <see cref="IFieldSecurityDtoCatalog"/>），只进入本应用与框架程序集里的类型。
    /// 就地修改：调用方传入的应是本次响应自己的对象，不能是缓存里共享的实例。
    /// </remarks>
    Task MaskAsync(object? response, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新建校验：只读字段不能填写（与入参类型的默认值相同视为没填）。
    /// </summary>
    /// <exception cref="InvalidOperationException">填写了当前用户无权填写的字段。</exception>
    Task EnsureCreatableAsync<TInput>(Type entityType, TInput input, CancellationToken cancellationToken = default) where TInput : class, new();

    /// <summary>
    /// 修改校验：只读字段不能改；脱敏字段（含只写）把拿到的脱敏值或空值原样交回时视为没改，并还原成库里的原值，免得脱敏值覆盖真实数据。
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="id">被修改记录的主键</param>
    /// <param name="input">修改入参（与实体同名的属性参与比对）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="InvalidOperationException">改动了当前用户只读的字段。</exception>
    Task EnsureUpdatableAsync<TInput>(Type entityType, long id, TInput input, CancellationToken cancellationToken = default) where TInput : class;
}

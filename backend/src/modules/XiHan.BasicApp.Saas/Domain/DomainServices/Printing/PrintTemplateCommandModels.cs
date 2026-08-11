// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 创建打印模板的领域命令。
/// </summary>
/// <param name="TemplateCode">不可变模板编码。</param>
/// <param name="DataSourceCode">可选数据源编码；为空表示自由模板。</param>
/// <param name="TemplateName">模板名称。</param>
/// <param name="TemplateJson">hiprint 模板 JSON。</param>
/// <param name="EngineVersion">hiprint 引擎版本。</param>
/// <param name="AllowTenantUse">全局模板是否向租户开放。</param>
/// <param name="Status">初始状态。</param>
/// <param name="Sort">显示排序。</param>
/// <param name="Remark">备注。</param>
public sealed record PrintTemplateCreateCommand(
    string TemplateCode,
    string? DataSourceCode,
    string TemplateName,
    string TemplateJson,
    string EngineVersion,
    bool AllowTenantUse,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 更新打印模板的领域命令；模板编码不可变，数据源可在自由模式与已注册契约之间切换。
/// </summary>
/// <param name="Id">模板主键。</param>
/// <param name="ExpectedRowVersion">客户端读取到的行版本。</param>
/// <param name="DataSourceCode">可选数据源编码；为空表示自由模板。</param>
/// <param name="TemplateName">模板名称。</param>
/// <param name="TemplateJson">hiprint 模板 JSON。</param>
/// <param name="EngineVersion">hiprint 引擎版本。</param>
/// <param name="AllowTenantUse">全局模板是否向租户开放。</param>
/// <param name="Sort">显示排序。</param>
/// <param name="Remark">备注。</param>
public sealed record PrintTemplateUpdateCommand(
    long Id,
    long ExpectedRowVersion,
    string? DataSourceCode,
    string TemplateName,
    string TemplateJson,
    string EngineVersion,
    bool AllowTenantUse,
    int Sort,
    string? Remark);

/// <summary>
/// 变更打印模板状态的领域命令。
/// </summary>
/// <param name="Id">模板主键。</param>
/// <param name="ExpectedRowVersion">客户端读取到的行版本。</param>
/// <param name="Status">目标状态。</param>
/// <param name="Remark">可选操作备注。</param>
public sealed record PrintTemplateStatusChangeCommand(
    long Id,
    long ExpectedRowVersion,
    EnableStatus Status,
    string? Remark);

/// <summary>
/// 删除打印模板的领域命令。
/// </summary>
/// <param name="Id">模板主键。</param>
/// <param name="ExpectedRowVersion">客户端读取到的行版本。</param>
public sealed record PrintTemplateDeleteCommand(long Id, long ExpectedRowVersion);

/// <summary>
/// 打印模板命令执行结果。
/// </summary>
/// <param name="Template">已经持久化的模板实体。</param>
public sealed record PrintTemplateCommandResult(SysPrintTemplate Template);

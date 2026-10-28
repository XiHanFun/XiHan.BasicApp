// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 实体的写入不进数据变更日志。全审计实体（<see cref="AppAuditedEntity"/>）缺省都记前后值；
/// 写得极频繁的运行态数据（会话、个人偏好）与只存密文和计数的账号安全数据标上它。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DiffLogIgnoredAttribute : Attribute;

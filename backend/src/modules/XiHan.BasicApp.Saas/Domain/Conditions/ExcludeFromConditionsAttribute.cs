// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Conditions;

/// <summary>
/// 标在实体属性上：该列不作为数据行属性参与权限条件
/// </summary>
/// <remarks>
/// 实体按权限码登记为条件资源后，公开的标量属性按 camelCase 映射成 <c>resource.&lt;名&gt;</c>；
/// 密码、密钥这类列用它排除，试算的取值说明里也就不会出现。
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class ExcludeFromConditionsAttribute : Attribute;

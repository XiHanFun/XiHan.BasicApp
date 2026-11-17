// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 平台库与各租户独立库的升级预览
/// </summary>
/// <param name="Plans">读得到的库的预览，平台库在前</param>
/// <param name="Failures">读不到、给不出预览的租户独立库</param>
public sealed record SchemaPlanResult(IReadOnlyList<SchemaPlan> Plans, IReadOnlyList<SchemaUpgradeFailure> Failures);

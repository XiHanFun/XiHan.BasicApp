// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Maintenance.Queries;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Maintenance.Operations.Versions;

/// <summary>
/// 版本操作共用的核对
/// </summary>
internal static class VersionGuards
{
    /// <summary>
    /// 库名是平台库或已就绪的租户独立库
    /// </summary>
    /// <returns>不认识的库名即 <see cref="MaintenanceErrors.SchemaRecordNotFound"/></returns>
    public static async Task<Result> RequireKnownAsync(SchemaQueries queries, string database, InvocationContext context)
    {
        var known = await queries.IsKnownAsync(database, context.CancellationToken).ConfigureAwait(false);
        if (!known.TryGetValue(out var isKnown))
        {
            return known.Error!;
        }

        return isKnown
            ? Result.Success()
            : Error.NotFound(MaintenanceErrors.SchemaRecordNotFound, MaintenanceMessages.SchemaRecordNotFound, MessageArguments.Of(("id", database)));
    }
}

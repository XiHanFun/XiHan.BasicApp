// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Seeds;

/// <summary>
/// 平台的缺省存储配置 <c>local-default</c>（本地目录、桶 <c>files</c>，4.x 同）：编码不存在才写入，之后由运营维护。
/// </summary>
/// <param name="service">配置服务。</param>
public sealed class StorageConfigSeeder(StorageConfigService service) : ISeeder
{
    /// <summary>
    /// 缺省配置的编码。
    /// </summary>
    public const string LocalDefault = "local-default";

    /// <inheritdoc />
    public string Name => "files.storage-configs";

    /// <inheritdoc />
    public int Order => SeedOrders.PlatformData + 1;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if ((await service.FindByCodeAsync(TenantIds.Platform, LocalDefault, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Result.Success();
        }

        var created = await service.CreateAsync(TenantIds.Platform, new StorageConfigDefinition(LocalDefault, "Local storage", StorageType.Local, null, null, "files", null, null, 10, null), isDefault: true, StorageConfigStatus.Active, context.CancellationToken).ConfigureAwait(false);
        return created.IsFailure ? created.Error! : Result.Success();
    }
}

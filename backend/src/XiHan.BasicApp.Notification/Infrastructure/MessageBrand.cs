// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// 模板变量 <c>brand</c> 的补值
/// </summary>
/// <remarks>
/// 调用方没传即补上发送这一侧站点设置的系统名（经 Contracts 的 <see cref="ISiteBrand"/>，没装 Settings 即应用名）；传了照用
/// </remarks>
/// <param name="brands">站点的系统名</param>
public sealed class MessageBrand(IEnumerable<ISiteBrand> brands)
{
    /// <summary>
    /// 模板变量名
    /// </summary>
    public const string Parameter = "brand";

    private readonly ISiteBrand? _brand = brands.FirstOrDefault();

    /// <summary>
    /// 补上品牌名的模板参数
    /// </summary>
    /// <param name="parameters">调用方给的参数</param>
    /// <param name="owner">发送这一侧</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask<Result<IReadOnlyList<KeyValuePair<string, string?>>>> CompleteAsync(IReadOnlyList<KeyValuePair<string, string?>> parameters, string owner, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (parameters.Any(static p => string.Equals(p.Key, Parameter, StringComparison.Ordinal)))
        {
            return Result.Success(parameters);
        }

        var name = AppInfo.Name;
        if (_brand is not null)
        {
            var resolved = await _brand.NameOfAsync(owner, cancellationToken).ConfigureAwait(false);
            if (!resolved.TryGetValue(out var brand))
            {
                return resolved.Error!;
            }

            name = brand;
        }

        return Result.Success<IReadOnlyList<KeyValuePair<string, string?>>>([.. parameters, new KeyValuePair<string, string?>(Parameter, name)]);
    }
}

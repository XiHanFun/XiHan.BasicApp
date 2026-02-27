using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Rbac.Application;

/// <summary>
/// 基于数据注解的校验扩展
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// 使用 DTO/Command 上的 <see cref="ValidationAttribute"/> 进行校验，不通过则抛出 <see cref="ValidationException"/>
    /// </summary>
    /// <param name="instance">待校验的 DTO 或 Command 实例</param>
    /// <param name="serviceProvider">可选，用于 <see cref="ValidationContext"/> 解析服务（如 IValidatableObject）</param>
    public static void ValidateAnnotations(this object instance, IServiceProvider? serviceProvider = null)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var context = new ValidationContext(instance, serviceProvider, null);
        Validator.ValidateObject(instance, context, validateAllProperties: true);
    }
}

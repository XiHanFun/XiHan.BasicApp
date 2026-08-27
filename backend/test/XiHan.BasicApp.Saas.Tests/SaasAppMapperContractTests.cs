// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Saas.Application;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 应用层映射器契约测试。
/// </summary>
/// <remarks>
/// 映射器是手写的逐字段搬运，五十多个文件三百多个方法，最典型的缺陷是<b>漏搬一个字段</b>：
/// 编译通过、接口 200、只是前端某一列永远是空的。这类问题靠人眼评审很难稳定发现。
/// <para>
/// 本类不逐个映射器写用例，而是反射遍历全部映射方法：给源对象每个属性塞上互不相同的值，
/// 跑一遍映射，再逐个比对"源与目标同名同类型"的属性是否一致——只要漏搬就会被抓出来，
/// 并在失败消息里列出具体是哪个映射器的哪个字段。
/// </para>
/// </remarks>
public sealed class SaasAppMapperContractTests
{
    /// <summary>
    /// 刻意不搬运的字段（映射器.方法.字段）。
    /// </summary>
    /// <remarks>
    /// 列入即声明“这个字段不该原样出去”，每一条都得有安全理由。
    /// </remarks>
    private static readonly HashSet<string> DeliberatelyNotCarriedOver = new(StringComparer.Ordinal)
    {
        // 加密配置项不回传明文，前端以「已加密」提示替代
        "ConfigApplicationMapper.ToDetailDto.ConfigValue",
        "ConfigApplicationMapper.ToDetailDto.DefaultValue"
    };

    /// <summary>
    /// 映射器必须能被发现，否则本类所有断言都是空跑。
    /// </summary>
    [Fact]
    public void Mappers_ShouldBeDiscoverable()
    {
        Assert.True(MapperTypes().Count >= 50, $"只发现了 {MapperTypes().Count} 个映射器，扫描条件可能失效了。");
        Assert.True(MapperMethods().Count >= 250, $"只发现了 {MapperMethods().Count} 个映射方法，扫描条件可能失效了。");
    }

    /// <summary>
    /// 映射器一律是无状态的静态类，命名以 Mapper 结尾。
    /// </summary>
    [Fact]
    public void Mappers_ShouldBeStaticAndNamedMapper()
    {
        var offenders = MapperTypes()
            .Where(type => !type.Name.EndsWith("Mapper", StringComparison.Ordinal))
            .Select(type => type.FullName ?? type.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下映射命名空间下的类型未以 Mapper 结尾：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 映射器不得持有任何实例成员或可变静态状态——它必须是纯函数集合，才能被任意并发调用。
    /// </summary>
    [Fact]
    public void Mappers_ShouldNotHoldMutableState()
    {
        var offenders = MapperTypes()
            .SelectMany(type => type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(field => !field.IsLiteral && !field.IsInitOnly)
                .Select(field => $"{type.Name}.{field.Name}"))
            .ToList();

        Assert.True(offenders.Count == 0, $"映射器出现了可变状态字段：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 映射方法统一以 To / From 开头，读起来就是“映射成什么”或“从什么映射而来”。
    /// </summary>
    /// <remarks>
    /// <c>From</c> 形式用在日志链路追踪那类“多种源归一”的映射上（每一类日志一个重载）。
    /// </remarks>
    [Fact]
    public void MapperMethods_ShouldBeNamedWithDirectionPrefix()
    {
        var offenders = MapperMethods()
            .Where(item => !item.Method.Name.StartsWith("To", StringComparison.Ordinal)
                           && !item.Method.Name.StartsWith("From", StringComparison.Ordinal))
            .Select(item => $"{item.Type.Name}.{item.Method.Name}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下映射方法未以 To / From 开头：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 映射方法必须拒绝 null 源对象，而不是返回一个全空的 DTO 把问题往下游推。
    /// </summary>
    /// <remarks>
    /// 上游拿到 null 往往意味着"这条记录查不到"，映射器如果宽容处理，
    /// 前端收到的就是一条字段全空的记录而非明确的错误。
    /// </remarks>
    [Fact]
    public void MapperMethods_ShouldRejectNullSource()
    {
        var offenders = new List<string>();
        var checkedCount = 0;

        foreach (var (type, method) in MapperMethods())
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0
                || !IsReferenceSource(parameters[0].ParameterType)
                || !parameters.Skip(1).All(parameter => parameter.HasDefaultValue))
            {
                continue;
            }

            checkedCount++;
            var arguments = BuildArguments(parameters, source: null);

            try
            {
                method.Invoke(null, arguments);
                offenders.Add($"{type.Name}.{method.Name} 接受了 null 源对象");
            }
            catch (TargetInvocationException exception) when (exception.InnerException is ArgumentNullException)
            {
                // 期望行为
            }
            catch (TargetInvocationException exception)
            {
                offenders.Add($"{type.Name}.{method.Name} 抛出了 {exception.InnerException?.GetType().Name} 而非 ArgumentNullException");
            }
        }

        Assert.True(checkedCount >= 200, $"只覆盖到 {checkedCount} 个映射方法，扫描条件可能失效了。");
        Assert.True(offenders.Count == 0, $"以下映射方法未对 null 源对象 fail-fast：{string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// 同名同类型的字段必须原样搬过去，一个都不许漏。
    /// </summary>
    /// <remarks>
    /// 做法是给源对象每个可写属性塞上互不相同的值，映射后逐个比对目标侧同名同类型属性。
    /// 类型不同（例如实体的枚举映射成 DTO 的文本描述）或目标独有的派生字段不在比对范围内，
    /// 因为那些本就不是"直接搬运"。
    /// </remarks>
    [Fact]
    public void MapperMethods_ShouldNotDropSameNamedFields()
    {
        var offenders = new List<string>();
        var comparedFields = 0;

        foreach (var (type, method) in MapperMethods())
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0
                || !IsReferenceSource(parameters[0].ParameterType)
                || !parameters.Skip(1).All(parameter => parameter.HasDefaultValue)
                || parameters[0].ParameterType.GetConstructor(Type.EmptyTypes) is null)
            {
                continue;
            }

            object source;
            object? mapped;
            try
            {
                source = Activator.CreateInstance(parameters[0].ParameterType)!;
                FillWithDistinctValues(source);
                mapped = method.Invoke(null, BuildArguments(parameters, source));
            }
            catch (Exception)
            {
                // 源对象无法用通用填充满足其领域校验时跳过：此处只负责抓"漏搬字段"，不负责验证校验规则
                continue;
            }

            if (mapped is null)
            {
                continue;
            }

            foreach (var targetProperty in mapped.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var sourceProperty = parameters[0].ParameterType
                    .GetProperty(targetProperty.Name, BindingFlags.Public | BindingFlags.Instance);
                if (sourceProperty is null
                    || !sourceProperty.CanRead
                    || sourceProperty.PropertyType != targetProperty.PropertyType
                    || !IsComparableValue(targetProperty.PropertyType))
                {
                    continue;
                }

                if (DeliberatelyNotCarriedOver.Contains($"{type.Name}.{method.Name}.{targetProperty.Name}"))
                {
                    continue;
                }

                comparedFields++;
                var expected = sourceProperty.GetValue(source);
                var actual = targetProperty.GetValue(mapped);
                if (!Equals(expected, actual))
                {
                    offenders.Add($"{type.Name}.{method.Name} 丢了字段 {targetProperty.Name}（源={expected ?? "null"} 目标={actual ?? "null"}）");
                }
            }
        }

        Assert.True(comparedFields >= 500, $"只比对到 {comparedFields} 个字段，扫描条件可能失效了。");
        Assert.True(offenders.Count == 0, $"以下映射存在字段丢失：{string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// 映射方法必须真的产出对象，不能返回 null 让调用方再判一次空。
    /// </summary>
    [Fact]
    public void MapperMethods_ShouldNotReturnNullForValidSource()
    {
        var offenders = new List<string>();

        foreach (var (type, method) in MapperMethods())
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0
                || !IsReferenceSource(parameters[0].ParameterType)
                || !parameters.Skip(1).All(parameter => parameter.HasDefaultValue)
                || parameters[0].ParameterType.GetConstructor(Type.EmptyTypes) is null
                || method.ReturnType == typeof(void))
            {
                continue;
            }

            object? mapped;
            try
            {
                var source = Activator.CreateInstance(parameters[0].ParameterType)!;
                FillWithDistinctValues(source);
                mapped = method.Invoke(null, BuildArguments(parameters, source));
            }
            catch (Exception)
            {
                continue;
            }

            if (mapped is null)
            {
                offenders.Add($"{type.Name}.{method.Name}");
            }
        }

        Assert.True(offenders.Count == 0, $"以下映射方法对有效源对象返回了 null：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 加密配置项的明文绝不能随详情接口出去，未加密项则必须原样返回。
    /// </summary>
    /// <remarks>
    /// 这是上面"字段丢失"扫描里唯一的豁免项，在这里正面锁死它的两个方向：
    /// 少了屏蔽等于泄密，多了屏蔽等于普通配置在前端变成空白。
    /// </remarks>
    /// <param name="isEncrypted">配置项是否加密。</param>
    /// <param name="expectValueExposed">是否应回传明文。</param>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ConfigDetail_ShouldMaskValuesOnlyForEncryptedEntries(bool isEncrypted, bool expectValueExposed)
    {
        var config = new SysConfig
        {
            ConfigName = "演示配置",
            ConfigKey = "saas.demo",
            ConfigValue = "明文值",
            DefaultValue = "默认值",
            IsEncrypted = isEncrypted
        };

        var detail = ConfigApplicationMapper.ToDetailDto(config);

        if (expectValueExposed)
        {
            Assert.Equal("明文值", detail.ConfigValue, StringComparer.Ordinal);
            Assert.Equal("默认值", detail.DefaultValue, StringComparer.Ordinal);
        }
        else
        {
            Assert.Null(detail.ConfigValue);
            Assert.Null(detail.DefaultValue);
        }

        // 无论是否加密，"有没有值"这一事实都要如实告知前端，否则前端无法区分"未配置"与"已加密"
        Assert.True(detail.HasCurrentValue);
        Assert.Equal(isEncrypted, detail.IsEncrypted);
    }

    /// <summary>
    /// 豁免名单必须与代码对得上：字段被改名或映射器被删除后，名单会悄悄失效。
    /// </summary>
    [Fact]
    public void DeliberateExemptions_ShouldStayInSyncWithCode()
    {
        var stale = DeliberatelyNotCarriedOver
            .Where(entry =>
            {
                var parts = entry.Split('.');
                var mapper = MapperTypes().FirstOrDefault(type => string.Equals(type.Name, parts[0], StringComparison.Ordinal));
                var method = mapper?.GetMethod(parts[1], BindingFlags.Public | BindingFlags.Static);
                return method is null
                       || method.ReturnType.GetProperty(parts[2], BindingFlags.Public | BindingFlags.Instance) is null;
            })
            .ToList();

        Assert.True(stale.Count == 0, $"豁免名单里的条目已不存在，请一并清理：{string.Join(", ", stale)}");
    }

    /// <summary>
    /// 枚举映射命名空间下的全部静态类型。
    /// </summary>
    /// <returns>映射器类型集合。</returns>
    private static List<Type> MapperTypes()
    {
        return typeof(SaasApplicationService).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: true, IsSealed: true })
            .Where(type => type.Namespace?.StartsWith("XiHan.BasicApp.Saas.Application.Mappers", StringComparison.Ordinal) == true)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 枚举全部公开映射方法。
    /// </summary>
    /// <returns>类型与方法对。</returns>
    private static List<(Type Type, MethodInfo Method)> MapperMethods()
    {
        return MapperTypes()
            .SelectMany(type => type
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Select(method => (Type: type, Method: method)))
            .ToList();
    }

    /// <summary>
    /// 判断参数类型是否为可传 null 的引用型源对象。
    /// </summary>
    /// <param name="type">参数类型。</param>
    /// <returns>是否引用型源对象。</returns>
    private static bool IsReferenceSource(Type type)
    {
        return type.IsClass && type != typeof(string) && !type.IsArray;
    }

    /// <summary>
    /// 判断类型是否适合做逐字段等值比对。
    /// </summary>
    /// <param name="type">属性类型。</param>
    /// <returns>是否可比对。</returns>
    private static bool IsComparableValue(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive
               || underlying.IsEnum
               || underlying == typeof(string)
               || underlying == typeof(decimal)
               || underlying == typeof(Guid)
               || underlying == typeof(DateTime)
               || underlying == typeof(DateTimeOffset);
    }

    /// <summary>
    /// 组装调用参数：首参为源对象，其余取声明的默认值。
    /// </summary>
    /// <param name="parameters">方法参数。</param>
    /// <param name="source">源对象。</param>
    /// <returns>调用参数数组。</returns>
    private static object?[] BuildArguments(ParameterInfo[] parameters, object? source)
    {
        var arguments = new object?[parameters.Length];
        arguments[0] = source;
        for (var index = 1; index < parameters.Length; index++)
        {
            arguments[index] = parameters[index].DefaultValue;
        }

        return arguments;
    }

    /// <summary>
    /// 给对象每个可写属性塞上互不相同的值，使"漏搬字段"必然表现为比对不等。
    /// </summary>
    /// <param name="instance">待填充对象。</param>
    private static void FillWithDistinctValues(object instance)
    {
        var seed = 1;
        foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite || property.GetSetMethod() is null)
            {
                seed++;
                continue;
            }

            var value = BuildDistinctValue(property.PropertyType, seed);
            if (value is not null)
            {
                try
                {
                    property.SetValue(instance, value);
                }
                catch (Exception)
                {
                    // 属性自带校验时保持原值，不影响本用例要抓的"漏搬"
                }
            }

            seed++;
        }
    }

    /// <summary>
    /// 按类型与序号构造一个可辨识的取值。
    /// </summary>
    /// <param name="propertyType">属性类型。</param>
    /// <param name="seed">序号。</param>
    /// <returns>取值（不支持的类型返回 null）。</returns>
    private static object? BuildDistinctValue(Type propertyType, int seed)
    {
        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (type == typeof(string))
        {
            return $"值{seed}";
        }

        if (type == typeof(bool))
        {
            return seed % 2 == 0;
        }

        if (type == typeof(int))
        {
            return seed;
        }

        if (type == typeof(long))
        {
            return (long)seed;
        }

        if (type == typeof(short))
        {
            return (short)seed;
        }

        if (type == typeof(byte))
        {
            return (byte)seed;
        }

        if (type == typeof(decimal))
        {
            return seed + 0.5m;
        }

        if (type == typeof(double))
        {
            return seed + 0.5d;
        }

        if (type == typeof(DateTimeOffset))
        {
            return new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(seed);
        }

        if (type == typeof(DateTime))
        {
            return new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(seed);
        }

        if (type == typeof(Guid))
        {
            return Guid.NewGuid();
        }

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.Length > 0 ? values.GetValue(values.Length - 1) : null;
        }

        return null;
    }
}

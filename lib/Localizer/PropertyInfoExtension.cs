using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Utharn.Library.Localizer;

public class PropertyHelper<T> where T : class
{
    public static string GetDisplayName(Expression<Func<T, object>> expression, ILocalizerService localizerService)
    {
        return expression.GetPropertyInfo().GetDisplayName(localizerService);
    }
}

public static class PropertyInfoExtension
{
    public static TAttribute? GetAttribute<TAttribute>(this PropertyInfo propertyInfo)
        where TAttribute : Attribute
    {
        return propertyInfo.GetCustomAttribute<TAttribute>();
    }

    /// <summary>
    ///     Gets the corresponding <see cref="PropertyInfo" /> from an <see cref="Expression" />.
    /// </summary>
    /// <param name="expression">The expression that selects the property to get info on.</param>
    /// <returns>The property info collected from the expression.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="expression" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The expression doesn't indicate a valid property."</exception>
    public static PropertyInfo GetPropertyInfo<T>(this Expression<Func<T, object>> expression)
    {
        return expression?.Body switch
        {
            null => throw new ArgumentNullException(nameof(expression)),
            UnaryExpression unaryExp when unaryExp.Operand is MemberExpression memberExp => (PropertyInfo)memberExp
                .Member,
            MemberExpression memberExp => (PropertyInfo)memberExp.Member,
            _ => throw new ArgumentException($"The expression doesn't indicate a valid property. [ {expression} ]")
        };
    }

    public static string GetDisplayName(this PropertyInfo prop, ILocalizerService service)
    {
        List<LocalizeAttribute> attr = GetLocalizeAttribute(prop).ToList();
        if (attr.Count == 0)
        {
            return prop.ToString() ?? string.Empty;
        }

        string? currentLang = service.GetCurrentLanguage();
        if (currentLang == null)
        {
            return prop.ToString() ?? string.Empty;
        }

        LocalizeAttribute? target = attr.FirstOrDefault(a => a.Language == currentLang);
        if (target == null)
        {
            return prop.ToString() ?? string.Empty;
        }

        return target.Value;
    }

    private static IEnumerable<LocalizeAttribute> GetLocalizeAttribute(PropertyInfo value)
    {
        return value.GetCustomAttributes<LocalizeAttribute>();
    }
}
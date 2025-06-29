using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Utharn.Library.Localizer;

public static class Extensions
{
    public static string GetLocalizedString(this Enum enu, ILocalizerService service)
    {
        List<LocalizeAttribute> attr = GetLocalizeAttribute(enu).ToList();
        if (attr.Count == 0)
        {
            return enu.ToString();
        }

        string? currentLang = service.GetCurrentLanguage();
        if (currentLang == null)
        {
            return enu.ToString();
        }

        LocalizeAttribute? target = attr.FirstOrDefault(a => a.Language == currentLang);
        if (target == null)
        {
            return enu.ToString();
        }

        return target.Value;
    }

    private static IEnumerable<LocalizeAttribute> GetLocalizeAttribute(object value)
    {
        Type type = value.GetType();
        if (!type.IsEnum && value == null)
        {
            throw new ArgumentException(string.Format("Type {0} is not an enum", type));
        }

        // Get the enum field.
        FieldInfo? field = type.GetField(value?.ToString() ?? throw new InvalidOperationException());
        return field != null ? field.GetCustomAttributes<LocalizeAttribute>() : Array.Empty<LocalizeAttribute>();
    }
}
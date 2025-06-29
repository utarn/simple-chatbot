using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;
using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Common.Extensions;

public static class EnumExtensions
{
    public static string? GetDisplayName(this Enum enumValue)
    {
        return enumValue.GetType()
            .GetMember(enumValue.ToString())
            .First()
            .GetCustomAttribute<DisplayAttribute>()
            ?.GetName();
    }
    
    public static SelectList EnumToSelectList<TEnum>(this IEnumerable<TEnum> enumObj, object? selectedValue, ILocalizerService? localizerService) where TEnum : struct, Enum
    {
        var selectList = enumObj
            .Select(x => new SelectListItem
            {
                Text = localizerService != null ? x.GetLocalizedString(localizerService) : x.GetDisplayName(),
                Value = x.ToString()
            }).ToList();
        return new SelectList(selectList, "Value", "Text", selectedValue);
    }
}

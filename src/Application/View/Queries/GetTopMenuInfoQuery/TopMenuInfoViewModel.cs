using System.Security.Cryptography;
using System.Text;
using ChatbotApi.Domain.Entities;
using Utharn.Library.Localizer;

namespace ChatbotApi.Application.View.Queries.GetTopMenuInfoQuery;

public class TopMenuInfoViewModel
{
    [Localize(Value = "อีเมล")] public string Email { get; set; } = default!;
    [Localize(Value = "ชื่อ นามสกุล")] public string? Name { get; set; }
    public string FullName { get; set; } = default!;

    public string ShortName
    {
        get
        {
            string? str = Name;
            if (Name == null)
            {
                str = Email;
            }

            str ??= string.Empty;
            return str.Contains("@")
                ? str.Substring(0, str.IndexOf("@", StringComparison.Ordinal) - 1)
                : str;
        }
    }

    public string GAvatar
    {
        get
        {
            byte[] tmpSource = Encoding.UTF8.GetBytes(Email);
            byte[] hashBytes = MD5.Create().ComputeHash(tmpSource);
            StringBuilder sb = new();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }

            return $"https://www.gravatar.com/avatar/{sb}?s=200".ToLower();
        }
    }

    public static TopMenuInfoViewModel MappingFunction(ApplicationUser user)
    {
        return new TopMenuInfoViewModel
        {
            Email = user.Email ?? "Anonymous",
            Name = user.UserName,
            FullName = user.UserName ?? "Anonymous"
        };
    }
}

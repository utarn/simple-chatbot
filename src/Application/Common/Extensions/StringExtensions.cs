using System.IO;
using System.Linq;

namespace ChatbotApi.Application.Common.Extensions
{
    public static class StringExtensions
    {
        public static string MakeValidFileName(this string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"[{0}]", invalidChars);
            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        /// <summary>
        /// Converts the first character of the string to uppercase and the rest to lowercase.
        /// </summary>
        public static string ToInitCap(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            if (input.Length == 1)
                return input.ToUpper();
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        /// <summary>
        /// Returns a substring of the input string with a maximum length, appending "..." if truncated.
        /// </summary>
        public static string GetSubString(this string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || maxLength <= 0)
                return string.Empty;
            if (input.Length <= maxLength)
                return input;
            return input.Substring(0, maxLength) + "...";
        }
    }
}
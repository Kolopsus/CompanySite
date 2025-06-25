using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CompanySite.Services
{
    public static class SecurityExtensions
    {
        private static readonly string[] DangerousPatterns = {
            @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>",
            @"javascript:",
            @"vbscript:",
            @"on\w+\s*=",
            @"<iframe",
            @"<object",
            @"<embed",
            @"<link",
            @"<meta"
        };

        public static string SanitizeHtml(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove dangerous HTML patterns
            var sanitized = input;
            foreach (var pattern in DangerousPatterns)
            {
                sanitized = Regex.Replace(sanitized, pattern, "", RegexOptions.IgnoreCase);
            }

            return sanitized.Trim();
        }

        public static bool ContainsSqlInjection(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var sqlPatterns = new[]
            {
                @"\b(select|insert|update|delete|drop|create|alter|exec|execute|union|script)\b",
                @"(-{2})|(%27)|(;)|(\|\|)|(\*)|(\bor\b)|(\band\b)"
            };

            return sqlPatterns.Any(pattern => 
                Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        public static string HashSensitiveData(this string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
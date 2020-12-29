using System;
using System.Collections.Generic;
using System.Text;

namespace FakeOrm.AzureTables.Extensions
{
    public static class StringExtension
    {
        public static string Underscored(this string s)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < s.Length; ++i)
            {
                if (ShouldUnderscore(i, s))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(s[i]));
            }

            return builder.ToString();
        }

        private static bool ShouldUnderscore(int i, string s)
        {
            if (i == 0 || i >= s.Length || s[i] == '_') return false;

            var curr = s[i];
            var prev = s[i - 1];
            var next = i < s.Length - 2 ? s[i + 1] : '_';

            return prev != '_' && ((char.IsUpper(curr) && (char.IsLower(prev) || char.IsLower(next))) ||
                (char.IsNumber(curr) && (!char.IsNumber(prev))));
        }

        public static Guid ToGuid(this string vlr)
        {
            Guid newValue;

            if (!Guid.TryParse(vlr, out newValue))
                throw new Exception("Guid.TryParse not executed.");

            return newValue;
        }

    }
}

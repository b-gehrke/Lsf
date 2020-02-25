using System;

namespace Lsf.Util
{
    public static class StringExtensions
    {
        public static string MaxSubstring(this string str, int startIndex, int length)
        {
            return str.Substring(startIndex, Math.Min(str.Length - startIndex, length));
        }
    }
}
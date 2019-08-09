using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VirtoCommerce.SitemapsModule.Data.Extensions
{
    public static class StringExtensions
    {
        public static bool IsJson(this string source, ref JToken token)
        {
            var trimmed = source.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]") ||
                trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                try
                {
                    token = JToken.Parse(source);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}

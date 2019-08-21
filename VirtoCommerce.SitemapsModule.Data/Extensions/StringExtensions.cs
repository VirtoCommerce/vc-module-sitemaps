using Newtonsoft.Json.Linq;

namespace VirtoCommerce.SitemapsModule.Data.Extensions
{
    public static class StringExtensions
    {
        public static bool TryParseJson(this string json, out JToken jToken)
        {
            try
            {
                jToken = JToken.Parse(json);
                return true;
            }
            catch
            {
                jToken = null;
                return false;
            }
        }
    }
}

using System.Text.RegularExpressions;

namespace VirtoCommerce.SitemapsModule.Web.Extensions
{
    public static class StringExtensions
    {
        // We cannot use storeId.IndexOfAny(Path.GetInvalidFileNameChars()) != -1 to validate path because default
        // sanitizer for Sonar Cube do not trust it, so we use Regex here with same logic. Check this out
        // https://community.sonarsource.com/t/help-sonarcloud-with-understanding-the-usage-of-untrusted-and-tainted-input/9873/7
        public static bool IsValidFolderName(this string folderName) => Regex.IsMatch(folderName, "^[a-zA-Z0-9]+$");
    }
}

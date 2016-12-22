namespace VirtoCommerce.SitemapsModule.Core.Services
{
    public interface ISitemapUrlBuilder
    {
        string CreateAbsoluteUrl(string urlTemplate, string baseUrl, string language = null, string semanticUrl = null);
    }
}
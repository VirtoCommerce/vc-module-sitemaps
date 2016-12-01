using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SitemapsModule.Core.Models
{
    public class SitemapItem : AuditableEntity
    {
        public string Title { get; set; }

        public string ImageUrl { get; set; }

        public string ObjectId { get; set; }

        public string ObjectType { get; set; }
    }
}
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SitemapsModule.Core.Model
{
    public class SitemapItem : AuditableEntity
    {
        public string Title { get; set; }

        public string ImageUrl { get; set; }

        public string AbsoluteUrl { get; set; }

        public string ObjectId { get; set; }

        public string ObjectType { get; set; }
    }
}
using Omu.ValueInjecter;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Model;

namespace VirtoCommerce.SitemapsModule.Data.Model
{
    public class SitemapItemEntity : AuditableEntity
    {
        [Required]
        [StringLength(256)]
        public string Title { get; set; }

        [StringLength(512)]
        public string ImageUrl { get; set; }

        [Required]
        [StringLength(128)]
        public string ObjectId { get; set; }

        [Required]
        [StringLength(128)]
        public string ObjectType { get; set; }

        [ForeignKey("Sitemap")]
        [StringLength(128)]
        public string SitemapId { get; set; }

        public virtual SitemapEntity Sitemap { get; set; }

        public virtual SitemapItem ToModel(SitemapItem sitemapItem)
        {
            if (sitemapItem == null)
            {
                throw new ArgumentNullException("sitemapItem");
            }

            sitemapItem.InjectFrom(this);

            return sitemapItem;
        }

        public virtual SitemapItemEntity FromModel(SitemapItem sitemapItem, PrimaryKeyResolvingMap pkMap)
        {
            if (sitemapItem == null)
            {
                throw new ArgumentNullException("sitemapItem");
            }
            if (pkMap == null)
            {
                throw new ArgumentNullException("pkMap");
            }

            pkMap.AddPair(sitemapItem, this);

            this.InjectFrom(sitemapItem);

            return this;
        }

        public virtual void Patch(SitemapItemEntity target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.InjectFrom(this);
        }
    }
}
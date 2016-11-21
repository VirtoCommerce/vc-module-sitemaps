using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VirtoCommerce.Platform.Core.Common;

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
        [StringLength(2048)]
        public string AbsoluteUrl { get; set; }

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

        public virtual void Patch(SitemapItemEntity target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.AbsoluteUrl = AbsoluteUrl;
            target.ImageUrl = ImageUrl;
            target.ObjectType = ObjectType;
            target.Title = Title;
        }
    }
}
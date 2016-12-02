using Omu.ValueInjecter;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Models;

namespace VirtoCommerce.SitemapsModule.Data.Models
{
    public class SitemapEntity : AuditableEntity
    {
        public SitemapEntity()
        {
            Items = new NullCollection<SitemapItemEntity>();
        }

        [Required]
        [StringLength(256)]
        public string Filename { get; set; }

        [Required]
        [StringLength(64)]
        public string StoreId { get; set; }

        public virtual ObservableCollection<SitemapItemEntity> Items { get; set; }

        public virtual Sitemap ToModel(Sitemap sitemap)
        {
            if (sitemap == null)
            {
                throw new ArgumentNullException("sitemap");
            }

            sitemap.InjectFrom(this);

            return sitemap;
        }

        public virtual SitemapEntity FromModel(Sitemap sitemap, PrimaryKeyResolvingMap pkMap)
        {
            if (sitemap == null)
            {
                throw new ArgumentNullException("sitemap");
            }
            if (pkMap == null)
            {
                throw new ArgumentNullException("pkMap");
            }

            pkMap.AddPair(sitemap, this);

            this.InjectFrom(sitemap);

            return this;
        }

        public virtual void Patch(SitemapEntity sitemapEntity)
        {
            if (sitemapEntity == null)
            {
                throw new ArgumentNullException("sitemapEntity");
            }

            sitemapEntity.InjectFrom(this);
        }
    }
}
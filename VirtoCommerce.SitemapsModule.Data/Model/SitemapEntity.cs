using Omu.ValueInjecter;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Core.Model;

namespace VirtoCommerce.SitemapsModule.Data.Model
{
    public class SitemapEntity : AuditableEntity
    {
        public SitemapEntity()
        {
            Items = new NullCollection<SitemapItemEntity>();
        }

        [Required]
        [StringLength(64)]
        public string StoreId { get; set; }

        [Required]
        [StringLength(256)]
        public string Filename { get; set; }

        public virtual ObservableCollection<SitemapItemEntity> Items { get; set; }

        public virtual Sitemap ToModel(Sitemap sitemap)
        {
            if (sitemap == null)
            {
                throw new ArgumentNullException("sitemap");
            }

            sitemap.InjectFrom(this);

            sitemap.Items = Items.Select(i => i.ToModel(AbstractTypeFactory<SitemapItem>.TryCreateInstance())).ToList();

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

            if (sitemap.Items != null)
            {
                Items = new ObservableCollection<SitemapItemEntity>(sitemap.Items.Select(i => AbstractTypeFactory<SitemapItemEntity>.TryCreateInstance().FromModel(i, pkMap)));
            }

            return this;
        }

        public virtual void Patch(SitemapEntity target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.InjectFrom(this);

            if (!Items.IsNullCollection())
            {
                Items.Patch(target.Items, (sourceItem, targetItem) => sourceItem.Patch(targetItem));
            }
        }
    }
}
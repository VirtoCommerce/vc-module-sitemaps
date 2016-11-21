using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using VirtoCommerce.Platform.Core.Common;

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

        public virtual void Patch(SitemapEntity target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            target.Filename = Filename;
            target.StoreId = StoreId;

            if (!Items.IsNullCollection())
            {
                Items.Patch(target.Items, (sourceItem, targetItem) => sourceItem.Patch(targetItem));
            }
        }
    }
}
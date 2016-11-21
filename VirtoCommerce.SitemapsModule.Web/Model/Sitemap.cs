using System;
using System.Collections.ObjectModel;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SitemapsModule.Data.Model;

namespace VirtoCommerce.SitemapsModule.Web.Model
{
    public class Sitemap
    {
        public Sitemap()
        {
            Items = new NullCollection<SitemapItem>();
        }

        public string Id { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string StoreId { get; set; }

        public string Filename { get; set; }

        public ObservableCollection<SitemapItem> Items { get; set; }

        public virtual Sitemap FromDataModel(SitemapEntity source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            Id = source.Id;
            CreatedBy = source.CreatedBy;
            CreatedDate = source.CreatedDate;
            ModifiedBy = source.ModifiedBy;
            ModifiedDate = source.ModifiedDate;
            StoreId = source.StoreId;
            Filename = source.Filename;
            Items = new ObservableCollection<SitemapItem>(source.Items.Select(i => AbstractTypeFactory<SitemapItem>.TryCreateInstance().FromDataModel(i)));

            return this;
        }

        public virtual SitemapEntity ToDataModel()
        {
            var result = new SitemapEntity();

            result.CreatedBy = CreatedBy;
            result.CreatedDate = CreatedDate;
            result.Filename = Filename;
            result.Id = Id;
            result.Items = new ObservableCollection<SitemapItemEntity>(Items.Select(i => i.ToDataModel()));
            result.ModifiedBy = ModifiedBy;
            result.ModifiedDate = ModifiedDate;
            result.StoreId = StoreId;

            return result;
        }
    }
}
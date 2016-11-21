using System;
using VirtoCommerce.SitemapsModule.Data.Model;

namespace VirtoCommerce.SitemapsModule.Web.Model
{
    public class SitemapItem
    {
        public string Id { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string Title { get; set; }

        public string ImageUrl { get; set; }

        public string AbsoluteUrl { get; set; }

        public string ObjectId { get; set; }

        public string ObjectType { get; set; }

        public string SitemapId { get; set; }

        public virtual SitemapItem FromDataModel(SitemapItemEntity source)
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
            Title = source.Title;
            ImageUrl = source.ImageUrl;
            AbsoluteUrl = source.AbsoluteUrl;
            ObjectId = source.ObjectId;
            ObjectType = source.ObjectType;
            SitemapId = source.SitemapId;

            return this;
        }

        public virtual SitemapItemEntity ToDataModel()
        {
            var result = new SitemapItemEntity();

            result.CreatedBy = CreatedBy;
            result.CreatedDate = CreatedDate;
            result.AbsoluteUrl = AbsoluteUrl;
            result.Id = Id;
            result.ImageUrl = ImageUrl;
            result.ModifiedBy = ModifiedBy;
            result.ModifiedDate = ModifiedDate;
            result.ObjectId = ObjectId;
            result.ObjectType = ObjectType;
            result.Title = Title;

            return result;
        }
    }
}
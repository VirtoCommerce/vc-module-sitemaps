﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
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

        [StringLength(256)]
        public string UrlTemplate { get; set; }

        public virtual ObservableCollection<SitemapItemEntity> Items { get; set; }

        public virtual Sitemap ToModel(Sitemap sitemap)
        {
            if (sitemap == null)
            {
                throw new ArgumentNullException("sitemap");
            }

            sitemap.CreatedBy = CreatedBy;
            sitemap.CreatedDate = CreatedDate;
            sitemap.Filename = Filename;
            sitemap.Id = Id;
            sitemap.ModifiedBy = ModifiedBy;
            sitemap.ModifiedDate = ModifiedDate;
            sitemap.StoreId = StoreId;
            sitemap.UrlTemplate = UrlTemplate;

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

            Filename = sitemap.Filename;
            StoreId = sitemap.StoreId;
            UrlTemplate = sitemap.UrlTemplate;

            return this;
        }

        public virtual void Patch(SitemapEntity sitemapEntity)
        {
            if (sitemapEntity == null)
            {
                throw new ArgumentNullException("sitemapEntity");
            }

            sitemapEntity.Filename = Filename;
            sitemapEntity.StoreId = StoreId;
            sitemapEntity.UrlTemplate = UrlTemplate;
        }
    }
}
using System.Collections.Generic;

namespace VirtoCommerce.SitemapsModule.Core.Model
{
    public class SearchResponse<T>
    {
        public SearchResponse()
        {
            Items = new List<T>();
        }

        public ICollection<T> Items { get; set; }

        public int TotalCount { get; set; }
    }
}
using System.Collections.Generic;

namespace VirtoCommerce.SitemapsModule.Core.Models;

public class SitemapFile(string name, IList<SitemapItemRecord> records)
{
    public string Name { get; } = name;

    public IList<SitemapItemRecord> Records { get; } = records;
}

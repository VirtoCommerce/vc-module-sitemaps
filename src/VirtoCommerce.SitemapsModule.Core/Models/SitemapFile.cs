using System.Collections.Generic;

namespace VirtoCommerce.SitemapsModule.Core.Models;

public class SitemapFile(string name, IList<SitemapRecord> records)
{
    public string Name { get; } = name;

    public IList<SitemapRecord> Records { get; } = records;
}

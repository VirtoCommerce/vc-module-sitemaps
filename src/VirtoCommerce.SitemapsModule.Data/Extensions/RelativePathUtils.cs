using System;

namespace VirtoCommerce.SitemapsModule.Data.Extensions;

public static class RelativePathUtils
{
    public static string Combine(string parentRelativePath, string childRelativePath)
    {
        ArgumentNullException.ThrowIfNull(parentRelativePath, nameof(parentRelativePath));

        if (string.IsNullOrEmpty(childRelativePath))
        {
            return parentRelativePath;
        }

        return $"{parentRelativePath.Trim('/')}/{childRelativePath.Trim('/')}";
    }
}

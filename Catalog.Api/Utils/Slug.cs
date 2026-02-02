using System.Text;
using System.Text.RegularExpressions;

namespace Catalog.Api.Utils;

public static class Slug
{
    public static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        var s = input.Trim().ToLowerInvariant();

        s = Regex.Replace(s, @"[^a-z0-9\s-]", "");
        s = Regex.Replace(s, @"\s+", " ").Trim();
        s = s.Replace(" ", "-");
        s = Regex.Replace(s, "-{2,}", "-").Trim('-');

        return s;
    }
}
using System.Text;

namespace LlamaShears.Core.Common;

public sealed class UriMerger : IUriMerger
{
    public Uri Merge(Uri rootUri, Uri otherUri)
    {
        ArgumentNullException.ThrowIfNull(rootUri);
        ArgumentNullException.ThrowIfNull(otherUri);

        var tailPath = otherUri.AbsolutePath;
        var otherQuery = otherUri.Query;

        if (tailPath == "/" && otherQuery.Length == 0)
        {
            return rootUri;
        }

        var builder = new UriBuilder(rootUri);

        if (tailPath != "/")
        {
            builder.Path = builder.Path.TrimEnd('/') + tailPath;
        }

        builder.Query = MergeQueryRootWins(
            otherQuery: otherQuery.TrimStart('?'),
            rootQuery: builder.Query.TrimStart('?'));
        return builder.Uri;
    }

    private static string MergeQueryRootWins(string otherQuery, string rootQuery)
    {
        if (rootQuery.Length == 0)
        {
            return otherQuery;
        }
        if (otherQuery.Length == 0)
        {
            return rootQuery;
        }

        var rootKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in rootQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equalsIndex = pair.IndexOf('=');
            rootKeys.Add(equalsIndex < 0 ? pair : pair[..equalsIndex]);
        }

        var builder = new StringBuilder();
        foreach (var pair in otherQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equalsIndex = pair.IndexOf('=');
            var key = equalsIndex < 0 ? pair : pair[..equalsIndex];
            if (rootKeys.Contains(key))
            {
                continue;
            }
            if (builder.Length > 0)
            {
                builder.Append('&');
            }
            builder.Append(pair);
        }
        if (builder.Length > 0)
        {
            builder.Append('&');
        }
        builder.Append(rootQuery);
        return builder.ToString();
    }
}

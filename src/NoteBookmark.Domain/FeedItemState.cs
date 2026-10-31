using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Data.Tables;

namespace NoteBookmark.Domain;

/// <summary>
/// What the user did with a feed item. Items are tracked by URL; once Added or Skipped
/// they leave the Feeds list (inbox model).
/// </summary>
public class FeedItemState : ITableEntity
{
    public const string Partition = "feeditemstate";
    public const string Added = "Added";
    public const string Skipped = "Skipped";

    [DataMember(Name = "url")]
    public string Url { get; set; } = string.Empty;

    /// <summary><see cref="Added"/> or <see cref="Skipped"/>.</summary>
    [DataMember(Name = "state")]
    public string State { get; set; } = string.Empty;

    [DataMember(Name = "feed_id")]
    public string? FeedId { get; set; }

    [DataMember(Name = "date_modified")]
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    public string PartitionKey { get; set; } = Partition;

    /// <summary>Hash of the normalized URL, see <see cref="KeyForUrl"/>.</summary>
    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public static bool IsValidState(string? state) => state is Added or Skipped;

    /// <summary>
    /// Normalizes a URL so trivially different spellings of the same article match:
    /// trims whitespace, drops the fragment, lowercases scheme and host, removes a trailing slash.
    /// </summary>
    public static string NormalizeUrl(string url)
    {
        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return trimmed;
        }

        var builder = new UriBuilder(uri) { Fragment = string.Empty };
        builder.Scheme = builder.Scheme.ToLowerInvariant();
        builder.Host = builder.Host.ToLowerInvariant();
        if (builder.Path.Length > 1 && builder.Path.EndsWith('/'))
        {
            builder.Path = builder.Path.TrimEnd('/');
        }

        return builder.Uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Fragment, UriFormat.UriEscaped);
    }

    /// <summary>
    /// Table Storage keys can't contain '/', '\', '#' or '?', so the RowKey is a SHA-256 hash
    /// (lowercase hex) of the normalized URL.
    /// </summary>
    public static string KeyForUrl(string url)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeUrl(url)));
        return Convert.ToHexStringLower(bytes);
    }
}

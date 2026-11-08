namespace NoteBookmark.Api.Tests.Feeds;

/// <summary>Small feed documents used across feed tests. Dates are relative to now so age filters stay valid.</summary>
public static class FeedSamples
{
    public static string Rfc822(DateTimeOffset date) => date.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", System.Globalization.CultureInfo.InvariantCulture);

    public static string Iso(DateTimeOffset date) => date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

    public static string Rss(params (string Url, string Title, DateTimeOffset Date)[] items) => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:media="http://search.yahoo.com/mrss/">
          <channel>
            <title>Sample RSS</title>
            <link>https://rss.example.com/</link>
            {string.Concat(items.Select(i => $"""
            <item>
              <title>{i.Title}</title>
              <link>{i.Url}</link>
              <pubDate>{Rfc822(i.Date)}</pubDate>
              <description>Summary of {i.Title}</description>
            </item>
            """))}
          </channel>
        </rss>
        """;

    public static string Atom(params (string Url, string Title, DateTimeOffset Date)[] entries) => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <feed xmlns="http://www.w3.org/2005/Atom">
          <title>Sample Atom</title>
          <link href="https://atom.example.com/" rel="alternate"/>
          <link href="https://atom.example.com/feed.xml" rel="self"/>
          {string.Concat(entries.Select(e => $"""
          <entry>
            <title>{e.Title}</title>
            <link href="{e.Url}"/>
            <published>{Iso(e.Date)}</published>
            <summary>Summary of {e.Title}</summary>
          </entry>
          """))}
        </feed>
        """;
}

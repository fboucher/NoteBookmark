using System;

namespace NoteBookmark.Domain;

/// <summary>
/// Notes Categories
/// Legacy code from the original ReadingNotres project.
/// </summary>
public static class NoteCategories
{
    /// <summary>
    /// Get a dictionary to change the short version by the long version of category name.
    /// </summary>
    public static string GetCategory(string? category)
    {
        category = category?.ToLower();
        var categories = new Dictionary<string, string>
                                                    {
                                                        {"ai", "AI"},
                                                        {"cloud", "Cloud"},
                                                        {"data", "Data"},
                                                        {"database", "Databases"},
                                                        {"dev", "Programming"},
                                                        {"devops", "DevOps"},
                                                        {"lowcode", "LowCode"},
                                                        {"misc", "Miscellaneous"},
                                                        {"top", "Suggestion of the week"},
                                                        {"oss", "Open Source"},
                                                        {"del", "del"}
                                                    };
        if (!String.IsNullOrEmpty(category) && categories.ContainsKey(category))
        {
            return categories[category];
        }
        else
        {
            return categories["misc"];
        }
    }

    public static List<string> GetCategories()
    {
        return new List<string>
        {
            "AI",
            "Cloud",
            "Data",
            "Databases",
            "DevOps",
            "LowCode",
            "Miscellaneous",
            "Programming",
            "Open Source",
            "Suggestion of the week",
            "del"
        };
    }
}

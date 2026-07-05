using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
#if !NOT_MAUI
using Microsoft.Maui.Storage;
#endif
using NoteBookmark.Domain;
using SQLite;

namespace NoteBookmark.MauiApp.Data;

public class LocalDataService : ILocalDataService
{
    private const string DatabaseFilename = "NoteBookmark.db3";

    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    private readonly string _databasePath;
    private SQLiteAsyncConnection? _database;

    public LocalDataService()
    {
#if !NOT_MAUI
        _databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
#else
        _databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseFilename);
#endif
    }

    public LocalDataService(string dbPath)
    {
        _databasePath = dbPath;
    }

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(_database))]
    private async Task InitAsync()
    {
        if (_database is not null)
            return;

        _database = new SQLiteAsyncConnection(_databasePath, Flags);
        await _database.CreateTableAsync<LocalPost>();
        await _database.CreateTableAsync<LocalNote>();
        await _database.CreateTableAsync<LocalSummary>();
        await _database.CreateTableAsync<LocalSettings>();
    }

    public async Task<List<Post>> GetPostsAsync()
    {
        await InitAsync();
        var localPosts = await _database.Table<LocalPost>()
            .Where(p => !p.IsDeleted)
            .ToListAsync();
        return localPosts.Select(p => p.ToDomain()).ToList();
    }

    public async Task<Post?> GetPostAsync(string id)
    {
        await InitAsync();
        var localPost = await _database.Table<LocalPost>()
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync();
        return localPost?.ToDomain();
    }

    public async Task SavePostAsync(Post post, bool isPendingSync = false)
    {
        await InitAsync();
        var localPost = LocalPost.FromDomain(post, isPendingSync);
        var existing = await _database.Table<LocalPost>().Where(p => p.Id == localPost.Id).FirstOrDefaultAsync();
        
        if (existing is not null)
        {
            localPost.IsPendingSync = isPendingSync || existing.IsPendingSync; // Preserve flag unless we are intentionally overwriting it
            await _database.UpdateAsync(localPost);
        }
        else
        {
            await _database.InsertAsync(localPost);
        }
    }

    public async Task SavePostsAsync(IEnumerable<Post> posts)
    {
        await InitAsync();
        var localPosts = posts.Select(p => LocalPost.FromDomain(p)).ToList();
        
        // Use RunInTransactionAsync for bulk operations
        await _database.RunInTransactionAsync(conn =>
        {
            foreach (var localPost in localPosts)
            {
                var existing = conn.Table<LocalPost>().Where(p => p.Id == localPost.Id).FirstOrDefault();
                if (existing is not null)
                {
                    localPost.IsPendingSync = existing.IsPendingSync;
                    conn.Update(localPost);
                }
                else
                {
                    conn.Insert(localPost);
                }
            }
        });
    }

    public async Task<List<Note>> GetNotesAsync()
    {
        await InitAsync();
        var localNotes = await _database.Table<LocalNote>()
            .Where(n => !n.IsDeleted)
            .ToListAsync();
        return localNotes.Select(n => n.ToDomain()).ToList();
    }

    public async Task<Note?> GetNoteAsync(string rowKey)
    {
        await InitAsync();
        var localNote = await _database.Table<LocalNote>()
            .Where(n => n.RowKey == rowKey && !n.IsDeleted)
            .FirstOrDefaultAsync();
        return localNote?.ToDomain();
    }

    public async Task SaveNoteAsync(Note note, bool isPendingSync = false)
    {
        await InitAsync();
        var localNote = LocalNote.FromDomain(note, isPendingSync);
        var existing = await _database.Table<LocalNote>().Where(n => n.RowKey == localNote.RowKey).FirstOrDefaultAsync();
        
        if (existing is not null)
        {
            localNote.IsPendingSync = isPendingSync || existing.IsPendingSync;
            await _database.UpdateAsync(localNote);
        }
        else
        {
            await _database.InsertAsync(localNote);
        }
    }

    public async Task DeleteNoteAsync(string rowKey, bool isPendingSync = false)
    {
        await InitAsync();
        var existing = await _database.Table<LocalNote>().Where(n => n.RowKey == rowKey).FirstOrDefaultAsync();
        if (existing is not null)
        {
            existing.IsDeleted = true;
            existing.IsPendingSync = isPendingSync || existing.IsPendingSync;
            existing.DateModified = DateTime.UtcNow;
            await _database.UpdateAsync(existing);
        }
    }

    public async Task DeletePostAsync(string id, bool isPendingSync = false)
    {
        await InitAsync();
        var existing = await _database.Table<LocalPost>().Where(p => p.Id == id).FirstOrDefaultAsync();
        if (existing is not null)
        {
            existing.IsDeleted = true;
            existing.IsPendingSync = isPendingSync || existing.IsPendingSync;
            existing.DateModified = DateTime.UtcNow;
            await _database.UpdateAsync(existing);
        }
    }

    public async Task<List<Summary>> GetSummariesAsync()
    {
        await InitAsync();
        var localSummaries = await _database.Table<LocalSummary>()
            .Where(s => !s.IsDeleted)
            .ToListAsync();
        return localSummaries.Select(s => s.ToDomain()).ToList();
    }

    public async Task SaveSummariesAsync(IEnumerable<Summary> summaries)
    {
        await InitAsync();
        var localSummaries = summaries.Select(s => LocalSummary.FromDomain(s)).ToList();
        
        await _database.RunInTransactionAsync(conn =>
        {
            foreach (var localSummary in localSummaries)
            {
                var existing = conn.Table<LocalSummary>().Where(s => s.RowKey == localSummary.RowKey).FirstOrDefault();
                if (existing is not null)
                {
                    localSummary.IsPendingSync = existing.IsPendingSync;
                    conn.Update(localSummary);
                }
                else
                {
                    conn.Insert(localSummary);
                }
            }
        });
    }

    public async Task<Settings?> GetSettingsAsync()
    {
        await InitAsync();
        var localSettings = await _database.Table<LocalSettings>().FirstOrDefaultAsync();
        return localSettings?.ToDomain();
    }

    public async Task SaveSettingsAsync(Settings settings)
    {
        await InitAsync();
        var localSettings = LocalSettings.FromDomain(settings);
        var existing = await _database.Table<LocalSettings>().Where(s => s.RowKey == localSettings.RowKey).FirstOrDefaultAsync();
        
        if (existing is not null)
        {
            localSettings.IsPendingSync = existing.IsPendingSync;
            await _database.UpdateAsync(localSettings);
        }
        else
        {
            await _database.InsertAsync(localSettings);
        }
    }

    public async Task<List<Post>> GetPendingSyncPostsAsync()
    {
        await InitAsync();
        var pending = await _database.Table<LocalPost>()
            .Where(p => p.IsPendingSync)
            .ToListAsync();
        return pending.Select(p => p.ToDomain()).ToList();
    }

    public async Task<List<Note>> GetPendingSyncNotesAsync()
    {
        await InitAsync();
        var pending = await _database.Table<LocalNote>()
            .Where(n => n.IsPendingSync)
            .ToListAsync();
        return pending.Select(n => n.ToDomain()).ToList();
    }

    public async Task MarkSyncedAsync(string id, bool isPost)
    {
        await InitAsync();
        if (isPost)
        {
            var existing = await _database.Table<LocalPost>().Where(p => p.Id == id).FirstOrDefaultAsync();
            if (existing is not null)
            {
                if (existing.IsDeleted)
                {
                    await _database.DeleteAsync(existing);
                }
                else
                {
                    existing.IsPendingSync = false;
                    await _database.UpdateAsync(existing);
                }
            }
        }
        else
        {
            var existing = await _database.Table<LocalNote>().Where(n => n.RowKey == id).FirstOrDefaultAsync();
            if (existing is not null)
            {
                if (existing.IsDeleted)
                {
                    await _database.DeleteAsync(existing);
                }
                else
                {
                    existing.IsPendingSync = false;
                    await _database.UpdateAsync(existing);
                }
            }
        }
    }
}

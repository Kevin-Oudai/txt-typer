using System.IO;
using System.Linq;
using System.Text.Json;
using TxtTyper.Models;
using TxtTyper.Services.Interfaces;

namespace TxtTyper.Services;

public sealed class SnippetStorageService : ISnippetStorageService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public string StoragePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "txt-typer",
        "snippets.json");

    public async Task<IReadOnlyList<Snippet>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(StoragePath))
        {
            return Array.Empty<Snippet>();
        }

        await using var stream = File.OpenRead(StoragePath);
        var snippets = await JsonSerializer.DeserializeAsync<List<Snippet>>(
                           stream,
                           SerializerOptions,
                           cancellationToken)
                       ?? [];

        return snippets
            .Where(snippet => !string.IsNullOrWhiteSpace(snippet.Name))
            .OrderBy(snippet => snippet.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task SaveAsync(IEnumerable<Snippet> snippets, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(StoragePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var normalizedSnippets = snippets
            .Where(snippet => !string.IsNullOrWhiteSpace(snippet.Name))
            .Select(snippet => new Snippet
            {
                Name = snippet.Name.Trim(),
                Content = snippet.Content ?? string.Empty
            })
            .OrderBy(snippet => snippet.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await using var stream = File.Create(StoragePath);
        await JsonSerializer.SerializeAsync(
            stream,
            normalizedSnippets,
            SerializerOptions,
            cancellationToken);
    }
}

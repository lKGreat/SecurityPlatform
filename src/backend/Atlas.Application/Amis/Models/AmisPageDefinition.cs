using System.Text.Json;

namespace Atlas.Application.Amis.Models;

public sealed record AmisPageDefinition(
    string Key,
    string Title,
    string TableKey,
    string? Description,
    JsonElement Schema);

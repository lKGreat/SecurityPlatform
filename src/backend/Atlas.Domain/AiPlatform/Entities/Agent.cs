using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.AiPlatform.Entities;

public sealed class Agent : TenantEntity
{
    public Agent()
        : base(TenantId.Empty)
    {
        Name = string.Empty;
        Description = string.Empty;
        AvatarUrl = string.Empty;
        SystemPrompt = string.Empty;
        ModelName = string.Empty;
        UpdatedAt = DateTime.UnixEpoch;
        PublishedAt = DateTime.UnixEpoch;
        Status = AgentStatus.Draft;
    }

    public Agent(
        TenantId tenantId,
        string name,
        long creatorId,
        long id)
        : base(tenantId)
    {
        Id = id;
        Name = name;
        CreatorId = creatorId;
        Status = AgentStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        PublishedAt = DateTime.UnixEpoch;
    }

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? SystemPrompt { get; private set; }
    public long? ModelConfigId { get; private set; }
    public string? ModelName { get; private set; }
    public float? Temperature { get; private set; }
    public int? MaxTokens { get; private set; }
    public AgentStatus Status { get; private set; }
    public long CreatorId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public int PublishVersion { get; private set; }

    public void Update(
        string name,
        string? description,
        string? avatarUrl,
        string? systemPrompt,
        long? modelConfigId,
        string? modelName,
        float? temperature,
        int? maxTokens)
    {
        Name = name;
        Description = description ?? string.Empty;
        AvatarUrl = avatarUrl ?? string.Empty;
        SystemPrompt = systemPrompt ?? string.Empty;
        ModelConfigId = modelConfigId;
        if (!ModelConfigId.HasValue)
        {
            ModelConfigId = 0;
        }
        ModelName = modelName ?? string.Empty;
        Temperature = temperature;
        if (!Temperature.HasValue)
        {
            Temperature = 0;
        }

        MaxTokens = maxTokens;
        if (!MaxTokens.HasValue)
        {
            MaxTokens = 0;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        Status = AgentStatus.Published;
        PublishVersion++;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        Status = AgentStatus.Disabled;
        UpdatedAt = DateTime.UtcNow;
        if (!PublishedAt.HasValue)
        {
            PublishedAt = DateTime.UnixEpoch;
        }
    }

    public Agent CreateDuplicate(long newId, string newName, long creatorId)
    {
        var duplicate = new Agent(TenantId, newName, creatorId, newId);
        duplicate.Update(
            newName,
            Description,
            AvatarUrl,
            SystemPrompt,
            ModelConfigId,
            ModelName,
            Temperature,
            MaxTokens);
        return duplicate;
    }
}

public enum AgentStatus
{
    Draft = 0,
    Published = 1,
    Disabled = 2
}

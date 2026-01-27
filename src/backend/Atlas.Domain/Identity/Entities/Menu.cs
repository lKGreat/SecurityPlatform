using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Identity.Entities;

public class Menu : TenantEntity
{
    public Menu()
        : base(TenantId.Empty)
    {
        Name = string.Empty;
        Path = string.Empty;
        Component = null;
        Icon = null;
        ParentId = null;
        SortOrder = 0;
        PermissionCode = null;
        IsHidden = false;
    }

    public Menu(
        TenantId tenantId,
        string name,
        string path,
        long id,
        long? parentId,
        int sortOrder,
        string? component,
        string? icon,
        string? permissionCode,
        bool isHidden)
        : base(tenantId)
    {
        Id = id;
        Name = name;
        Path = path;
        ParentId = parentId;
        SortOrder = sortOrder;
        Component = component;
        Icon = icon;
        PermissionCode = permissionCode;
        IsHidden = isHidden;
    }

    public string Name { get; private set; }
    public string Path { get; private set; }
    public string? Component { get; private set; }
    public string? Icon { get; private set; }
    public long? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public string? PermissionCode { get; private set; }
    public bool IsHidden { get; private set; }

    public void Update(
        string name,
        string path,
        long? parentId,
        int sortOrder,
        string? component,
        string? icon,
        string? permissionCode,
        bool isHidden)
    {
        Name = name;
        Path = path;
        ParentId = parentId;
        SortOrder = sortOrder;
        Component = component;
        Icon = icon;
        PermissionCode = permissionCode;
        IsHidden = isHidden;
    }
}

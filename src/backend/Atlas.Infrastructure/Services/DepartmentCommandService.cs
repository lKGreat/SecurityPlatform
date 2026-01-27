using Atlas.Application.Identity.Abstractions;
using Atlas.Application.Identity.Models;
using Atlas.Application.Identity.Repositories;
using Atlas.Core.Exceptions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;

namespace Atlas.Infrastructure.Services;

public sealed class DepartmentCommandService : IDepartmentCommandService
{
    private readonly IDepartmentRepository _departmentRepository;

    public DepartmentCommandService(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<long> CreateAsync(
        TenantId tenantId,
        DepartmentCreateRequest request,
        long id,
        CancellationToken cancellationToken)
    {
        var department = new Department(tenantId, request.Name, id, request.ParentId, request.SortOrder);
        await _departmentRepository.AddAsync(department, cancellationToken);
        return department.Id;
    }

    public async Task UpdateAsync(
        TenantId tenantId,
        long departmentId,
        DepartmentUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.FindByIdAsync(tenantId, departmentId, cancellationToken);
        if (department is null)
        {
            throw new BusinessException("Department not found.", ErrorCodes.NotFound);
        }

        department.Update(request.Name, request.ParentId, request.SortOrder);
        await _departmentRepository.UpdateAsync(department, cancellationToken);
    }
}

using AutoMapper;
using Atlas.Application.Identity.Abstractions;
using Atlas.Application.Identity.Models;
using Atlas.Application.Identity.Repositories;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Infrastructure.Services;

public sealed class DepartmentQueryService : IDepartmentQueryService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IMapper _mapper;

    public DepartmentQueryService(IDepartmentRepository departmentRepository, IMapper mapper)
    {
        _departmentRepository = departmentRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<DepartmentListItem>> QueryDepartmentsAsync(
        PagedRequest request,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var (items, total) = await _departmentRepository.QueryPageAsync(
            pageIndex,
            pageSize,
            request.Keyword,
            cancellationToken);

        var resultItems = items.Select(x => _mapper.Map<DepartmentListItem>(x)).ToArray();
        return new PagedResult<DepartmentListItem>(resultItems, total, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<DepartmentListItem>> QueryAllAsync(
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        var items = await _departmentRepository.QueryAllAsync(tenantId, cancellationToken);
        return items.Select(x => _mapper.Map<DepartmentListItem>(x)).ToArray();
    }
}

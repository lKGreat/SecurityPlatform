using AutoMapper;
using Atlas.Application.Approval.Abstractions;
using Atlas.Application.Approval.Models;
using Atlas.Application.Approval.Repositories;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Enums;

namespace Atlas.Infrastructure.Services;

/// <summary>
/// 审批流定义查询服务实现
/// </summary>
public sealed class ApprovalFlowQueryService : IApprovalFlowQueryService
{
    private readonly IApprovalFlowRepository _flowRepository;
    private readonly IMapper _mapper;

    public ApprovalFlowQueryService(IApprovalFlowRepository flowRepository, IMapper mapper)
    {
        _flowRepository = flowRepository;
        _mapper = mapper;
    }

    public async Task<ApprovalFlowDefinitionResponse?> GetByIdAsync(
        TenantId tenantId,
        long id,
        CancellationToken cancellationToken)
    {
        var entity = await _flowRepository.GetByIdAsync(tenantId, id, cancellationToken);
        return entity != null ? _mapper.Map<ApprovalFlowDefinitionResponse>(entity) : null;
    }

    public async Task<PagedResult<ApprovalFlowDefinitionListItem>> GetPagedAsync(
        TenantId tenantId,
        PagedRequest request,
        ApprovalFlowStatus? status = null,
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _flowRepository.GetPagedAsync(
            tenantId,
            request.PageIndex,
            request.PageSize,
            status,
            keyword,
            cancellationToken);

        return new PagedResult<ApprovalFlowDefinitionListItem>(
            _mapper.Map<List<ApprovalFlowDefinitionListItem>>(items),
            totalCount,
            request.PageIndex,
            request.PageSize);
    }
}

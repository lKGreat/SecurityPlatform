using AutoMapper;
using Atlas.Application.Approval.Models;
using Atlas.Core.Abstractions;
using Atlas.Domain.Approval.Entities;

namespace Atlas.Application.Approval.Mappings;

/// <summary>
/// 审批流 AutoMapper 映射配置
/// </summary>
public sealed class ApprovalMappingProfile : Profile
{
    public ApprovalMappingProfile()
    {
        // ApprovalFlowDefinition 映射
        CreateMap<ApprovalFlowDefinitionCreateRequest, ApprovalFlowDefinition>()
            .ConstructUsing((src, ctx) =>
            {
                var tenantId = (Atlas.Core.Tenancy.TenantId)ctx.Items["TenantId"];
                var idGenerator = (IIdGenerator)ctx.Items["IdGenerator"];
                return new ApprovalFlowDefinition(tenantId, src.Name, src.DefinitionJson, idGenerator.NextId());
            });

        CreateMap<ApprovalFlowDefinition, ApprovalFlowDefinitionResponse>();

        CreateMap<ApprovalFlowDefinition, ApprovalFlowDefinitionListItem>();

        // ApprovalProcessInstance 映射
        CreateMap<ApprovalStartRequest, ApprovalProcessInstance>()
            .ConstructUsing((src, ctx) =>
            {
                var tenantId = (Atlas.Core.Tenancy.TenantId)ctx.Items["TenantId"];
                var idGenerator = (IIdGenerator)ctx.Items["IdGenerator"];
                var initiatorUserId = (long)ctx.Items["UserId"];
                return new ApprovalProcessInstance(
                    tenantId,
                    src.DefinitionId,
                    src.BusinessKey,
                    initiatorUserId,
                    idGenerator.NextId(),
                    src.DataJson);
            });

        CreateMap<ApprovalProcessInstance, ApprovalInstanceResponse>();

        // ApprovalTask 映射
        CreateMap<ApprovalTask, ApprovalTaskResponse>();

        // ApprovalDepartmentLeader 映射
        CreateMap<ApprovalDepartmentLeaderRequest, ApprovalDepartmentLeader>()
            .ConstructUsing((src, ctx) =>
            {
                var tenantId = (Atlas.Core.Tenancy.TenantId)ctx.Items["TenantId"];
                var idGenerator = (IIdGenerator)ctx.Items["IdGenerator"];
                return new ApprovalDepartmentLeader(tenantId, src.DepartmentId, src.LeaderUserId, idGenerator.NextId());
            });
    }
}

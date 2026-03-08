import { requestApi } from "@/services/api-core";
import type { ApiResponse, PagedResult } from "@/types/api";

export interface ToolAuthorizationPolicyItem {
  id: string;
  toolId: string;
  toolName: string;
  policyType: string;
  rateLimitQuota: number;
  auditEnabled: boolean;
}

export async function getPlatformOverview() {
  const resp = await requestApi<ApiResponse<Record<string, unknown>>>("/platform/overview", {
    method: "GET",
  });
  return resp.data ?? {};
}

export async function getPlatformResources() {
  const resp = await requestApi<ApiResponse<Record<string, unknown>>>("/platform/resources", {
    method: "GET",
  });
  return resp.data ?? {};
}

export async function getPlatformReleases(pageIndex = 1, pageSize = 10) {
  const resp = await requestApi<ApiResponse<PagedResult<Record<string, unknown>>>>(
    `/platform/releases?pageIndex=${pageIndex}&pageSize=${pageSize}`,
    { method: "GET" }
  );
  return (
    resp.data ?? {
      items: [],
      total: 0,
      pageIndex,
      pageSize,
    }
  );
}

export async function getToolAuthorizationPolicies(pageIndex = 1, pageSize = 10): Promise<PagedResult<ToolAuthorizationPolicyItem>> {
  const resp = await requestApi<ApiResponse<PagedResult<ToolAuthorizationPolicyItem>>>(
    `/tools/authorization-policies?pageIndex=${pageIndex}&pageSize=${pageSize}`,
    {
      method: "GET",
    }
  );
  return (
    resp.data ?? {
      items: [],
      total: 0,
      pageIndex,
      pageSize,
    }
  );
}

import type { ApiResponse, PagedRequest, PagedResult } from "@/types/api";
import { message } from "ant-design-vue";

const API_BASE = import.meta.env.VITE_API_BASE ?? "/api";

interface TokenResult {
  accessToken: string;
  expiresAt: string;
}

export interface AssetListItem {
  id: string;
  name: string;
}

export interface AuditListItem {
  id: string;
  action: string;
  occurredAt: string;
}

export interface AlertListItem {
  id: string;
  title: string;
  createdAt: string;
}

export async function createToken(tenantId: string, username: string, password: string) {
  const response = await requestApi<ApiResponse<TokenResult>>("/auth/token", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-Tenant-Id": tenantId
    },
    body: JSON.stringify({ username, password })
  });

  if (!response.data) {
    throw new Error(response.message || "登录失败");
  }

  return response.data;
}

export async function getAssetsPaged(pagedRequest: PagedRequest) {
  const query = toQuery(pagedRequest);
  const response = await requestApi<ApiResponse<PagedResult<AssetListItem>>>(`/assets?${query}`);
  if (!response.data) {
    throw new Error(response.message || "查询失败");
  }

  return response.data;
}

export async function getAuditsPaged(pagedRequest: PagedRequest) {
  const query = toQuery(pagedRequest);
  const response = await requestApi<ApiResponse<PagedResult<AuditListItem>>>(`/audit?${query}`);
  if (!response.data) {
    throw new Error(response.message || "查询失败");
  }

  return response.data;
}

export async function getAlertsPaged(pagedRequest: PagedRequest) {
  const query = toQuery(pagedRequest);
  const response = await requestApi<ApiResponse<PagedResult<AlertListItem>>>(`/alert?${query}`);
  if (!response.data) {
    throw new Error(response.message || "查询失败");
  }

  return response.data;
}

function toQuery(pagedRequest: PagedRequest) {
  const query = new URLSearchParams({
    pageIndex: pagedRequest.pageIndex.toString(),
    pageSize: pagedRequest.pageSize.toString(),
    keyword: pagedRequest.keyword ?? "",
    sortBy: pagedRequest.sortBy ?? "",
    sortDesc: pagedRequest.sortDesc ? "true" : "false"
  });

  return query.toString();
}

async function requestApi<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers ?? {});
  const token = localStorage.getItem("access_token");
  const tenantId = localStorage.getItem("tenant_id");

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  if (tenantId && !headers.has("X-Tenant-Id")) {
    headers.set("X-Tenant-Id", tenantId);
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers
  });

  if (!response.ok) {
    const text = await response.text();
    message.error(text || "网络请求失败");
    throw new Error(text || "网络请求失败");
  }

  return (await response.json()) as T;
}

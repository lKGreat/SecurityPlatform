// 授权证书模块 API
import type {
  ApiResponse,
  LicenseStatus,
  LicenseActivateResult,
  LicenseFingerprintResult,
} from "@/types/api";
import { requestApi } from "@/services/api-core";

interface ApiRequestErrorLike extends Error {
  payload?: {
    code?: string;
    message?: string;
    traceId?: string;
  } | null;
}

const TENANT_ID_REGEX =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

function resolveTenantHeader(tenantId?: string): Record<string, string> | undefined {
  const normalized = tenantId?.trim();
  if (!normalized || !TENANT_ID_REGEX.test(normalized)) {
    return undefined;
  }
  return { "X-Tenant-Id": normalized };
}

/** 获取当前授权状态 */
export async function getLicenseStatus(tenantId?: string): Promise<LicenseStatus> {
  const resp = await requestApi<ApiResponse<LicenseStatus>>(
    "/license/status",
    {
      method: "GET",
      headers: resolveTenantHeader(tenantId),
    },
    { disableAutoRefresh: true, suppressErrorMessage: true }
  );
  return (
    resp.data ?? {
      status: "None" as const,
      edition: "Trial" as const,
      isPermanent: false,
      issuedAt: null,
      expiresAt: null,
      remainingDays: null,
      machineBound: false,
      machineMatched: false,
      features: {},
      limits: {},
    }
  );
}

/** 获取当前机器码（离线授权时提供给颁发方） */
export async function getMachineFingerprint(tenantId?: string): Promise<string> {
  const resp = await requestApi<ApiResponse<LicenseFingerprintResult>>(
    "/license/fingerprint",
    {
      method: "GET",
      headers: resolveTenantHeader(tenantId),
    },
    { disableAutoRefresh: true, suppressErrorMessage: true }
  );
  return resp.data?.fingerprint ?? "";
}

/** 上传授权证书激活（支持登录前调用） */
export async function activateLicense(
  licenseContent: string,
  tenantId?: string
): Promise<ApiResponse<LicenseActivateResult>> {
  try {
    return await requestApi<ApiResponse<LicenseActivateResult>>(
      "/license/activate",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...resolveTenantHeader(tenantId),
        },
        body: JSON.stringify({ licenseContent }),
      },
      {
        disableAutoRefresh: true,
        suppressErrorMessage: true,
      }
    );
  } catch (error) {
    const requestError = error as ApiRequestErrorLike;
    const payload = requestError?.payload ?? null;
    return {
      success: false,
      code: payload?.code ?? "LICENSE_ACTIVATE_FAILED",
      message: payload?.message ?? requestError?.message ?? "证书激活失败",
      traceId: payload?.traceId ?? "",
    };
  }
}

// 授权证书模块 API
import type {
  ApiResponse,
  LicenseStatus,
  LicenseActivateResult,
  LicenseFingerprintResult,
} from "@/types/api";
import { requestApi } from "@/services/api-core";

/** 获取当前授权状态 */
export async function getLicenseStatus(): Promise<LicenseStatus> {
  const resp = await requestApi<ApiResponse<LicenseStatus>>(
    "/license/status",
    { method: "GET" },
    { disableAutoRefresh: true }
  );
  return (
    resp.data ?? {
      status: "None" as const,
      edition: "Trial" as const,
      isPermanent: false,
      issuedAt: null,
      expiresAt: null,
      remainingDays: null,
      machineMatched: false,
      features: {},
      limits: {},
    }
  );
}

/** 获取当前机器码（离线授权时提供给颁发方） */
export async function getMachineFingerprint(): Promise<string> {
  const resp = await requestApi<ApiResponse<LicenseFingerprintResult>>(
    "/license/fingerprint",
    { method: "GET" },
    { disableAutoRefresh: true }
  );
  return resp.data?.fingerprint ?? "";
}

/** 上传授权证书激活（支持登录前调用） */
export async function activateLicense(
  licenseContent: string
): Promise<ApiResponse<LicenseActivateResult>> {
  return requestApi<ApiResponse<LicenseActivateResult>>(
    "/license/activate",
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ licenseContent }),
    },
    { disableAutoRefresh: true }
  );
}

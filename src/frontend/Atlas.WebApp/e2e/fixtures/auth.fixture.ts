import { test as base, expect, type Page } from "@playwright/test";

type AuthFixture = {
  loginAsSuperAdmin: () => Promise<void>;
  loginAsSysAdmin: () => Promise<void>;
  loginAsSecurityAdmin: () => Promise<void>;
  loginAsApprovalAdmin: () => Promise<void>;
  loginAsDeptAdminA: () => Promise<void>;
  loginAsDeptAdminB: () => Promise<void>;
  loginAsReadonly: () => Promise<void>;
  loginAsUserA: () => Promise<void>;
  loginAsUserB: () => Promise<void>;

  // Keep old fixture for backward compatibility or when UI login is needed
  loginAsAdmin: () => Promise<void>;
};

const defaultTenantId = "00000000-0000-0000-0000-000000000001";
const e2eTenantId = process.env.E2E_TEST_TENANT_ID ?? defaultTenantId;
const e2ePassword = process.env.E2E_TEST_PASSWORD ?? "P@ssw0rd!"; // Default from AGENTS.md

// Generic API Login Helper
async function apiLogin(page: Page, username: string) {
  const loginResp = await page.request.post("/api/v1/auth/token", {
    headers: { "Content-Type": "application/json", "X-Tenant-Id": e2eTenantId },
    data: { username, password: e2ePassword },
  });
  
  expect(loginResp.ok(), `API Login failed for ${username}`).toBeTruthy();
  const respJson = await loginResp.json();
  const { accessToken, refreshToken } = respJson.data;

  // Visit the domain first to set storage
  await page.goto("/login");
  
  // Inject tokens
  await page.evaluate(
    ({ accessToken, refreshToken, tenantId }) => {
      sessionStorage.setItem("access_token", accessToken);
      localStorage.setItem("refresh_token", refreshToken);
      localStorage.setItem("tenant_id", tenantId);
    },
    { accessToken, refreshToken, tenantId: e2eTenantId }
  );

  // Navigate directly to console
  await page.goto("/console");
  await page.waitForURL(/\/console/, { timeout: 15000 });
}

export const test = base.extend<AuthFixture>({
  loginAsSuperAdmin: async ({ page }, use) => {
    await use(async () => await apiLogin(page, process.env.E2E_SUPERADMIN_USERNAME ?? "superadmin.e2e"));
  },
  loginAsSysAdmin: async ({ page }, use) => {
    await use(async () => await apiLogin(page, process.env.E2E_SYSADMIN_USERNAME ?? "sysadmin.e2e"));
  },
  loginAsSecurityAdmin: async ({ page }, use) => {
    await use(async () => await apiLogin(page, process.env.E2E_SECURITYADMIN_USERNAME ?? "securityadmin.e2e"));
  },
  loginAsApprovalAdmin: async ({ page }, use) => {
    await use(async () => await apiLogin(page, process.env.E2E_APPROVALADMIN_USERNAME ?? "approvaladmin.e2e"));
  },
  loginAsDeptAdminA: async ({ page }, use) => {
    await use(async () => await apiLogin(page, process.env.E2E_DEPTADMIN_A_USERNAME ?? "deptadmin.a.e2e"));
  },
  loginAsDeptAdminB: async ({ page }, use) => {
    await use(async () => await apiLogin(page, process.env.E2E_DEPTADMIN_B_USERNAME ?? "deptadmin.b.e2e"));
  },
  loginAsReadonly: async ({ page }, use) => {
    await use(async () => await apiLogin(page, process.env.E2E_READONLY_USERNAME ?? "readonly.e2e"));
  },
  loginAsUserA: async ({ page }, use) => {
    await use(async () => await apiLogin(page, process.env.E2E_USER_A_USERNAME ?? "user.a.e2e"));
  },
  loginAsUserB: async ({ page }, use) => {
    await use(async () => await apiLogin(page, process.env.E2E_USER_B_USERNAME ?? "user.b.e2e"));
  },
  
  // Legacy UI login for other old tests
  loginAsAdmin: async ({ page }, use) => {
    await use(async () => {
      if (!e2ePassword) {
        throw new Error("缺少 E2E_TEST_PASSWORD 环境变量，无法执行登录步骤。");
      }
      const e2eUsername = process.env.E2E_TEST_USERNAME ?? "admin";

      await page.goto("/login");
      await page.getByPlaceholder("请输入租户 / 组织 ID").fill(e2eTenantId);
      await page.getByPlaceholder("请输入手机号/邮箱/用户名").fill(e2eUsername);
      await page.getByPlaceholder("请输入密码").fill(e2ePassword);
      await page.getByRole("button", { name: "登录" }).click();
      await page.waitForURL(/\/console/, { timeout: 15000 });
    });
  }
});

export { expect };

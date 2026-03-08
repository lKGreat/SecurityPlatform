import { test as base, expect } from "@playwright/test";

type AuthFixture = {
  loginAsAdmin: () => Promise<void>;
};

const defaultTenantId = "00000000-0000-0000-0000-000000000001";
const e2eTenantId = process.env.E2E_TEST_TENANT_ID ?? defaultTenantId;
const e2eUsername = process.env.E2E_TEST_USERNAME ?? "admin";
const e2ePassword = process.env.E2E_TEST_PASSWORD;

export const test = base.extend<AuthFixture>({
  loginAsAdmin: async ({ page }, use) => {
    await use(async () => {
      if (!e2ePassword) {
        throw new Error("缺少 E2E_TEST_PASSWORD 环境变量，无法执行登录步骤。");
      }

      await page.goto("/login");
      await page.getByPlaceholder("请输入租户 / 组织 ID").fill(e2eTenantId);
      await page.getByPlaceholder("请输入手机号/邮箱/用户名").fill(e2eUsername);
      await page.getByPlaceholder("请输入密码").fill(e2ePassword);
      await page.getByRole("button", { name: "登录" }).click();
      await page.waitForURL(/\/(home|$)/, { timeout: 15000 });
    });
  }
});

export { expect };

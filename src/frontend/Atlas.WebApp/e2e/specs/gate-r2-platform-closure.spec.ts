import { expect, test } from "@playwright/test";
import path from "node:path";
import fs from "node:fs";
const runStamp = new Date().toISOString().replace(/[:.]/g, "-");
const evidenceDir = path.resolve(process.cwd(), `../../../docs/evidence/gate-r2-${runStamp}`);

function ensureEvidenceDir() {
  if (!fs.existsSync(evidenceDir)) {
    fs.mkdirSync(evidenceDir, { recursive: true });
  }
}

async function login(page: Parameters<typeof test>[0]["page"]) {
  const tenantId = "00000000-0000-0000-0000-000000000001";
  const loginResp = await page.request.post("/api/v1/auth/token", {
    headers: {
      "Content-Type": "application/json",
      "X-Tenant-Id": tenantId
    },
    data: {
      username: "admin",
      password: "P@ssw0rd!"
    }
  });
  expect(loginResp.ok()).toBeTruthy();
  const loginJson = await loginResp.json() as { data: { accessToken: string; refreshToken: string } };

  await page.goto("/login");
  await page.evaluate(({ accessToken, refreshToken, tid }) => {
    sessionStorage.setItem("access_token", accessToken);
    localStorage.setItem("refresh_token", refreshToken);
    localStorage.setItem("tenant_id", tid);
  }, {
    accessToken: loginJson.data.accessToken,
    refreshToken: loginJson.data.refreshToken,
    tid: tenantId
  });

  await page.goto("/console");
  await page.waitForURL(/\/console/, { timeout: 20_000 });
}

test("Gate-R2: 平台封板闭环 GUI 手测取证", async ({ page }) => {
  test.setTimeout(180_000);
  ensureEvidenceDir();
  console.info(`[gate-r2] evidenceDir=${evidenceDir}`);

  await login(page);

  await expect(page).toHaveURL(/\/console/);
  const shot01 = path.join(evidenceDir, "01-console-home.png");
  await page.screenshot({ path: shot01, fullPage: true });
  expect(fs.existsSync(shot01)).toBeTruthy();

  await page.goto("/console/apps");
  await expect(page.getByText("应用列表")).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDir, "02-console-apps.png"), fullPage: true });

  await page.goto("/console/resources");
  await expect(page).toHaveURL(/\/console\/resources/);
  await expect(page.getByText("应用列表")).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDir, "03-console-resources.png"), fullPage: true });

  await page.goto("/console/releases");
  await expect(page).toHaveURL(/\/console\/releases/);
  await expect(page.getByText("应用列表")).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDir, "04-console-releases.png"), fullPage: true });

  await page.goto("/console/tools");
  await expect(page.getByText("Tools Authorization Center")).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDir, "05-console-tools.png"), fullPage: true });

  await page.goto("/apps/1/dashboard");
  await expect(page.getByText("页面数量")).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDir, "06-app-dashboard.png"), fullPage: true });

  await page.goto("/apps/1/pages");
  await expect(page.locator(".ant-page-header-heading-title").filter({ hasText: "页面管理" })).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDir, "07-app-pages.png"), fullPage: true });

  await page.goto("/apps/1/settings");
  await expect(page.locator(".ant-page-header-heading-title").filter({ hasText: "应用设置" })).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDir, "08-app-settings.png"), fullPage: true });

  await page.goto("/r/crm_demo_2026/customer-form");
  await expect(page.getByText("返回控制台")).toBeVisible();
  await expect(page.getByRole("button", { name: /待\s*办/ })).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDir, "09-runtime-page.png"), fullPage: true });

  await page.goto("/settings/license");
  await expect(page.locator(".ant-page-header-heading-title").filter({ hasText: "授权管理" })).toBeVisible();
  await page.screenshot({ path: path.join(evidenceDir, "10-license-center.png"), fullPage: true });

  await page.goto("/console/apps");
  await page.getByRole("button", { name: "新建应用" }).click();
  await expect(page.locator(".ant-modal")).toBeVisible({ timeout: 10_000 });
  await page.screenshot({ path: path.join(evidenceDir, "11-app-create-modal.png"), fullPage: true });

  // 控制台-运行态跳转回显（校验 RuntimeLayout 导航）
  await page.goto("/r/crm_demo_2026/customer-form");
  await page.getByRole("button", { name: "返回控制台" }).click();
  await expect(page).toHaveURL(/\/console/, { timeout: 15_000 });
  await page.screenshot({ path: path.join(evidenceDir, "12-runtime-back-console.png"), fullPage: true });
});


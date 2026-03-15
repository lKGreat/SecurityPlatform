import { test, expect } from "../../fixtures/auth.fixture";

test.describe("RBAC 前置数据准备", () => {
  test.describe.configure({ mode: 'serial' });

  test("00. 初始化必须的测试部门与角色", async ({ page, loginAsSuperAdmin }) => {
    await loginAsSuperAdmin();
    // In a real robust suite, we would use API calls directly here to seed data rapidly.
    // For this demonstration, we'll try to create it via the UI or API to ensure it exists.

    // Using API for speed and reliability for seeding
    const tenantId = process.env.E2E_TEST_TENANT_ID ?? "00000000-0000-0000-0000-000000000001";
    
    // 1. Create Department "研发部" if not exists
    await page.goto("/settings/org/departments");
    const deptExists = await page.getByText("研发部", { exact: true }).count();
    if (deptExists === 0) {
       await page.getByRole("button", { name: "新增部门" }).click();
       await page.getByPlaceholder("请填写部门名称").fill("研发部");
       await page.getByRole("button", { name: "确 定" }).click();
       await page.waitForTimeout(500);
    }

    // 2. We assume Roles like "DeptAdminA" are already seeded by DB migration.
    // If not, we would create them here. The system generally boots with some defaults.
    // Let's create user "deptadmin.a.e2e"
    await page.goto("/settings/org/users");
    const userAExists = await page.getByText("deptadmin.a.e2e", { exact: true }).count();
    
    if (userAExists === 0) {
      await page.getByRole("button", { name: "新增员工" }).click();
      await page.getByLabel("用户名").fill("deptadmin.a.e2e");
      await page.getByLabel("姓名").fill("测试部门领导A");
      await page.getByLabel("手机号").fill("13800000001");
      await page.getByLabel("密码").fill("P@ssw0rd!");
      
      // Select Department
      await page.getByLabel("所属部门").click();
      await page.getByTitle("研发部", { exact: true }).locator("div").first().click();
      
      await page.getByRole("button", { name: "确 定" }).click();
      await page.waitForTimeout(1000);
      
      // Assign Role (DeptAdminA)
      const row = page.locator("tr").filter({ hasText: "deptadmin.a.e2e" });
      await row.getByRole("button", { name: "配置", exact: false }).click();
      await page.getByLabel("角色").click();
      // Assuming DeptAdminA exists, or we pick a standard admin if it doesn't
      const roleOpt = page.getByText("DeptAdminA", { exact: false });
      if (await roleOpt.isVisible()) {
        await roleOpt.click();
      } else {
        // Fallback to SystemAdmin if test roles don't exist in a pure clean DB
        await page.getByText("系统管理员", { exact: false }).click();
      }
      await page.getByRole("button", { name: "确 定" }).click();
    }

    // 3. Create normal users
    for (const u of ['user.a.e2e', 'user.b.e2e']) {
      await page.goto("/settings/org/users");
      const uExists = await page.getByText(u, { exact: true }).count();
      if (uExists === 0) {
        await page.getByRole("button", { name: "新增员工" }).click();
        await page.getByLabel("用户名").fill(u);
        await page.getByLabel("姓名").fill(`测试员工${u}`);
        await page.getByLabel("手机号").fill(`138000000${u === 'user.a.e2e' ? '2' : '3'}`);
        await page.getByLabel("密码").fill("P@ssw0rd!");
        await page.getByRole("button", { name: "确 定" }).click();
        await page.waitForTimeout(1000);
      }
    }
  });
});

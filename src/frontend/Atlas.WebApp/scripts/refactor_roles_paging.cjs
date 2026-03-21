const fs = require('fs');
let content = fs.readFileSync('src/components/system/roles/RoleAssignPanel.vue', 'utf8');

// 1. imports - handle \n and \r\n safely.
content = content.replace(
  /getPermissionsPaged,\s+getMenusPaged,\s+getDepartmentsPaged/g,
  `getPermissionsPaged,\n  getMenusAll,\n  getDepartmentsAll`
);

// 2. pageSize for permissions
content = content.replace(
  /getPermissionsPaged\(\{\s+pageIndex: 1,\s+pageSize: 1000,/g,
  `getPermissionsPaged({\n      pageIndex: 1,\n      pageSize: 200,`
);

// 3. getMenusAll
content = content.replace(
  /const loadMenuOptions = async[\s\S]*?};/g,
  `const loadMenuOptions = async (keyword?: string) => {
  if (!isMounted.value) return;
  menuLoading.value = true;
  try {
    const items = await getMenusAll();
    if (!isMounted.value) return;
    const formatted = items.map((item: any) => ({
      ...item,
      key: Number(item.id),
      value: Number(item.id),
      title: \`\${item.name}\`,
      id: Number(item.id),
      parentId: item.parentId ? Number(item.parentId) : 0
    }));
    const keywordTrimmed = keyword?.trim().toLowerCase();
    const filtered = keywordTrimmed ? formatted.filter((f: any) => f.title.toLowerCase().includes(keywordTrimmed)) : formatted;
    menuTreeData.value = handleTree(filtered, "id", "parentId", "children");
  } catch (error) {
    if (isMounted.value) {
      // ignore
    }
  } finally {
    if (isMounted.value) menuLoading.value = false;
  }
};`
);

// 4. getDepartmentsAll
content = content.replace(
  /const loadDepartmentOptions = async[\s\S]*?};/g,
  `const loadDepartmentOptions = async (keyword?: string) => {
  if (!isMounted.value) return;
  departmentLoading.value = true;
  try {
    const items = await getDepartmentsAll();
    if (!isMounted.value) return;
    const formatted = items.map((item: any) => ({
      ...item,
      key: Number(item.id),
      value: Number(item.id),
      title: \`\${item.name}\`,
      id: Number(item.id),
      parentId: item.parentId ? Number(item.parentId) : 0
    }));
    const keywordTrimmed = keyword?.trim().toLowerCase();
    const filtered = keywordTrimmed ? formatted.filter((f: any) => f.title.toLowerCase().includes(keywordTrimmed)) : formatted;
    departmentTreeData.value = handleTree(filtered, "id", "parentId", "children");
  } catch (error) {
    if (isMounted.value) {
      // ignore
    }
  } finally {
    if (isMounted.value) departmentLoading.value = false;
  }
};`
);

// 5. swallow dynamic tables APP_CONTEXT_REQUIRED
// Because there was an earlier message.error, I'll match the try/catch block
content = content.replace(
  /const loadDynamicTableOptions = async \([\s\S]*?finally \{/g,
  `const loadDynamicTableOptions = async (search?: string) => {
  if (!isMounted.value) return;
  dynamicTableLoading.value = true;
  try {
    const result = await getDynamicTablesPaged({
      pageIndex: 1,
      pageSize: 200,
      keyword: search?.trim() || undefined
    });
    if (!isMounted.value) return;
    dynamicTableOptions.value = result.items.map((item) => ({
      label: \`\${item.displayName} (\${item.tableKey})\`,
      value: item.tableKey
    }));
  } catch (error: any) {
    if (isMounted.value) {
      const isMissingAppCtx = error.payload?.code === 'APP_CONTEXT_REQUIRED' || error.message?.includes('APP_CONTEXT_REQUIRED');
      if (!isMissingAppCtx) {
        // ignore
      }
    }
  } finally {`
);


// 6. fix rootNodes TS implicit any
content = content.replace(
  /const rootNodes = \{\};/g,
  `const rootNodes: Record<string, any> = {};`
);

fs.writeFileSync('src/components/system/roles/RoleAssignPanel.vue', content);
console.log('Regex patch complete.');

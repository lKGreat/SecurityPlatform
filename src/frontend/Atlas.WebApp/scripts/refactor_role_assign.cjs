const fs = require('fs');

let content = fs.readFileSync('src/components/system/roles/RoleAssignPanel.vue', 'utf8');

content = content.replace(
  `<a-select
              v-model:value="assignModel.permissionIds"
              mode="multiple"
              style="width: 100%"
              :placeholder="t('systemRoles.permissionSelectPlaceholder')"
              :options="permissionOptions"
              :loading="permissionLoading"
              :filter-option="false"
              show-search
              @search="handlePermissionSearch"
              @focus="() => loadPermissionOptions()"
            />`,
  `<div style="margin-bottom: 12px; display: flex; align-items: center; justify-content: space-between;">
              <a-switch v-model:checked="permissionsCheckStrictly" :checked-children="'独立选择'" :un-checked-children="'父子联动'" />
            </div>
            <a-tree
              v-if="permissionTreeData.length"
              v-model:checkedKeys="permissionCheckedKeys"
              :tree-data="permissionTreeData"
              checkable
              :check-strictly="permissionsCheckStrictly"
              :height="400"
              default-expand-all
              block-node
            />`
);

content = content.replace(
  `<a-select
              v-model:value="assignModel.menuIds"
              mode="multiple"
              style="width: 100%"
              :placeholder="t('systemRoles.menuSelectPlaceholder')"
              :options="menuOptions"
              :loading="menuLoading"
              :filter-option="false"
              show-search
              @search="handleMenuSearch"
              @focus="() => loadMenuOptions()"
            />`,
  `<div style="margin-bottom: 12px; display: flex; align-items: center; justify-content: space-between;">
              <a-switch v-model:checked="menusCheckStrictly" :checked-children="'独立选择'" :un-checked-children="'父子联动'" />
            </div>
            <a-tree
              v-if="menuTreeData.length"
              v-model:checkedKeys="menuCheckedKeys"
              :tree-data="menuTreeData"
              checkable
              :check-strictly="menusCheckStrictly"
              :height="400"
              default-expand-all
              block-node
            />`
);

content = content.replace(
  /<a-table-column key="canView"[^>]*>([\s\S]*?)<\/a-table-column>/g,
  `<a-table-column key="canView" width="100" align="center">
                  <template #title>
                     <div style="display:flex; flex-direction:column; align-items:center; gap: 4px;">
                        <span>{{ t('systemRoles.fieldColumnCanView') }}</span>
                        <a-switch size="small" :checked="isAllViewChecked" @change="toggleAllView" />
                     </div>
                  </template>
                  <template #default="{ record }">
                    <a-switch
                      :checked="record.canView"
                      size="small"
                      @change="(value: boolean) => handleFieldViewChange(record.fieldName, value)"
                    />
                  </template>
                </a-table-column>`
);

content = content.replace(
  /<a-table-column key="canEdit"[^>]*>([\s\S]*?)<\/a-table-column>/g,
  `<a-table-column key="canEdit" width="100" align="center">
                  <template #title>
                     <div style="display:flex; flex-direction:column; align-items:center; gap: 4px;">
                        <span>{{ t('systemRoles.fieldColumnCanEdit') }}</span>
                        <a-switch size="small" :checked="isAllEditChecked" @change="toggleAllEdit" />
                     </div>
                  </template>
                  <template #default="{ record }">
                    <a-switch
                      :checked="record.canEdit"
                      size="small"
                      :disabled="!record.canView"
                      @change="(value: boolean) => handleFieldEditChange(record.fieldName, value)"
                    />
                  </template>
                </a-table-column>`
);

content = content.replace(
  `<a-select
                v-model:value="assignModel.deptIds"
                mode="multiple"
                style="width: 100%"
                :placeholder="t('systemRoles.departmentSelectPlaceholder')"
                :options="departmentOptions"
                :loading="departmentLoading"
                :filter-option="false"
                show-search
                @search="handleDepartmentSearch"
                @focus="() => loadDepartmentOptions()"
              />`,
  `<a-tree-select
                v-model:value="assignModel.deptIds"
                tree-checkable
                style="width: 100%"
                :placeholder="t('systemRoles.departmentSelectPlaceholder')"
                :tree-data="departmentTreeData"
                :loading="departmentLoading"
                allow-clear
                tree-default-expand-all
                :max-tag-count="5"
                show-search
                tree-node-filter-prop="title"
              />`
);

content = content.replace(
  `import { debounce, type SelectOption } from '@/utils/common';`,
  `import { debounce, handleTree, type SelectOption } from '@/utils/common';\nimport type { DataNode } from 'ant-design-vue/es/tree';`
);

let stateAdditions = `
const permissionsCheckStrictly = ref(true);
const menusCheckStrictly = ref(true);
const permissionCheckedKeys = ref<any>([]);
const menuCheckedKeys = ref<any>([]);
const permissionTreeData = ref<DataNode[]>([]);
const menuTreeData = ref<DataNode[]>([]);
const departmentTreeData = ref<DataNode[]>([]);

watch(permissionCheckedKeys, (val) => {
  const keys = Array.isArray(val) ? val : (val ? val.checked : []);
  assignModel.permissionIds = keys.filter((k: any) => typeof k === 'number') as number[];
}, { deep: true });

watch(menuCheckedKeys, (val) => {
  const keys = Array.isArray(val) ? val : (val ? val.checked : []);
  assignModel.menuIds = keys.filter((k: any) => typeof k === 'number') as number[];
}, { deep: true });

const isAllViewChecked = computed(() => {
  if (fieldPermissionRows.value.length === 0) return false;
  return fieldPermissionRows.value.every(row => row.canView);
});

const isAllEditChecked = computed(() => {
  if (fieldPermissionRows.value.length === 0) return false;
  return fieldPermissionRows.value.every(row => row.canEdit);
});

const toggleAllView = (val: boolean) => {
  fieldPermissionRows.value.forEach(row => {
    row.canView = val;
    if (!val) {
      row.canEdit = false;
    }
  });
};

const toggleAllEdit = (val: boolean) => {
  fieldPermissionRows.value.forEach(row => {
    row.canEdit = val;
    if (val) {
      row.canView = true;
    }
  });
};
`;

content = content.replace(
  `const permissionOptions = ref<SelectOption[]>([]);`,
  `const permissionOptions = ref<SelectOption[]>([]);` + stateAdditions
);

content = content.replace(
  /const loadPermissionOptions = async[\s\S]*?};/g,
  `const loadPermissionOptions = async (keyword?: string) => {
  if (!isMounted.value) return;
  permissionLoading.value = true;
  try {
    const result = await getPermissionsPaged({
      pageIndex: 1,
      pageSize: 1000,
      keyword: keyword?.trim() || undefined
    });
    if (!isMounted.value) return;
    const items = result.items;
    const rootNodes = {};
    items.forEach(item => {
      const parts = item.code.split(':');
      const moduleCode = parts[0] || '默认';
      if (!rootNodes[moduleCode]) {
         rootNodes[moduleCode] = {
           key: \`module_\${moduleCode}\`,
           title: moduleCode.toUpperCase(),
           children: []
         };
      }
      rootNodes[moduleCode].children.push({
        key: Number(item.id),
        title: \`\${item.name} (\${item.code})\`
      });
    });
    permissionTreeData.value = Object.values(rootNodes);
  } catch (error) {
    if (isMounted.value) {
      // message.error((error as Error).message || "加载权限失败");
    }
  } finally {
    if (isMounted.value) {
      permissionLoading.value = false;
    }
  }
};`
);

content = content.replace(
  /const loadMenuOptions = async[\s\S]*?};/g,
  `const loadMenuOptions = async (keyword?: string) => {
  if (!isMounted.value) return;
  menuLoading.value = true;
  try {
    const result = await getMenusPaged({
      pageIndex: 1,
      pageSize: 1000,
      keyword: keyword?.trim() || undefined
    });
    if (!isMounted.value) return;
    const formatted = result.items.map(item => ({
      ...item,
      key: Number(item.id),
      value: Number(item.id),
      title: \`\${item.name}\`,
      id: Number(item.id),
      parentId: item.parentId ? Number(item.parentId) : 0
    }));
    menuTreeData.value = handleTree(formatted, "id", "parentId", "children");
  } catch (error) {
    if (isMounted.value) {
      // message.error((error as Error).message || "加载菜单失败");
    }
  } finally {
    if (isMounted.value) {
      menuLoading.value = false;
    }
  }
};`
);

content = content.replace(
  /const loadDepartmentOptions = async[\s\S]*?};/g,
  `const loadDepartmentOptions = async (keyword?: string) => {
  if (!isMounted.value) return;
  departmentLoading.value = true;
  try {
    const result = await getDepartmentsPaged({
      pageIndex: 1,
      pageSize: 1000,
      keyword: keyword?.trim() || undefined
    });
    if (!isMounted.value) return;
    const formatted = result.items.map(item => ({
      ...item,
      key: Number(item.id),
      value: Number(item.id),
      title: \`\${item.name}\`,
      id: Number(item.id),
      parentId: item.parentId ? Number(item.parentId) : 0
    }));
    departmentTreeData.value = handleTree(formatted, "id", "parentId", "children");
  } catch (error) {
    if (isMounted.value) {
      // message.error((error as Error).message || "加载部门失败");
    }
  } finally {
    if (isMounted.value) {
      departmentLoading.value = false;
    }
  }
};`
);

content = content.replace(
  `assignModel.permissionIds = detail.permissionIds?.slice() ?? [];
    assignModel.menuIds = detail.menuIds?.slice() ?? [];`,
  `assignModel.permissionIds = detail.permissionIds?.slice() ?? [];
    permissionCheckedKeys.value = assignModel.permissionIds;
    assignModel.menuIds = detail.menuIds?.slice() ?? [];
    menuCheckedKeys.value = assignModel.menuIds;`
);

fs.writeFileSync('src/components/system/roles/RoleAssignPanel.vue', content);
console.log('Done!');

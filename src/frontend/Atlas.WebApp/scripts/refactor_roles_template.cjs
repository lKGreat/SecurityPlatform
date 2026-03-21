const fs = require('fs');
let content = fs.readFileSync('src/components/system/roles/RoleAssignPanel.vue', 'utf8');

// 1. permissions a-select
content = content.replace(
  /<a-select[\s\S]*?v-model:value="assignModel\.permissionIds"[\s\S]*?\/>/,
  `<div style="display: flex; justify-content: flex-end; margin-bottom: 8px;">
              <a-switch v-model:checked="permissionsCheckStrictly" :checked-children="t('systemRoles.independentSelection')" :un-checked-children="t('systemRoles.parentChildLinkage')" />
            </div>
            <a-tree
              v-model:checkedKeys="permissionCheckedKeys"
              checkable
              :check-strictly="permissionsCheckStrictly"
              :tree-data="permissionTreeData"
              :selectable="false"
              default-expand-all
              style="max-height: 400px; overflow-y: auto; border: 1px solid #f0f0f0; border-radius: 6px; padding: 12px; background: #fafafa;"
            />`
);

// 2. menus a-select
content = content.replace(
  /<a-select[\s\S]*?v-model:value="assignModel\.menuIds"[\s\S]*?\/>/,
  `<div style="display: flex; justify-content: flex-end; margin-bottom: 8px;">
              <a-switch v-model:checked="menusCheckStrictly" :checked-children="t('systemRoles.independentSelection')" :un-checked-children="t('systemRoles.parentChildLinkage')" />
            </div>
            <a-tree
              v-model:checkedKeys="menuCheckedKeys"
              checkable
              :check-strictly="menusCheckStrictly"
              :tree-data="menuTreeData"
              :selectable="false"
              default-expand-all
              style="max-height: 400px; overflow-y: auto; border: 1px solid #f0f0f0; border-radius: 6px; padding: 12px; background: #fafafa;"
            />`
);

// 3. field permissions batch toggles
content = content.replace(
  /<a-table-column title="可查看" data-index="canView" align="center" width="100">/,
  `<a-table-column data-index="canView" align="center" width="100">
                  <template #title>
                    <div style="display: flex; flex-direction: column; align-items: center; gap: 4px;">
                      <span>可查看</span>
                      <a-switch size="small" :checked="isAllViewChecked" @change="toggleAllView" />
                    </div>
                  </template>`
);

content = content.replace(
  /<a-table-column title="可编辑" data-index="canEdit" align="center" width="100">/,
  `<a-table-column data-index="canEdit" align="center" width="100">
                  <template #title>
                    <div style="display: flex; flex-direction: column; align-items: center; gap: 4px;">
                      <span>可编辑</span>
                      <a-switch size="small" :checked="isAllEditChecked" @change="toggleAllEdit" />
                    </div>
                  </template>`
);

// 4. departments a-select -> a-tree-select
content = content.replace(
  /<a-select[\s\S]*?v-model:value="assignModel\.deptIds"[\s\S]*?\/>/,
  `<a-tree-select
                v-model:value="assignModel.deptIds"
                tree-checkable
                tree-default-expand-all
                :tree-data="departmentTreeData"
                allow-clear
                style="width: 100%"
                :placeholder="t('systemRoles.departmentSelectPlaceholder')"
                :dropdown-style="{ maxHeight: '400px', overflow: 'auto' }"
                :show-checked-strategy="'SHOW_ALL'"
              />`
);

// 5. Add reactive state updates... Oh wait, I didn't verify if my previous script failed to add the state updates. 
// Let's check if `permissionTreeData` is already defined in the file.
if (!content.includes('const permissionTreeData = ref<DataNode[]>([])')) {
  // Let's inject them below `const assignModel = reactive({` block ending at `});`
  content = content.replace(
    /const assignModel = reactive\(\{[\s\S]*?\}\);/,
    `$&

const permissionTreeData = ref<DataNode[]>([]);
const permissionCheckedKeys = ref<number[] | { checked: number[]; halfChecked: number[] }>([]);
const permissionsCheckStrictly = ref(true);

const menuTreeData = ref<DataNode[]>([]);
const menuCheckedKeys = ref<number[] | { checked: number[]; halfChecked: number[] }>([]);
const menusCheckStrictly = ref(true);

const departmentTreeData = ref<DataNode[]>([]);

const isAllViewChecked = computed(() => {
  if (fieldPermissionRows.value.length === 0) return false;
  return fieldPermissionRows.value.every(row => row.canView);
});

const isAllEditChecked = computed(() => {
  if (fieldPermissionRows.value.length === 0) return false;
  return fieldPermissionRows.value.every(row => row.canEdit);
});

const toggleAllView = (checked: boolean) => {
  fieldPermissionRows.value.forEach(row => {
    row.canView = checked;
    // If we uncheck view, also uncheck edit since view is a prerequisite
    if (!checked) row.canEdit = false;
  });
};

const toggleAllEdit = (checked: boolean) => {
  fieldPermissionRows.value.forEach(row => {
    row.canEdit = checked;
    // If we check edit, also check view since view is a prerequisite
    if (checked) row.canView = true;
  });
};

watch(permissionCheckedKeys, (val) => {
  if (Array.isArray(val)) {
    assignModel.permissionIds = val;
  } else if (val && val.checked) {
    assignModel.permissionIds = val.checked;
  }
});

watch(menuCheckedKeys, (val) => {
  if (Array.isArray(val)) {
    assignModel.menuIds = val;
  } else if (val && val.checked) {
    assignModel.menuIds = val.checked;
  }
});

watch(() => assignModel.permissionIds, (newVal) => {
  if (Array.isArray(newVal) && (!Array.isArray(permissionCheckedKeys.value) || newVal.join(',') !== (permissionCheckedKeys.value as number[]).join(','))) {
    permissionCheckedKeys.value = [...newVal];
  }
});

watch(() => assignModel.menuIds, (newVal) => {
  if (Array.isArray(newVal) && (!Array.isArray(menuCheckedKeys.value) || newVal.join(',') !== (menuCheckedKeys.value as number[]).join(','))) {
    menuCheckedKeys.value = [...newVal];
  }
});
`
  );
}

fs.writeFileSync('src/components/system/roles/RoleAssignPanel.vue', content);
console.log('Template replace script completed.');

<template>
  <div class="role-assign-panel">
    <div class="panel-header">
      <div class="panel-title">
        {{ t("systemRoles.assignPanelTitle", { roleName, roleCode }) }}
      </div>
      <a-space>
        <a-tag v-if="scope === 'app'" color="blue">{{ t('systemRoles.appScope', '应用级') }}</a-tag>
        <a-button type="primary" :loading="submitting" @click="submitAssign">{{ t("systemRoles.saveAssign") }}</a-button>
      </a-space>
    </div>

    <div class="panel-content">
      <a-spin :spinning="loading">
        <a-tabs v-model:active-key="activeTab">
          <a-tab-pane v-if="canAssignPermissions" key="permissions" :tab="t('systemRoles.permissionTab')">
            <a-alert
              :message="t('systemRoles.permissionTabHint')"
              type="info"
              show-icon
              style="margin-bottom: 12px"
            />
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
              <a-space>
                <a-button size="small" @click="selectAllPermissions">{{ t('systemRoles.selectAll', '全选') }}</a-button>
                <a-button size="small" @click="clearAllPermissions">{{ t('systemRoles.clearAll', '清空') }}</a-button>
              </a-space>
              <a-switch v-model:checked="permissionsCheckStrictly" :checked-children="t('systemRoles.independentSelection')" :un-checked-children="t('systemRoles.parentChildLinkage')" />
            </div>
            <a-tree
              v-model:checked-keys="permissionCheckedKeys"
              checkable
              :check-strictly="permissionsCheckStrictly"
              :tree-data="permissionTreeData"
              :selectable="false"
              default-expand-all
              style="max-height: 400px; overflow-y: auto; border: 1px solid #f0f0f0; border-radius: 6px; padding: 12px; background: #fafafa;"
            />
          </a-tab-pane>
          
          <a-tab-pane v-if="canAssignMenus" key="menus" :tab="scope === 'app' ? t('systemRoles.pageTab', '页面分配') : t('systemRoles.menuTab')">
            <a-alert
              :message="scope === 'app' ? t('systemRoles.pageTabHint', '勾选此应用角色可访问的页面') : t('systemRoles.menuTabHint')"
              type="info"
              show-icon
              style="margin-bottom: 12px"
            />
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
              <a-space>
                <a-button size="small" @click="selectAllMenus">{{ t('systemRoles.selectAll', '全选') }}</a-button>
                <a-button size="small" @click="clearAllMenus">{{ t('systemRoles.clearAll', '清空') }}</a-button>
              </a-space>
              <a-switch v-model:checked="menusCheckStrictly" :checked-children="t('systemRoles.independentSelection')" :un-checked-children="t('systemRoles.parentChildLinkage')" />
            </div>
            <a-tree
              v-model:checked-keys="menuCheckedKeys"
              checkable
              :check-strictly="menusCheckStrictly"
              :tree-data="menuTreeData"
              :selectable="false"
              default-expand-all
              style="max-height: 400px; overflow-y: auto; border: 1px solid #f0f0f0; border-radius: 6px; padding: 12px; background: #fafafa;"
            />
          </a-tab-pane>

          <a-tab-pane v-if="canAssignPermissions" key="field-permissions" :tab="t('systemRoles.fieldPermissionTab')">
            <a-alert
              :message="t('systemRoles.fieldPermissionTabHint')"
              type="info"
              show-icon
              style="margin-bottom: 12px"
            />
            <a-select
              v-model:value="fieldPermissionTableKey"
              style="width: 100%; margin-bottom: 12px"
              :placeholder="t('systemRoles.dynamicTableSelectPlaceholder')"
              :options="dynamicTableOptions"
              :loading="dynamicTableLoading"
              :filter-option="false"
              show-search
              @search="handleDynamicTableSearch"
              @change="handleFieldPermissionTableChange"
              @focus="() => loadDynamicTableOptions()"
            />
            <div class="table-container">
              <a-table
                :data-source="fieldPermissionRows"
                :loading="fieldPermissionLoading"
                :pagination="false"
                row-key="fieldName"
                size="small"
                :scroll="{ y: 'calc(100vh - 350px)' }"
              >
                <a-table-column key="label" :title="t('systemRoles.fieldColumnLabel')" data-index="label" />
                <a-table-column key="canView" data-index="canView" align="center" width="100">
                  <template #title>
                    <div style="display: flex; flex-direction: column; align-items: center; gap: 4px;">
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
                </a-table-column>
                <a-table-column key="canEdit" data-index="canEdit" align="center" width="100">
                  <template #title>
                    <div style="display: flex; flex-direction: column; align-items: center; gap: 4px;">
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
                </a-table-column>
              </a-table>
            </div>
          </a-tab-pane>

          <a-tab-pane v-if="canManageDataScope" key="data-scope" :tab="t('systemRoles.dataScopeTab')">
            <a-alert
              :message="t('systemRoles.dataScopeTabHint')"
              type="info"
              show-icon
              style="margin-bottom: 16px"
            />
            <a-radio-group v-model:value="assignModel.dataScope" button-style="solid" class="display-block-radio">
              <a-radio-button
                v-for="scope in dataScopeOptions"
                :key="scope.value"
                :value="scope.value"
                style="display: block; width: 100%; text-align: left; margin-bottom: 8px; border-radius: 6px; border-left: 1px solid #d9d9d9;"
              >
                <div class="scope-radio-content">
                  <div class="scope-label">{{ scope.label }}</div>
                  <div class="scope-desc">{{ scope.description }}</div>
                </div>
              </a-radio-button>
            </a-radio-group>

            <div v-if="assignModel.dataScope === 2" style="margin-top: 12px;">
              <a-tree-select
                v-model:value="assignModel.deptIds"
                tree-checkable
                tree-default-expand-all
                :tree-data="departmentTreeData"
                allow-clear
                style="width: 100%"
                :placeholder="t('systemRoles.departmentSelectPlaceholder')"
                :dropdown-style="{ maxHeight: '400px', overflow: 'auto' }"
                :show-checked-strategy="'SHOW_ALL'"
              />
            </div>
            <a-alert
              style="margin-top: 12px"
              type="success"
              show-icon
              :message="dataScopePreviewTitle"
              :description="dataScopePreviewDescription"
            />
            <a-alert
              v-if="assignModel.dataScope === 6 && !projectScopeEnabled"
              style="margin-top: 8px"
              type="warning"
              show-icon
              :message="t('systemRoles.projectScopeConsistencyWarning')"
            />
          </a-tab-pane>
        </a-tabs>
      </a-spin>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch, onBeforeUnmount } from 'vue';
import { message } from 'ant-design-vue';
import { useI18n } from "vue-i18n";
import {
  getRoleDetail,
  updateRolePermissions,
  updateRoleMenus,
  setRoleDataScope,
  getPermissionsPaged,
  getMenusAll,
  getDepartmentsAll
} from '@/services/api';
import {
  getDynamicTablesPaged,
  getDynamicTableFields,
  getDynamicFieldPermissions,
  setDynamicFieldPermissions
} from '@/services/dynamic-tables';
import {
  getTenantAppRoleDetail,
  updateTenantAppRolePermissions,
  getAppRoleDataScope,
  setAppRoleDataScope,
  getAppDepartmentAll,
  getAppPermissionsPaged,
  getAppRoleAvailablePages,
  getAppRolePages,
  setAppRolePages,
  getAppRoleFieldPermissions,
  setAppRoleFieldPermissions
} from '@/services/api-app-members';
import { debounce, handleTree, type SelectOption } from '@/utils/common';
import type { DataNode } from 'ant-design-vue/es/tree';
import { getProjectScopeEnabled } from "@/utils/auth";
import type { AppPageListItem, AppRoleFieldPermissionGroup } from '@/types/platform-v2';

const { t } = useI18n();

const props = defineProps<{
  roleId: string | null;
  roleCode: string;
  roleName: string;
  canAssignPermissions: boolean;
  canAssignMenus: boolean;
  canManageDataScope: boolean;
  /** 上下文范围：platform=平台级，app=应用级 */
  scope?: 'platform' | 'app';
  /** 应用 ID，scope=app 时必填 */
  appId?: string;
}>();

const emit = defineEmits<{
  (e: 'success'): void;
}>();

const isAppScope = computed(() => props.scope === 'app' && !!props.appId);

const isMounted = ref(true);
onBeforeUnmount(() => {
  isMounted.value = false;
});

const loading = ref(false);
const submitting = ref(false);
const activeTab = ref(
  props.canAssignPermissions
    ? 'permissions'
    : props.canAssignMenus
      ? 'menus'
      : 'data-scope'
);
const projectScopeEnabled = getProjectScopeEnabled();

const dataScopeOptions = [
  { value: 0, label: t("systemRoles.scopeAllLabel"), description: t("systemRoles.scopeAllDesc") },
  { value: 2, label: t("systemRoles.scopeCustomDeptLabel"), description: t("systemRoles.scopeCustomDeptDesc") },
  { value: 3, label: t("systemRoles.scopeCurrentDeptLabel"), description: t("systemRoles.scopeCurrentDeptDesc") },
  { value: 4, label: t("systemRoles.scopeCurrentDeptAndBelowLabel"), description: t("systemRoles.scopeCurrentDeptAndBelowDesc") },
  { value: 5, label: t("systemRoles.scopeOnlySelfLabel"), description: t("systemRoles.scopeOnlySelfDesc") },
  { value: 6, label: t("systemRoles.scopeProjectLabel"), description: t("systemRoles.scopeProjectDesc") }
];

const assignModel = reactive({
  permissionIds: [] as number[],
  permissionCodes: [] as string[],
  menuIds: [] as number[],
  dataScope: 0 as number,
  deptIds: [] as number[]
});

const permissionTreeData = ref<DataNode[]>([]);
const permissionCheckedKeys = ref<(number | string)[] | { checked: (number | string)[]; halfChecked: (number | string)[] }>([]);
const permissionsCheckStrictly = ref(true);

const menuTreeData = ref<DataNode[]>([]);
const menuCheckedKeys = ref<(number | string)[] | { checked: (number | string)[]; halfChecked: (number | string)[] }>([]);
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
    if (!checked) row.canEdit = false;
  });
};

const toggleAllEdit = (checked: boolean) => {
  fieldPermissionRows.value.forEach(row => {
    row.canEdit = checked;
    if (checked) row.canView = true;
  });
};

const filterNumericKeys = (keys: (number | string)[]) => keys.filter((k): k is number => typeof k === 'number' && !isNaN(k));

const collectTreeKeys = (nodes: DataNode[]): (number | string)[] => {
  let keys: (number | string)[] = [];
  for (const node of nodes) {
    keys.push(node.key as (number | string));
    if (node.children && node.children.length > 0) {
      keys = keys.concat(collectTreeKeys(node.children));
    }
  }
  return keys;
};

const selectAllPermissions = () => {
  permissionCheckedKeys.value = collectTreeKeys(permissionTreeData.value);
};

const clearAllPermissions = () => {
  permissionCheckedKeys.value = [];
};

const selectAllMenus = () => {
  menuCheckedKeys.value = collectTreeKeys(menuTreeData.value);
};

const clearAllMenus = () => {
  menuCheckedKeys.value = [];
};

// 平台模式：key 是 ID（数字），应用模式：key 是编码（字符串，前缀 perm_code:XXX）
const PERM_CODE_PREFIX = 'perm_code:';

watch(permissionCheckedKeys, (val) => {
  const keys = Array.isArray(val) ? val : (val?.checked ?? []);
  if (isAppScope.value) {
    // 应用模式：只收集以 PERM_CODE_PREFIX 开头的叶子节点
    assignModel.permissionCodes = keys
      .filter((k): k is string => typeof k === 'string' && k.startsWith(PERM_CODE_PREFIX))
      .map(k => k.slice(PERM_CODE_PREFIX.length));
  } else {
    assignModel.permissionIds = filterNumericKeys(Array.isArray(val) ? val : (val?.checked ?? []));
  }
});

watch(menuCheckedKeys, (val) => {
  if (Array.isArray(val)) {
    assignModel.menuIds = filterNumericKeys(val);
  } else if (val && val.checked) {
    assignModel.menuIds = filterNumericKeys(val.checked);
  }
});

watch(() => assignModel.permissionIds, (newVal) => {
  if (!isAppScope.value && Array.isArray(newVal) && (!Array.isArray(permissionCheckedKeys.value) || newVal.join(',') !== (permissionCheckedKeys.value as number[]).join(','))) {
    permissionCheckedKeys.value = [...newVal];
  }
});

watch(() => assignModel.menuIds, (newVal) => {
  if (Array.isArray(newVal) && (!Array.isArray(menuCheckedKeys.value) || newVal.join(',') !== (menuCheckedKeys.value as number[]).join(','))) {
    menuCheckedKeys.value = [...newVal];
  }
});

const permissionOptions = ref<SelectOption[]>([]);
const menuOptions = ref<SelectOption[]>([]);
const departmentOptions = ref<SelectOption[]>([]);
const permissionLoading = ref(false);
const menuLoading = ref(false);
const departmentLoading = ref(false);

// 应用级页面列表（app scope 时用于"页面分配"Tab）
const appPageListItems = ref<AppPageListItem[]>([]);
// 应用级字段权限所有分组（预加载，app scope 时用）
const appFieldPermissionGroups = ref<AppRoleFieldPermissionGroup[]>([]);

const dynamicTableOptions = ref<Array<{ label: string; value: string }>>([]);
const dynamicTableLoading = ref(false);
const fieldPermissionLoading = ref(false);
const fieldPermissionTableKey = ref<string>();
const existingFieldPermissions = ref<Array<{
  fieldName: string;
  roleCode: string;
  canView: boolean;
  canEdit: boolean;
}>>([]);
const fieldPermissionRows = ref<Array<{
  fieldName: string;
  label: string;
  canView: boolean;
  canEdit: boolean;
}>>([]);

const selectedDataScopeOption = computed(() => {
  return dataScopeOptions.find((item) => item.value === assignModel.dataScope) ?? dataScopeOptions[0];
});

const dataScopePreviewTitle = computed(() => t("systemRoles.dataScopePreviewTitle"));
const dataScopePreviewDescription = computed(() => {
  if (assignModel.dataScope === 2) {
    return t("systemRoles.dataScopePreviewCustomDept", { count: assignModel.deptIds.length });
  }
  return selectedDataScopeOption.value.description;
});

const loadPermissionOptions = async (keyword?: string) => {
  if (!isMounted.value) return;
  permissionLoading.value = true;
  try {
    let items: Array<{ id: string; code: string; name: string }> = [];
    if (isAppScope.value && props.appId) {
      // 应用模式：从应用级权限 API 加载
      const result = await getAppPermissionsPaged(props.appId, {
        pageIndex: 1,
        pageSize: 200,
        keyword: keyword?.trim() || undefined
      });
      items = result.items;
    } else {
      // 平台模式：从平台级权限 API 加载
      const result = await getPermissionsPaged({
        pageIndex: 1,
        pageSize: 200,
        keyword: keyword?.trim() || undefined
      });
      items = result.items;
    }
    if (!isMounted.value) return;
    const rootNodes: Record<string, DataNode> = {};
    items.forEach((item: { id: string; code: string; name: string }) => {
      const parts = item.code.split(':');
      const moduleCode = parts[0] || '默认';
      if (!rootNodes[moduleCode]) {
        rootNodes[moduleCode] = {
          key: `module_${moduleCode}`,
          title: moduleCode.toUpperCase(),
          children: []
        };
      }
      // 应用模式：key 用字符串编码，平台模式：key 用数字 ID
      const nodeKey = isAppScope.value ? `${PERM_CODE_PREFIX}${item.code}` : Number(item.id);
      (rootNodes[moduleCode].children as DataNode[]).push({
        key: nodeKey,
        title: `${item.name} (${item.code})`
      });
    });
    permissionTreeData.value = Object.values(rootNodes);
  } catch {
    // silent
  } finally {
    if (isMounted.value) {
      permissionLoading.value = false;
    }
  }
};

const loadMenuOptions = async (keyword?: string) => {
  if (!isMounted.value) return;
  menuLoading.value = true;
  try {
    const items = await getMenusAll();
    if (!isMounted.value) return;
    const formatted = items.map((item) => ({
      ...item,
      key: Number(item.id),
      value: Number(item.id),
      title: `${item.name}`,
      id: Number(item.id),
      parentId: item.parentId ? Number(item.parentId) : 0
    }));
    const keywordTrimmed = keyword?.trim().toLowerCase();
    const filtered = keywordTrimmed ? formatted.filter((f) => (f.title as string).toLowerCase().includes(keywordTrimmed)) : formatted;
    menuTreeData.value = handleTree(filtered, "id", "parentId", "children");
  } catch {
    // silent
  } finally {
    if (isMounted.value) {
      menuLoading.value = false;
    }
  }
};

const handlePermissionSearch = debounce((value: string) => void loadPermissionOptions(value));
const handleMenuSearch = debounce((value: string) => void loadMenuOptions(value));

const loadAppPageOptions = async () => {
  if (!isMounted.value || !props.appId) return;
  menuLoading.value = true;
  try {
    const pages = await getAppRoleAvailablePages(props.appId);
    if (!isMounted.value) return;
    appPageListItems.value = pages;
    const formatted = pages.map((p) => ({
      key: Number(p.id),
      value: Number(p.id),
      title: p.name,
      id: Number(p.id),
      parentId: p.parentPageId ?? 0
    }));
    menuTreeData.value = handleTree(formatted, "id", "parentId", "children");
  } catch {
    // silent
  } finally {
    if (isMounted.value) menuLoading.value = false;
  }
};

const loadAppFieldPermissions = async () => {
  if (!isMounted.value || !props.appId || !props.roleId) return;
  try {
    const groups = await getAppRoleFieldPermissions(props.appId, props.roleId);
    if (!isMounted.value) return;
    appFieldPermissionGroups.value = groups;
    dynamicTableOptions.value = groups.map((g) => ({
      label: g.tableKey,
      value: g.tableKey
    }));
  } catch {
    // silent
  }
};
const loadDepartmentOptions = async (keyword?: string) => {
  if (!isMounted.value) return;
  departmentLoading.value = true;
  try {
    let items: Array<{ id: string; name: string; parentId?: string | number | null }> = [];
    if (isAppScope.value && props.appId) {
      // 应用模式：加载应用级部门
      const appDepts = await getAppDepartmentAll(props.appId);
      items = appDepts.map(d => ({ id: d.id, name: d.name, parentId: d.parentId }));
    } else {
      // 平台模式：加载平台级部门
      items = await getDepartmentsAll();
    }
    if (!isMounted.value) return;
    const formatted = items.map((item) => ({
      ...item,
      key: Number(item.id),
      value: Number(item.id),
      title: item.name,
      id: Number(item.id),
      parentId: item.parentId ? Number(item.parentId) : 0
    }));
    const keywordTrimmed = keyword?.trim().toLowerCase();
    const filtered = keywordTrimmed ? formatted.filter((f) => (f.title as string).toLowerCase().includes(keywordTrimmed)) : formatted;
    departmentTreeData.value = handleTree(filtered, "id", "parentId", "children");
  } catch {
    // silent
  } finally {
    if (isMounted.value) {
      departmentLoading.value = false;
    }
  }
};

const handleDepartmentSearch = debounce((value: string) => void loadDepartmentOptions(value));

const loadDynamicTableOptions = async (search?: string) => {
  if (!isMounted.value) return;
  dynamicTableLoading.value = true;
  try {
    const result = await getDynamicTablesPaged({
      pageIndex: 1,
      pageSize: 100,
      keyword: search?.trim() || undefined
    }, { suppressErrorMessage: true });
    if (!isMounted.value) return;
    dynamicTableOptions.value = result.items.map((item) => ({
      label: `${item.displayName} (${item.tableKey})`,
      value: item.tableKey
    }));
  } catch {
    // silent
  } finally {
    if (isMounted.value) {
      dynamicTableLoading.value = false;
    }
  }
};

const handleDynamicTableSearch = debounce((value: string) => void loadDynamicTableOptions(value));

const loadFieldPermissions = async (tableKey: string) => {
  if (!props.roleCode || !isMounted.value) return;

  fieldPermissionLoading.value = true;
  try {
    const [fields, permissions] = await Promise.all([
      getDynamicTableFields(tableKey),
      getDynamicFieldPermissions(tableKey)
    ]);
    if (!isMounted.value) return;
    existingFieldPermissions.value = permissions;
    fieldPermissionRows.value = fields.map((field) => {
      const current = permissions.find((item) =>
        item.roleCode === props.roleCode && item.fieldName === field.name
      );
      return {
        fieldName: field.name,
        label: field.displayName || field.name,
        canView: current?.canView ?? false,
        canEdit: current?.canEdit ?? false
      };
    });
  } catch (error) {
    if (isMounted.value) {
      message.error((error as Error).message || "加载字段权限失败");
    }
  } finally {
    if (isMounted.value) {
      fieldPermissionLoading.value = false;
    }
  }
};

const handleFieldPermissionTableChange = (value: string) => {
  if (!value) {
    fieldPermissionRows.value = [];
    existingFieldPermissions.value = [];
    return;
  }
  if (isAppScope.value) {
    const group = appFieldPermissionGroups.value.find((g) => g.tableKey === value);
    fieldPermissionRows.value = group
      ? group.fields.map((f) => ({ fieldName: f.fieldName, label: f.fieldName, canView: f.canView, canEdit: f.canEdit }))
      : [];
    existingFieldPermissions.value = [];
  } else {
    void loadFieldPermissions(value);
  }
};

const handleFieldViewChange = (fieldName: string, value: boolean) => {
  const target = fieldPermissionRows.value.find((item) => item.fieldName === fieldName);
  if (!target) return;
  target.canView = value;
  if (!value) target.canEdit = false;
};

const handleFieldEditChange = (fieldName: string, value: boolean) => {
  const target = fieldPermissionRows.value.find((item) => item.fieldName === fieldName);
  if (!target) return;
  target.canEdit = value;
  if (value) target.canView = true;
};

const fetchRoleDetail = async () => {
  if (!props.roleId || !isMounted.value) return;
  
  loading.value = true;
  fieldPermissionTableKey.value = undefined;
  fieldPermissionRows.value = [];
  existingFieldPermissions.value = [];

  try {
    if (isAppScope.value && props.appId) {
      // 应用模式：并行加载权限选项、动态表等
      await Promise.all([
        props.canAssignPermissions ? loadPermissionOptions() : Promise.resolve(),
        props.canAssignMenus ? loadAppPageOptions() : Promise.resolve(),
        props.canAssignPermissions ? loadAppFieldPermissions() : Promise.resolve(),
        props.canManageDataScope ? loadDepartmentOptions() : Promise.resolve()
      ]);
      if (!isMounted.value) return;

      const [roleDetail, dataScopeDetail, pageIds] = await Promise.all([
        getTenantAppRoleDetail(props.appId, props.roleId),
        props.canManageDataScope ? getAppRoleDataScope(props.appId, props.roleId) : Promise.resolve(null),
        props.canAssignMenus ? getAppRolePages(props.appId, props.roleId) : Promise.resolve([])
      ]);
      if (!isMounted.value) return;

      // 应用角色：权限是 codes
      assignModel.permissionCodes = roleDetail.permissionCodes?.slice() ?? [];
      permissionCheckedKeys.value = assignModel.permissionCodes.map(code => `${PERM_CODE_PREFIX}${code}`);

      // 页面分配
      assignModel.menuIds = pageIds.map(Number).filter(Number.isFinite);

      if (dataScopeDetail) {
        assignModel.dataScope = dataScopeDetail.dataScope ?? 0;
        assignModel.deptIds = (dataScopeDetail.deptIds ?? [])
          .map(v => Number(v))
          .filter(v => Number.isFinite(v));
      }
    } else {
      // 平台模式：原有逻辑
      await Promise.all([
        props.canAssignPermissions ? loadPermissionOptions() : Promise.resolve(),
        props.canAssignMenus ? loadMenuOptions() : Promise.resolve(),
        props.canAssignPermissions ? loadDynamicTableOptions() : Promise.resolve(),
        props.canManageDataScope ? loadDepartmentOptions() : Promise.resolve()
      ]);

      if (!isMounted.value) return;
      const detail = await getRoleDetail(props.roleId);
      if (!isMounted.value) return;
      assignModel.permissionIds = detail.permissionIds?.slice() ?? [];
      assignModel.menuIds = detail.menuIds?.slice() ?? [];
      // 兼容历史值：CurrentTenant(1) 在前端统一映射为"全部数据(0)"展示。
      assignModel.dataScope = detail.dataScope === 1 ? 0 : (detail.dataScope ?? 0);
      assignModel.deptIds = (detail.deptIds ?? [])
        .map((value) => Number(value))
        .filter((value) => Number.isFinite(value));
    }
  } catch (error) {
    if (isMounted.value) {
      message.error((error as Error).message || "加载角色详情失败");
    }
  } finally {
    if (isMounted.value) {
      loading.value = false;
    }
  }
};

const submitAssign = async () => {
  if (!props.roleId || !isMounted.value) return;
  
  submitting.value = true;
  try {
    const tasks: Promise<unknown>[] = [];

    if (isAppScope.value && props.appId) {
      // 应用模式：使用应用级 API
      if (props.canAssignPermissions) {
        tasks.push(updateTenantAppRolePermissions(props.appId, props.roleId, {
          permissionCodes: assignModel.permissionCodes
        }));
      }

      if (props.canAssignMenus) {
        tasks.push(setAppRolePages(props.appId, props.roleId, {
          pageIds: assignModel.menuIds
        }));
      }

      if (props.canManageDataScope) {
        tasks.push(setAppRoleDataScope(props.appId, props.roleId, {
          dataScope: assignModel.dataScope,
          deptIds: assignModel.dataScope === 2 ? assignModel.deptIds : undefined
        }));
      }

      // 应用级字段权限：将当前已编辑表的数据合并回 groups 后整体保存
      if (props.canAssignPermissions && appFieldPermissionGroups.value.length > 0) {
        let groups = [...appFieldPermissionGroups.value];
        if (fieldPermissionTableKey.value) {
          const curTableKey = fieldPermissionTableKey.value;
          groups = groups.map((g) => {
            if (g.tableKey === curTableKey) {
              return {
                tableKey: g.tableKey,
                fields: fieldPermissionRows.value.map((r) => ({
                  fieldName: r.fieldName,
                  canView: r.canView,
                  canEdit: r.canEdit
                }))
              };
            }
            return g;
          });
        }
        tasks.push(setAppRoleFieldPermissions(props.appId, props.roleId, { groups }));
      }
    } else {
      // 平台模式：使用平台 API
      if (props.canAssignPermissions) {
        tasks.push(updateRolePermissions(props.roleId, { permissionIds: assignModel.permissionIds }));
      }
      
      if (props.canAssignMenus) {
        tasks.push(updateRoleMenus(props.roleId, { menuIds: assignModel.menuIds }));
      }
      
      if (props.canManageDataScope) {
        tasks.push(setRoleDataScope(
          props.roleId,
          assignModel.dataScope,
          assignModel.dataScope === 2 ? assignModel.deptIds : undefined
        ));
      }
    }
    
    if (fieldPermissionTableKey.value && props.roleCode && !isAppScope.value) {
      const merged = existingFieldPermissions.value
        .filter((item) => item.roleCode !== props.roleCode)
        .concat(
          fieldPermissionRows.value
            .filter((item) => item.canView || item.canEdit)
            .map((item) => ({
              fieldName: item.fieldName,
              roleCode: props.roleCode,
              canView: item.canView,
              canEdit: item.canEdit
            }))
        );
      tasks.push(setDynamicFieldPermissions(fieldPermissionTableKey.value, { permissions: merged }));
    }
    
    await Promise.all(tasks);
    if (!isMounted.value) return;
    message.success(t("systemRoles.assignSaveSuccess"));
    emit('success');
  } catch (error) {
    if (isMounted.value) {
      message.error((error as Error).message || t("systemRoles.assignSaveFailed"));
    }
  } finally {
    if (isMounted.value) {
      submitting.value = false;
    }
  }
};

watch(() => props.roleId, (newId) => {
  if (newId) {
    fetchRoleDetail();
  }
}, { immediate: true });

watch(() => assignModel.dataScope, (scope) => {
  if (scope !== 2) {
    assignModel.deptIds = [];
  }
});
</script>

<style scoped>
.role-assign-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.panel-header {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid var(--color-border);
  background-color: var(--color-bg-container);
}

.panel-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--color-text-primary);
  display: flex;
  align-items: center;
  gap: 8px;
}

.role-code {
  font-size: 13px;
  font-weight: normal;
  color: var(--color-text-tertiary);
  padding: 2px 6px;
  border-radius: 4px;
  background-color: var(--color-bg-layout);
}

.panel-content {
  flex: 1;
  padding: 0 20px 20px;
  overflow-y: auto;
}

.table-container {
  border: 1px solid var(--color-border);
  border-radius: 6px;
  overflow: hidden;
}

:deep(.ant-tabs-nav) {
  margin-bottom: 16px !important;
}

:deep(.display-block-radio .ant-radio-button-wrapper) {
  height: auto;
  padding: 12px 16px;
}

:deep(.display-block-radio .ant-radio-button-wrapper-checked) {
  background-color: var(--color-primary-bg) !important;
}

.scope-radio-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
  line-height: 1.4;
}

.scope-label {
  font-weight: 600;
  font-size: 14px;
}

.scope-desc {
  font-size: 12px;
  color: var(--color-text-secondary);
}
</style>

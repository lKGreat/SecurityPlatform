<template>
  <div class="role-assign-panel">
    <div class="panel-header">
      <div class="panel-title">
        {{ t("systemRoles.assignPanelTitle", { roleName, roleCode }) }}
      </div>
      <a-button type="primary" :loading="submitting" @click="submitAssign">{{ t("systemRoles.saveAssign") }}</a-button>
    </div>

    <div class="panel-content">
      <a-spin :spinning="loading">
        <a-tabs v-model:activeKey="activeTab">
          <a-tab-pane v-if="canAssignPermissions" key="permissions" :tab="t('systemRoles.permissionTab')">
            <a-alert
              :message="t('systemRoles.permissionTabHint')"
              type="info"
              show-icon
              style="margin-bottom: 12px"
            />
            <a-select
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
            />
          </a-tab-pane>
          
          <a-tab-pane v-if="canAssignMenus" key="menus" :tab="t('systemRoles.menuTab')">
            <a-alert
              :message="t('systemRoles.menuTabHint')"
              type="info"
              show-icon
              style="margin-bottom: 12px"
            />
            <a-select
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
                <a-table-column key="canView" :title="t('systemRoles.fieldColumnCanView')" width="80" align="center">
                  <template #default="{ record }">
                    <a-switch
                      :checked="record.canView"
                      size="small"
                      @change="(value: boolean) => handleFieldViewChange(record.fieldName, value)"
                    />
                  </template>
                </a-table-column>
                <a-table-column key="canEdit" :title="t('systemRoles.fieldColumnCanEdit')" width="80" align="center">
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
              <a-select
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
  getMenusPaged,
  getDepartmentsPaged
} from '@/services/api';
import {
  getDynamicTablesPaged,
  getDynamicTableFields,
  getDynamicFieldPermissions,
  setDynamicFieldPermissions
} from '@/services/dynamic-tables';
import { debounce, type SelectOption } from '@/utils/common';
import { getProjectScopeEnabled } from "@/utils/auth";

const { t } = useI18n();

const props = defineProps<{
  roleId: string | null;
  roleCode: string;
  roleName: string;
  canAssignPermissions: boolean;
  canAssignMenus: boolean;
  canManageDataScope: boolean;
}>();

const emit = defineEmits<{
  (e: 'success'): void;
}>();

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
  menuIds: [] as number[],
  dataScope: 0 as number,
  deptIds: [] as number[]
});

const permissionOptions = ref<SelectOption[]>([]);
const menuOptions = ref<SelectOption[]>([]);
const departmentOptions = ref<SelectOption[]>([]);
const permissionLoading = ref(false);
const menuLoading = ref(false);
const departmentLoading = ref(false);

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
    const result = await getPermissionsPaged({
      pageIndex: 1,
      pageSize: 20,
      keyword: keyword?.trim() || undefined
    });
    if (!isMounted.value) return;
    permissionOptions.value = result.items.map((item) => ({
      label: `${item.name} (${item.code})`,
      value: Number(item.id)
    }));
  } catch (error) {
    if (isMounted.value) {
      message.error((error as Error).message || "加载权限失败");
    }
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
    const result = await getMenusPaged({
      pageIndex: 1,
      pageSize: 20,
      keyword: keyword?.trim() || undefined
    });
    if (!isMounted.value) return;
    menuOptions.value = result.items.map((item) => ({
      label: `${item.name} (${item.path})`,
      value: Number(item.id)
    }));
  } catch (error) {
    if (isMounted.value) {
      message.error((error as Error).message || "加载菜单失败");
    }
  } finally {
    if (isMounted.value) {
      menuLoading.value = false;
    }
  }
};

const handlePermissionSearch = debounce((value: string) => void loadPermissionOptions(value));
const handleMenuSearch = debounce((value: string) => void loadMenuOptions(value));

const loadDepartmentOptions = async (keyword?: string) => {
  if (!isMounted.value) return;
  departmentLoading.value = true;
  try {
    const result = await getDepartmentsPaged({
      pageIndex: 1,
      pageSize: 20,
      keyword: keyword?.trim() || undefined
    });
    if (!isMounted.value) return;
    departmentOptions.value = result.items.map((item) => ({
      label: `${item.name} (${item.code})`,
      value: Number(item.id)
    }));
  } catch (error) {
    if (isMounted.value) {
      message.error((error as Error).message || "加载部门失败");
    }
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
      pageSize: 20,
      keyword: search?.trim() || undefined
    });
    if (!isMounted.value) return;
    dynamicTableOptions.value = result.items.map((item) => ({
      label: `${item.displayName} (${item.tableKey})`,
      value: item.tableKey
    }));
  } catch (error) {
    if (isMounted.value) {
      message.error((error as Error).message || "加载动态表失败");
    }
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
  void loadFieldPermissions(value);
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
    // 兼容历史值：CurrentTenant(1) 在前端统一映射为“全部数据(0)”展示。
    assignModel.dataScope = detail.dataScope === 1 ? 0 : (detail.dataScope ?? 0);
    assignModel.deptIds = (detail.deptIds ?? [])
      .map((value) => Number(value))
      .filter((value) => Number.isFinite(value));
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
    
    if (fieldPermissionTableKey.value && props.roleCode) {
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

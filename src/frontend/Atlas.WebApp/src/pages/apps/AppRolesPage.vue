<template>
  <div class="app-roles-page">
    <a-page-header :title="t('appsRoles.pageTitle')" :sub-title="t('appsRoles.pageSubtitle')">
      <template #extra>
        <a-space>
          <a-input-search
            v-model:value="keyword"
            style="width: 240px"
            :placeholder="t('appsRoles.searchPlaceholder')"
            allow-clear
            @search="handleSearch"
          />
          <a-button @click="handleRefresh">{{ t("commonUi.refresh") }}</a-button>
          <a-button v-if="canManageRoles" type="primary" @click="openCreateModal">{{ t("appsRoles.newRole") }}</a-button>
        </a-space>
      </template>
    </a-page-header>

    <a-row :gutter="[12, 12]" class="summary-row">
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic :title="t('appsRoles.statTotal')" :value="overview?.totalRoles ?? 0" /></a-card>
      </a-col>
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic :title="t('appsRoles.statSystem')" :value="overview?.systemRoleCount ?? 0" /></a-card>
      </a-col>
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic :title="t('appsRoles.statCustom')" :value="overview?.customRoleCount ?? 0" /></a-card>
      </a-col>
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic :title="t('appsRoles.statMembers')" :value="overview?.totalMembers ?? 0" /></a-card>
      </a-col>
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic :title="t('appsRoles.statCovered')" :value="overview?.coveredMembers ?? 0" /></a-card>
      </a-col>
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic :title="t('appsRoles.statCoverage')" :value="coverageRateText" /></a-card>
      </a-col>
    </a-row>

    <div class="content-area mt12">
      <MasterDetailLayout :detail-visible="!!selectedRole" :master-width="680">
        <template #master>
          <a-table
            row-key="id"
            :columns="columns"
            :data-source="rows"
            :loading="loading"
            :pagination="pagination"
            :custom-row="(record: TenantAppRoleListItem) => ({ onClick: () => selectRole(record) })"
            :row-class-name="(record: TenantAppRoleListItem) => record.id === selectedRole?.id ? 'selected-row' : ''"
            @change="handleTableChange"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'isSystem'">
                <a-tag :color="record.isSystem ? 'purple' : 'blue'">
                  {{ record.isSystem ? t("appsRoles.typeSystem") : t("appsRoles.typeCustom") }}
                </a-tag>
              </template>
              <template v-else-if="column.key === 'permissionCoverage'">
                <a-tag :color="record.permissionCodes.length > 0 ? 'green' : 'warning'">
                  {{ record.permissionCodes.length > 0 ? t("appsRoles.permCovered") : t("appsRoles.permMissing") }}
                </a-tag>
              </template>
              <template v-else-if="column.key === 'actions'">
                <a-space v-if="canManageRoles" @click.stop>
                  <a-button type="link" size="small" @click="openEditModal(record)">{{ t("common.edit") }}</a-button>
                  <a-popconfirm
                    v-if="!record.isSystem"
                    :title="t('appsRoles.deleteConfirm')"
                    :ok-text="t('common.delete')"
                    :cancel-text="t('common.cancel')"
                    @confirm="removeRole(record.id)"
                  >
                    <a-button type="link" size="small" danger>{{ t("common.delete") }}</a-button>
                  </a-popconfirm>
                </a-space>
                <span v-else class="placeholder">-</span>
              </template>
            </template>
          </a-table>
        </template>
        <template #detail>
          <RoleAssignPanel
            v-if="selectedRole"
            :role-id="selectedRole.id"
            :role-code="selectedRole.code"
            :role-name="selectedRole.name"
            :can-assign-permissions="canManageRoles"
            :can-assign-menus="canManageRoles"
            :can-manage-data-scope="canManageRoles"
            scope="app"
            :app-id="appId"
            @success="handleAssignSuccess"
          />
          <div v-else class="detail-placeholder">
            <a-empty :description="t('appsRoles.emptySelectRole')" />
          </div>
        </template>
      </MasterDetailLayout>
    </div>

    <a-modal
      v-model:open="createModalOpen"
      :title="t('appsRoles.modalCreateTitle')"
      :confirm-loading="submitting"
      :ok-text="t('appsRoles.okCreate')"
      :cancel-text="t('common.cancel')"
      @ok="submitCreate"
    >
      <a-form layout="vertical">
        <a-form-item :label="t('appsRoles.labelCode')" required>
          <a-input v-model:value="createForm.code" maxlength="64" :placeholder="t('appsRoles.codePlaceholder')" />
        </a-form-item>
        <a-form-item :label="t('appsRoles.labelName')" required>
          <a-input v-model:value="createForm.name" maxlength="64" :placeholder="t('appsRoles.namePlaceholder')" />
        </a-form-item>
        <a-form-item :label="t('appsRoles.labelDesc')">
          <a-input v-model:value="createForm.description" maxlength="200" :placeholder="t('appsRoles.descPlaceholder')" />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal
      v-model:open="editModalOpen"
      :title="t('appsRoles.modalEditTitle')"
      :confirm-loading="submitting"
      :ok-text="t('common.save')"
      :cancel-text="t('common.cancel')"
      @ok="submitEdit"
    >
      <a-form layout="vertical">
        <a-form-item :label="t('appsRoles.labelCode')">
          <a-input :value="editingRoleCode" disabled />
        </a-form-item>
        <a-form-item :label="t('appsRoles.labelName')" required>
          <a-input v-model:value="editForm.name" maxlength="64" />
        </a-form-item>
        <a-form-item :label="t('appsRoles.labelDesc')">
          <a-input v-model:value="editForm.description" maxlength="200" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";
import type { TableColumnsType, TablePaginationConfig } from "ant-design-vue";
import { message } from "ant-design-vue";
import { useRoute } from "vue-router";
import { isAdminRole } from "@/utils/auth";
import { useUserStore } from "@/stores/user";
import MasterDetailLayout from "@/components/layout/MasterDetailLayout.vue";
import RoleAssignPanel from "@/components/system/roles/RoleAssignPanel.vue";
import {
  createTenantAppRole,
  deleteTenantAppRole,
  getTenantAppRoleDetail,
  getTenantAppRoleGovernanceOverview,
  getTenantAppRolesPaged,
  updateTenantAppRole
} from "@/services/api-app-members";
import type {
  TenantAppRoleGovernanceOverview,
  TenantAppRoleListItem
} from "@/types/platform-v2";

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

const { t } = useI18n();
const route = useRoute();
const userStore = useUserStore();
const appId = computed(() => String(route.params.appId ?? ""));

const loading = ref(false);
const submitting = ref(false);
const keyword = ref("");
const rows = ref<TenantAppRoleListItem[]>([]);
const overview = ref<TenantAppRoleGovernanceOverview | null>(null);
const selectedRole = ref<TenantAppRoleListItem | null>(null);
const pagination = reactive<TablePaginationConfig>({
  current: 1,
  pageSize: 10,
  total: 0,
  showSizeChanger: true,
  showTotal: (total) => t("crud.totalItems", { total })
});

const createModalOpen = ref(false);
const editModalOpen = ref(false);
const editingRoleId = ref<string>("");
const editingRoleCode = ref<string>("");

const createForm = reactive({
  code: "",
  name: "",
  description: ""
});

const editForm = reactive({
  name: "",
  description: ""
});

const canManageRoles = computed(() => {
  return userStore.permissions.includes("apps:roles:update") || isAdminRole(userStore.profile);
});

const coverageRateText = computed(() => `${((overview.value?.permissionCoverageRate ?? 0) * 100).toFixed(2)}%`);

const columns = computed<TableColumnsType<TenantAppRoleListItem>>(() => [
  { title: t("appsRoles.colCode"), dataIndex: "code", key: "code", width: 160 },
  { title: t("appsRoles.colName"), dataIndex: "name", key: "name", width: 160 },
  { title: t("appsRoles.colType"), key: "isSystem", width: 90 },
  { title: t("appsRoles.colMembers"), dataIndex: "memberCount", key: "memberCount", width: 90 },
  { title: t("appsRoles.colPermCov"), key: "permissionCoverage", width: 110 },
  { title: t("appsRoles.colDescription"), dataIndex: "description", key: "description", ellipsis: true },
  { title: t("appsRoles.colActions"), key: "actions", width: 120, fixed: "right" }
]);

function selectRole(record: TenantAppRoleListItem) {
  selectedRole.value = selectedRole.value?.id === record.id ? null : record;
}

function handleAssignSuccess() {
  void loadRoles();
}

async function loadRoles() {
  if (!appId.value) return;

  loading.value = true;
  try {
    const [roleResult, overviewResult] = await Promise.all([
      getTenantAppRolesPaged(appId.value, {
        pageIndex: Number(pagination.current ?? 1),
        pageSize: Number(pagination.pageSize ?? 10),
        keyword: keyword.value.trim() || undefined
      }),
      getTenantAppRoleGovernanceOverview(appId.value)
    ]);

    if (!isMounted.value) return;
    rows.value = roleResult.items;
    pagination.total = roleResult.total;
    pagination.current = roleResult.pageIndex;
    pagination.pageSize = roleResult.pageSize;
    overview.value = overviewResult;
  } catch (error) {
    rows.value = [];
    pagination.total = 0;
    overview.value = null;
    message.error((error as Error).message || t("appsRoles.loadRolesFailed"));
  } finally {
    loading.value = false;
  }
}

function handleTableChange(page: TablePaginationConfig) {
  pagination.current = page.current ?? 1;
  pagination.pageSize = page.pageSize ?? 10;
  void loadRoles();
}

function handleSearch() {
  pagination.current = 1;
  void loadRoles();
}

function handleRefresh() {
  void loadRoles();
}

function openCreateModal() {
  createForm.code = "";
  createForm.name = "";
  createForm.description = "";
  createModalOpen.value = true;
}

async function submitCreate() {
  if (!appId.value) return;
  if (!createForm.code.trim() || !createForm.name.trim()) {
    message.warning(t("appsRoles.fillRequired"));
    return;
  }

  submitting.value = true;
  try {
    await createTenantAppRole(appId.value, {
      code: createForm.code.trim(),
      name: createForm.name.trim(),
      description: createForm.description.trim() || undefined,
      permissionCodes: []
    });

    if (!isMounted.value) return;
    message.success(t("appsRoles.createRoleSuccess"));
    createModalOpen.value = false;
    await loadRoles();
  } catch (error) {
    message.error((error as Error).message || t("appsRoles.createRoleFailed"));
  } finally {
    submitting.value = false;
  }
}

async function openEditModal(record: TenantAppRoleListItem) {
  editingRoleId.value = record.id;
  editingRoleCode.value = record.code;
  editModalOpen.value = true;
  try {
    const detail = await getTenantAppRoleDetail(appId.value, record.id);
    if (!isMounted.value) return;
    editForm.name = detail.name;
    editForm.description = detail.description || "";
  } catch (error) {
    editModalOpen.value = false;
    message.error((error as Error).message || t("appsRoles.loadDetailFailed"));
  }
}

async function submitEdit() {
  if (!appId.value || !editingRoleId.value) return;
  if (!editForm.name.trim()) {
    message.warning(t("appsRoles.nameRequired"));
    return;
  }

  submitting.value = true;
  try {
    await updateTenantAppRole(appId.value, editingRoleId.value, {
      name: editForm.name.trim(),
      description: editForm.description.trim() || undefined
    });

    if (!isMounted.value) return;
    message.success(t("appsRoles.updateRoleSuccess"));
    editModalOpen.value = false;
    await loadRoles();
  } catch (error) {
    message.error((error as Error).message || t("appsRoles.updateRoleFailed"));
  } finally {
    submitting.value = false;
  }
}

async function removeRole(roleId: string) {
  if (!appId.value) return;

  try {
    await deleteTenantAppRole(appId.value, roleId);
    if (!isMounted.value) return;
    if (selectedRole.value?.id === roleId) {
      selectedRole.value = null;
    }
    message.success(t("appsRoles.deleteRoleSuccess"));
    await loadRoles();
  } catch (error) {
    message.error((error as Error).message || t("appsRoles.deleteRoleFailed"));
  }
}

onMounted(() => {
  void loadRoles();
});
</script>

<style scoped>
.app-roles-page {
  padding: 8px;
  display: flex;
  flex-direction: column;
  height: 100%;
}

.summary-row {
  margin-top: 12px;
}

.mt12 {
  margin-top: 12px;
}

.content-area {
  flex: 1;
  min-height: 0;
}

.placeholder {
  color: #999;
}

.detail-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  min-height: 300px;
}

:deep(.selected-row) {
  background-color: var(--color-primary-bg);
}
</style>

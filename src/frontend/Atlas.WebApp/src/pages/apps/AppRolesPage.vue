<template>
  <div class="app-roles-page">
    <a-page-header title="应用角色" sub-title="角色治理与权限覆盖分析">
      <template #extra>
        <a-space>
          <a-input-search
            v-model:value="keyword"
            style="width: 240px"
            placeholder="按角色编码/名称搜索"
            allow-clear
            @search="handleSearch"
          />
          <a-button @click="handleRefresh">刷新</a-button>
          <a-button v-if="canManageRoles" type="primary" @click="openCreateModal">新建角色</a-button>
        </a-space>
      </template>
    </a-page-header>

    <a-row :gutter="[12, 12]" class="summary-row">
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic title="角色总数" :value="overview?.totalRoles ?? 0" /></a-card>
      </a-col>
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic title="系统角色" :value="overview?.systemRoleCount ?? 0" /></a-card>
      </a-col>
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic title="自定义角色" :value="overview?.customRoleCount ?? 0" /></a-card>
      </a-col>
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic title="成员总数" :value="overview?.totalMembers ?? 0" /></a-card>
      </a-col>
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic title="已覆盖成员" :value="overview?.coveredMembers ?? 0" /></a-card>
      </a-col>
      <a-col :xs="24" :md="8" :xl="4">
        <a-card><a-statistic title="覆盖率" :value="coverageRateText" /></a-card>
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
                  {{ record.isSystem ? "系统" : "自定义" }}
                </a-tag>
              </template>
              <template v-else-if="column.key === 'permissionCoverage'">
                <a-tag :color="record.permissionCodes.length > 0 ? 'green' : 'warning'">
                  {{ record.permissionCodes.length > 0 ? "已覆盖" : "缺失" }}
                </a-tag>
              </template>
              <template v-else-if="column.key === 'actions'">
                <a-space v-if="canManageRoles" @click.stop>
                  <a-button type="link" size="small" @click="openEditModal(record)">编辑</a-button>
                  <a-popconfirm
                    v-if="!record.isSystem"
                    title="确认删除该角色？"
                    ok-text="删除"
                    cancel-text="取消"
                    @confirm="removeRole(record.id)"
                  >
                    <a-button type="link" size="small" danger>删除</a-button>
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
            <a-empty description="请选择左侧角色以配置权限" />
          </div>
        </template>
      </MasterDetailLayout>
    </div>

    <!-- 新建角色 Modal -->
    <a-modal
      v-model:open="createModalOpen"
      title="新建应用角色"
      :confirm-loading="submitting"
      ok-text="创建"
      cancel-text="取消"
      @ok="submitCreate"
    >
      <a-form layout="vertical">
        <a-form-item label="角色编码" required>
          <a-input v-model:value="createForm.code" maxlength="64" placeholder="如 APP_EDITOR" />
        </a-form-item>
        <a-form-item label="角色名称" required>
          <a-input v-model:value="createForm.name" maxlength="64" placeholder="请输入角色名称" />
        </a-form-item>
        <a-form-item label="角色描述">
          <a-input v-model:value="createForm.description" maxlength="200" placeholder="请输入角色描述" />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- 编辑角色 Modal -->
    <a-modal
      v-model:open="editModalOpen"
      title="编辑应用角色"
      :confirm-loading="submitting"
      ok-text="保存"
      cancel-text="取消"
      @ok="submitEdit"
    >
      <a-form layout="vertical">
        <a-form-item label="角色编码">
          <a-input :value="editingRoleCode" disabled />
        </a-form-item>
        <a-form-item label="角色名称" required>
          <a-input v-model:value="editForm.name" maxlength="64" />
        </a-form-item>
        <a-form-item label="角色描述">
          <a-input v-model:value="editForm.description" maxlength="200" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, onUnmounted } from "vue";
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
  showTotal: (total) => `共 ${total} 条`
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
  { title: "角色编码", dataIndex: "code", key: "code", width: 160 },
  { title: "角色名称", dataIndex: "name", key: "name", width: 160 },
  { title: "类型", key: "isSystem", width: 90 },
  { title: "成员数", dataIndex: "memberCount", key: "memberCount", width: 90 },
  { title: "权限覆盖", key: "permissionCoverage", width: 110 },
  { title: "描述", dataIndex: "description", key: "description", ellipsis: true },
  { title: "操作", key: "actions", width: 120, fixed: "right" }
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
    message.error((error as Error).message || "加载应用角色失败");
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
    message.warning("请填写角色编码与角色名称");
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
    message.success("创建应用角色成功");
    createModalOpen.value = false;
    await loadRoles();
  } catch (error) {
    message.error((error as Error).message || "创建应用角色失败");
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
    message.error((error as Error).message || "加载角色详情失败");
  }
}

async function submitEdit() {
  if (!appId.value || !editingRoleId.value) return;
  if (!editForm.name.trim()) {
    message.warning("角色名称不能为空");
    return;
  }

  submitting.value = true;
  try {
    await updateTenantAppRole(appId.value, editingRoleId.value, {
      name: editForm.name.trim(),
      description: editForm.description.trim() || undefined
    });

    if (!isMounted.value) return;
    message.success("角色信息已更新");
    editModalOpen.value = false;
    await loadRoles();
  } catch (error) {
    message.error((error as Error).message || "更新角色失败");
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
    message.success("角色已删除");
    await loadRoles();
  } catch (error) {
    message.error((error as Error).message || "删除角色失败");
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

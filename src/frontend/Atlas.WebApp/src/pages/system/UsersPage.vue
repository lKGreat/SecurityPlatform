<template>
  <CrudPageLayout
    title="员工管理"
    v-model:keyword="keyword"
    search-placeholder="搜索用户名/姓名/邮箱"
    :drawer-open="formVisible"
    :drawer-title="formMode === 'create' ? '新增员工' : '编辑员工'"
    :drawer-width="640"
    @update:drawer-open="formVisible = $event"
    @search="handleSearch"
    @reset="resetFilters"
    @close-form="closeForm"
    @submit="handleSubmit"
  >
    <template #toolbar-actions>
      <a-button v-if="canCreate" type="primary" @click="handleOpenCreate">新增员工</a-button>
    </template>
    <template #toolbar-right>
      <TableViewToolbar :controller="tableViewController" />
    </template>

    <template #table>
      <a-table
        :columns="tableColumns"
        :data-source="dataSource"
        :pagination="pagination"
        :loading="loading"
        :size="tableSize"
        row-key="id"
        @change="onTableChange"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <a-tag :color="record.isActive ? 'green' : 'red'">
              {{ record.isActive ? "启用" : "停用" }}
            </a-tag>
          </template>
          <template v-else-if="column.key === 'actions'">
            <a-space>
              <a-button v-if="canUpdate || canAssignRoles || canAssignDepartments || canAssignPositions" type="link" @click="handleOpenEdit(record.id)">编辑</a-button>
              <a-popconfirm
                v-if="canDelete"
                title="确认删除该员工？"
                ok-text="删除"
                cancel-text="取消"
                @confirm="handleDelete(record.id)"
              >
                <a-button type="link" danger>删除</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </template>

    <template #form>
      <a-tabs v-model:activeKey="activeTab">
        <a-tab-pane key="basic" tab="基本信息">
          <a-form ref="formRef" :model="formModel" :rules="formRules" layout="vertical">
            <a-form-item v-if="formMode === 'create'" label="用户名" name="username">
              <a-input v-model:value="formModel.username" />
            </a-form-item>
            <a-form-item v-if="formMode === 'create'" label="密码" name="password">
              <a-input-password v-model:value="formModel.password" />
            </a-form-item>
            <a-form-item label="姓名" name="displayName">
              <a-input v-model:value="formModel.displayName" />
            </a-form-item>
            <a-form-item label="邮箱" name="email">
              <a-input v-model:value="formModel.email" />
            </a-form-item>
            <a-form-item label="手机号" name="phoneNumber">
              <a-input v-model:value="formModel.phoneNumber" />
            </a-form-item>
            <a-form-item label="状态" name="isActive">
              <a-switch v-model:checked="formModel.isActive" />
            </a-form-item>
          </a-form>
        </a-tab-pane>
        <a-tab-pane v-if="canAssignRoles" key="roles" tab="角色">
          <a-form layout="vertical">
            <a-form-item label="角色">
              <a-select
                v-model:value="formModel.roleIds"
                mode="multiple"
                placeholder="选择角色"
                :options="roleOptions"
                :loading="roleLoading"
                :filter-option="false"
                show-search
                @search="handleRoleSearch"
                @focus="() => loadRoleOptions()"
              />
            </a-form-item>
          </a-form>
        </a-tab-pane>
        <a-tab-pane v-if="canAssignDepartments" key="departments" tab="部门">
          <a-form layout="vertical">
            <a-form-item label="部门">
              <a-select
                v-model:value="formModel.departmentIds"
                mode="multiple"
                placeholder="选择部门"
                :options="departmentOptions"
                :loading="departmentLoading"
                :filter-option="false"
                show-search
                @search="handleDepartmentSearch"
                @focus="() => loadDepartmentOptions()"
              />
            </a-form-item>
          </a-form>
        </a-tab-pane>
        <a-tab-pane v-if="canAssignPositions" key="positions" tab="职位">
          <a-form layout="vertical">
            <a-form-item label="职位">
              <a-select
                v-model:value="formModel.positionIds"
                mode="multiple"
                placeholder="选择职位"
                :options="positionOptions"
                :loading="positionLoading"
                :filter-option="false"
                show-search
                @search="handlePositionSearch"
                @focus="() => loadPositionOptions()"
              />
            </a-form-item>
          </a-form>
        </a-tab-pane>
      </a-tabs>
    </template>
  </CrudPageLayout>
</template>

<script setup lang="ts">
import { ref } from "vue";
import type { FormInstance } from "ant-design-vue";
import { message } from "ant-design-vue";
import CrudPageLayout from "@/components/crud/CrudPageLayout.vue";
import TableViewToolbar from "@/components/table/table-view-toolbar.vue";
import { useCrudPage } from "@/composables/useCrudPage";
import {
  createUser,
  deleteUser,
  getDepartmentsPaged,
  getPositionsPaged,
  getRolesPaged,
  getUserDetail,
  getUsersPaged,
  updateUser,
  updateUserDepartments,
  updateUserPositions,
  updateUserRoles
} from "@/services/api";
import type {
  DepartmentListItem,
  RoleListItem,
  UserDetail,
  UserListItem,
  UserCreateRequest,
  UserUpdateRequest,
  PositionListItem
} from "@/types/api";
import { debounce, type SelectOption } from "@/utils/common";

const activeTab = ref("basic");

// Select options
const roleOptions = ref<SelectOption[]>([]);
const departmentOptions = ref<SelectOption[]>([]);
const positionOptions = ref<SelectOption[]>([]);
const roleLoading = ref(false);
const departmentLoading = ref(false);
const positionLoading = ref(false);

const loadRoleOptions = async (keyword?: string) => {
  roleLoading.value = true;
  try {
    const result = await getRolesPaged({
      pageIndex: 1,
      pageSize: 20,
      keyword: keyword?.trim() || undefined
    });
    roleOptions.value = result.items.map((role: RoleListItem) => ({
      label: `${role.name} (${role.code})`,
      value: Number(role.id)
    }));
  } catch (error) {
    message.error((error as Error).message || "加载角色失败");
  } finally {
    roleLoading.value = false;
  }
};

const loadDepartmentOptions = async (keyword?: string) => {
  departmentLoading.value = true;
  try {
    const result = await getDepartmentsPaged({
      pageIndex: 1,
      pageSize: 20,
      keyword: keyword?.trim() || undefined
    });
    departmentOptions.value = result.items.map((dept: DepartmentListItem) => ({
      label: dept.name,
      value: Number(dept.id)
    }));
  } catch (error) {
    message.error((error as Error).message || "加载部门失败");
  } finally {
    departmentLoading.value = false;
  }
};

const loadPositionOptions = async (keyword?: string) => {
  positionLoading.value = true;
  try {
    const result = await getPositionsPaged({
      pageIndex: 1,
      pageSize: 20,
      keyword: keyword?.trim() || undefined
    });
    positionOptions.value = result.items.map((position: PositionListItem) => ({
      label: `${position.name} (${position.code})`,
      value: Number(position.id)
    }));
  } catch (error) {
    message.error((error as Error).message || "加载职位失败");
  } finally {
    positionLoading.value = false;
  }
};

const handleRoleSearch = debounce((value: string) => {
  void loadRoleOptions(value);
});

const handleDepartmentSearch = debounce((value: string) => {
  void loadDepartmentOptions(value);
});

const handlePositionSearch = debounce((value: string) => {
  void loadPositionOptions(value);
});

const formRef = ref<FormInstance>();

const crud = useCrudPage<UserListItem, UserDetail, UserCreateRequest, UserUpdateRequest>({
  tableKey: "system.users",
  columns: [
    { title: "用户名", dataIndex: "username", key: "username" },
    { title: "姓名", dataIndex: "displayName", key: "displayName" },
    { title: "邮箱", dataIndex: "email", key: "email" },
    { title: "手机号", dataIndex: "phoneNumber", key: "phoneNumber" },
    { title: "状态", dataIndex: "isActive", key: "status" },
    { title: "最近登录", dataIndex: "lastLoginAt", key: "lastLoginAt" },
    { title: "操作", key: "actions", view: { canHide: false } }
  ],
  permissions: {
    create: "users:create",
    update: "users:update",
    delete: "users:delete",
    assignRoles: "users:assign-roles",
    assignDepartments: "users:assign-departments",
    assignPositions: "users:assign-positions"
  },
  api: {
    list: getUsersPaged,
    detail: getUserDetail,
    create: createUser,
    update: updateUser,
    delete: deleteUser
  },
  formRef,
  defaultFormModel: () => ({
    username: "",
    password: "",
    displayName: "",
    email: "",
    phoneNumber: "",
    isActive: true,
    roleIds: [],
    departmentIds: [],
    positionIds: []
  }),
  formRules: {
    username: [{ required: true, message: "请输入用户名" }],
    password: [{ required: true, message: "请输入密码" }],
    displayName: [{ required: true, message: "请输入姓名" }]
  },
  buildCreatePayload: (model) => ({
    username: model.username,
    password: model.password,
    displayName: model.displayName,
    email: model.email || undefined,
    phoneNumber: model.phoneNumber || undefined,
    isActive: model.isActive,
    roleIds: model.roleIds,
    departmentIds: model.departmentIds,
    positionIds: model.positionIds
  }),
  buildUpdatePayload: (model) => ({
    displayName: model.displayName,
    email: model.email || undefined,
    phoneNumber: model.phoneNumber || undefined,
    isActive: model.isActive
  }),
  mapDetailToForm: (detail, model) => {
    (model as any).username = detail.username;
    model.displayName = detail.displayName;
    (model as any).email = detail.email ?? "";
    (model as any).phoneNumber = detail.phoneNumber ?? "";
    model.isActive = detail.isActive;
    (model as any).roleIds = detail.roleIds.slice();
    (model as any).departmentIds = detail.departmentIds.slice();
    (model as any).positionIds = detail.positionIds.slice();
  },
  autoFetch: true
});

const {
  dataSource, loading, keyword, pagination,
  formVisible, formMode, formModel, formRules,
  selectedId,
  tableViewController, tableColumns, tableSize,
  canCreate, canUpdate, canDelete,
  onTableChange, handleSearch, resetFilters,
  closeForm, handleDelete, fetchData
} = crud;

const canAssignRoles = crud.hasPermissionFor("assignRoles");
const canAssignDepartments = crud.hasPermissionFor("assignDepartments");
const canAssignPositions = crud.hasPermissionFor("assignPositions");

const handleOpenCreate = () => {
  activeTab.value = "basic";
  crud.openCreate();
  void loadRoleOptions();
  void loadDepartmentOptions();
  void loadPositionOptions();
};

const handleOpenEdit = async (id: string) => {
  activeTab.value = "basic";
  void loadRoleOptions();
  void loadDepartmentOptions();
  void loadPositionOptions();
  await crud.openEdit(id);
};

const handleSubmit = async () => {
  if (formMode.value === "create") {
    await crud.submitForm();
  } else if (selectedId.value) {
    // Update basic info
    try {
      await updateUser(selectedId.value, {
        displayName: formModel.displayName,
        email: (formModel as any).email || undefined,
        phoneNumber: (formModel as any).phoneNumber || undefined,
        isActive: formModel.isActive
      });

      // Update assignments in parallel
      const promises: Promise<void>[] = [];
      if (canAssignRoles) {
        promises.push(updateUserRoles(selectedId.value, { roleIds: (formModel as any).roleIds }));
      }
      if (canAssignDepartments) {
        promises.push(updateUserDepartments(selectedId.value, { departmentIds: (formModel as any).departmentIds }));
      }
      if (canAssignPositions) {
        promises.push(updateUserPositions(selectedId.value, { positionIds: (formModel as any).positionIds }));
      }
      if (promises.length) {
        await Promise.all(promises);
      }

      message.success("更新成功");
      formVisible.value = false;
      fetchData();
    } catch (error) {
      message.error((error as Error).message || "更新失败");
    }
  }
};
</script>

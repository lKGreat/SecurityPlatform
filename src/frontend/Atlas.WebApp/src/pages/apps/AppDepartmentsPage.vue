<template>
  <div class="app-org-page">
    <a-page-header title="应用部门" sub-title="管理此应用内的部门组织架构（独立于平台级部门）" />
    <a-card class="mt12">
      <template #extra>
        <a-space>
          <a-input-search
            v-model:value="keyword"
            style="width: 220px"
            placeholder="搜索部门名称/编码"
            allow-clear
            @search="handleSearch"
          />
          <a-button @click="loadData">刷新</a-button>
          <a-button type="primary" @click="openCreateModal">新建部门</a-button>
        </a-space>
      </template>
      <a-table row-key="id" :columns="columns" :data-source="rows" :loading="loading" :pagination="pagination" @change="handleTableChange">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'parentId'">
            <span>{{ record.parentId ? deptMap[record.parentId]?.name ?? record.parentId : '-' }}</span>
          </template>
          <template v-else-if="column.key === 'actions'">
            <a-space>
              <a-button type="link" size="small" @click="openEditModal(record)">编辑</a-button>
              <a-popconfirm title="确认删除该部门？" ok-text="删除" cancel-text="取消" @confirm="removeItem(record.id)">
                <a-button type="link" size="small" danger>删除</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal v-model:open="modalOpen" :title="modalTitle" :confirm-loading="submitting" ok-text="保存" cancel-text="取消" @ok="submitForm">
      <a-form layout="vertical">
        <a-form-item label="部门名称" required><a-input v-model:value="form.name" maxlength="64" /></a-form-item>
        <a-form-item label="部门编码" required><a-input v-model:value="form.code" maxlength="64" /></a-form-item>
        <a-form-item label="上级部门">
          <a-select v-model:value="form.parentId" allow-clear placeholder="选择上级部门（可为空）" show-search :filter-option="(input: string, option: { label?: string | number }) => option?.label?.toString().toLowerCase().includes(input.toLowerCase()) ?? false" :options="parentOptions" />
        </a-form-item>
        <a-form-item label="排序">
          <a-input-number v-model:value="form.sortOrder" :min="0" :max="9999" style="width: 100%" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import type { TableColumnsType, TablePaginationConfig } from "ant-design-vue";
import { message } from "ant-design-vue";
import { useRoute } from "vue-router";
import { getAppDepartmentAll, createAppDepartment, updateAppDepartment, deleteAppDepartment, getAppDepartmentsPaged } from "@/services/api-app-members";
import type { AppDepartmentListItem } from "@/types/platform-v2";

const route = useRoute();
const appId = computed(() => String(route.params.appId ?? ""));
const loading = ref(false);
const submitting = ref(false);
const keyword = ref("");
const rows = ref<AppDepartmentListItem[]>([]);
const allDepts = ref<AppDepartmentListItem[]>([]);
const pagination = reactive<TablePaginationConfig>({ current: 1, pageSize: 20, total: 0, showSizeChanger: true });
const modalOpen = ref(false);
const editingId = ref<string | null>(null);
const form = reactive({ name: "", code: "", parentId: undefined as number | undefined, sortOrder: 0 });

const modalTitle = computed(() => editingId.value ? "编辑部门" : "新建部门");
const deptMap = computed(() => Object.fromEntries(allDepts.value.map(d => [d.id, d])));
const parentOptions = computed(() => allDepts.value
  .filter(d => d.id !== editingId.value)
  .map(d => ({ value: Number(d.id), label: d.name })));

const columns: TableColumnsType<AppDepartmentListItem> = [
  { title: "部门名称", dataIndex: "name", key: "name" },
  { title: "编码", dataIndex: "code", key: "code", width: 140 },
  { title: "上级部门", key: "parentId", width: 140 },
  { title: "排序", dataIndex: "sortOrder", key: "sortOrder", width: 80 },
  { title: "操作", key: "actions", width: 140, fixed: "right" }
];

async function loadData() {
  if (!appId.value) return;
  loading.value = true;
  try {
    const [result, all] = await Promise.all([
      getAppDepartmentsPaged(appId.value, { pageIndex: Number(pagination.current), pageSize: Number(pagination.pageSize), keyword: keyword.value.trim() || undefined }),
      getAppDepartmentAll(appId.value)
    ]);
    rows.value = result.items;
    pagination.total = result.total;
    allDepts.value = all;
  } catch (error) {
    message.error((error as Error).message || "加载失败");
  } finally {
    loading.value = false;
  }
}

function handleSearch() { pagination.current = 1; void loadData(); }
function handleTableChange(page: TablePaginationConfig) { pagination.current = page.current ?? 1; pagination.pageSize = page.pageSize ?? 20; void loadData(); }

function openCreateModal() {
  editingId.value = null;
  form.name = ""; form.code = ""; form.parentId = undefined; form.sortOrder = 0;
  modalOpen.value = true;
}

function openEditModal(record: AppDepartmentListItem) {
  editingId.value = record.id;
  form.name = record.name; form.code = record.code;
  form.parentId = record.parentId ? Number(record.parentId) : undefined;
  form.sortOrder = record.sortOrder;
  modalOpen.value = true;
}

async function submitForm() {
  if (!appId.value || !form.name.trim() || !form.code.trim()) { message.warning("请填写部门名称和编码"); return; }
  submitting.value = true;
  try {
    if (editingId.value) {
      await updateAppDepartment(appId.value, editingId.value, { name: form.name.trim(), code: form.code.trim(), parentId: form.parentId, sortOrder: form.sortOrder });
    } else {
      await createAppDepartment(appId.value, { name: form.name.trim(), code: form.code.trim(), parentId: form.parentId, sortOrder: form.sortOrder });
    }
    message.success("保存成功");
    modalOpen.value = false;
    await loadData();
  } catch (error) {
    message.error((error as Error).message || "保存失败");
  } finally {
    submitting.value = false;
  }
}

async function removeItem(id: string) {
  if (!appId.value) return;
  try {
    await deleteAppDepartment(appId.value, id);
    message.success("删除成功");
    await loadData();
  } catch (error) {
    message.error((error as Error).message || "删除失败");
  }
}

onMounted(() => { void loadData(); });
</script>

<style scoped>
.app-org-page { padding: 8px; }
.mt12 { margin-top: 12px; }
</style>

<template>
  <div class="app-org-page">
    <a-page-header title="应用项目" sub-title="管理此应用内的项目（独立于平台级项目）" />
    <a-card class="mt12">
      <template #extra>
        <a-space>
          <a-input-search v-model:value="keyword" style="width: 220px" placeholder="搜索项目名称/编码" allow-clear @search="handleSearch" />
          <a-button @click="loadData">刷新</a-button>
          <a-button type="primary" @click="openCreateModal">新建项目</a-button>
        </a-space>
      </template>
      <a-table row-key="id" :columns="columns" :data-source="rows" :loading="loading" :pagination="pagination" @change="handleTableChange">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'isActive'">
            <a-tag :color="record.isActive ? 'blue' : 'default'">{{ record.isActive ? '活跃' : '停用' }}</a-tag>
          </template>
          <template v-else-if="column.key === 'actions'">
            <a-space>
              <a-button type="link" size="small" @click="openEditModal(record)">编辑</a-button>
              <a-popconfirm title="确认删除该项目？" ok-text="删除" cancel-text="取消" @confirm="removeItem(record.id)">
                <a-button type="link" size="small" danger>删除</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal v-model:open="modalOpen" :title="modalTitle" :confirm-loading="submitting" ok-text="保存" cancel-text="取消" @ok="submitForm">
      <a-form layout="vertical">
        <a-form-item label="项目名称" required><a-input v-model:value="form.name" maxlength="64" /></a-form-item>
        <a-form-item label="项目编码" required><a-input v-model:value="form.code" maxlength="64" :disabled="!!editingId" /></a-form-item>
        <a-form-item label="描述"><a-textarea v-model:value="form.description" :rows="2" maxlength="300" /></a-form-item>
        <a-form-item label="状态">
          <a-switch v-model:checked="form.isActive" checked-children="活跃" un-checked-children="停用" />
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
import { getAppProjectsPaged, createAppProject, updateAppProject, deleteAppProject } from "@/services/api-app-members";
import type { AppProjectListItem } from "@/types/platform-v2";

const route = useRoute();
const appId = computed(() => String(route.params.appId ?? ""));
const loading = ref(false);
const submitting = ref(false);
const keyword = ref("");
const rows = ref<AppProjectListItem[]>([]);
const pagination = reactive<TablePaginationConfig>({ current: 1, pageSize: 20, total: 0, showSizeChanger: true });
const modalOpen = ref(false);
const editingId = ref<string | null>(null);
const form = reactive({ name: "", code: "", description: "", isActive: true });

const modalTitle = computed(() => editingId.value ? "编辑项目" : "新建项目");

const columns: TableColumnsType<AppProjectListItem> = [
  { title: "项目名称", dataIndex: "name", key: "name" },
  { title: "编码", dataIndex: "code", key: "code", width: 140 },
  { title: "描述", dataIndex: "description", key: "description", ellipsis: true },
  { title: "状态", key: "isActive", width: 100 },
  { title: "操作", key: "actions", width: 140, fixed: "right" }
];

async function loadData() {
  if (!appId.value) return;
  loading.value = true;
  try {
    const result = await getAppProjectsPaged(appId.value, { pageIndex: Number(pagination.current), pageSize: Number(pagination.pageSize), keyword: keyword.value.trim() || undefined });
    rows.value = result.items;
    pagination.total = result.total;
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
  form.name = ""; form.code = ""; form.description = ""; form.isActive = true;
  modalOpen.value = true;
}

function openEditModal(record: AppProjectListItem) {
  editingId.value = record.id;
  form.name = record.name; form.code = record.code;
  form.description = record.description || ""; form.isActive = record.isActive;
  modalOpen.value = true;
}

async function submitForm() {
  if (!appId.value || !form.name.trim() || !form.code.trim()) { message.warning("请填写项目名称和编码"); return; }
  submitting.value = true;
  try {
    if (editingId.value) {
      await updateAppProject(appId.value, editingId.value, { name: form.name.trim(), description: form.description.trim() || undefined, isActive: form.isActive });
    } else {
      await createAppProject(appId.value, { name: form.name.trim(), code: form.code.trim(), description: form.description.trim() || undefined, isActive: form.isActive });
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
    await deleteAppProject(appId.value, id);
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

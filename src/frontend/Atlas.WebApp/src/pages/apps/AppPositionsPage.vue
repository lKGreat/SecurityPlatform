<template>
  <div class="app-org-page">
    <a-page-header title="应用职位" sub-title="管理此应用内的职位（独立于平台级职位）" />
    <a-card class="mt12">
      <template #extra>
        <a-space>
          <a-input-search v-model:value="keyword" style="width: 220px" placeholder="搜索职位名称/编码" allow-clear @search="handleSearch" />
          <a-button @click="loadData">刷新</a-button>
          <a-button type="primary" @click="openCreateModal">新建职位</a-button>
        </a-space>
      </template>
      <a-table row-key="id" :columns="columns" :data-source="rows" :loading="loading" :pagination="pagination" @change="handleTableChange">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'isActive'">
            <a-tag :color="record.isActive ? 'green' : 'default'">{{ record.isActive ? '启用' : '停用' }}</a-tag>
          </template>
          <template v-else-if="column.key === 'actions'">
            <a-space>
              <a-button type="link" size="small" @click="openEditModal(record)">编辑</a-button>
              <a-popconfirm title="确认删除该职位？" ok-text="删除" cancel-text="取消" @confirm="removeItem(record.id)">
                <a-button type="link" size="small" danger>删除</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal v-model:open="modalOpen" :title="modalTitle" :confirm-loading="submitting" ok-text="保存" cancel-text="取消" @ok="submitForm">
      <a-form layout="vertical">
        <a-form-item label="职位名称" required><a-input v-model:value="form.name" maxlength="64" /></a-form-item>
        <a-form-item label="职位编码" required><a-input v-model:value="form.code" maxlength="64" :disabled="!!editingId" /></a-form-item>
        <a-form-item label="描述"><a-textarea v-model:value="form.description" :rows="2" maxlength="200" /></a-form-item>
        <a-form-item label="状态">
          <a-switch v-model:checked="form.isActive" checked-children="启用" un-checked-children="停用" />
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
import { getAppPositionsPaged, createAppPosition, updateAppPosition, deleteAppPosition } from "@/services/api-app-members";
import type { AppPositionListItem } from "@/types/platform-v2";

const route = useRoute();
const appId = computed(() => String(route.params.appId ?? ""));
const loading = ref(false);
const submitting = ref(false);
const keyword = ref("");
const rows = ref<AppPositionListItem[]>([]);
const pagination = reactive<TablePaginationConfig>({ current: 1, pageSize: 20, total: 0, showSizeChanger: true });
const modalOpen = ref(false);
const editingId = ref<string | null>(null);
const form = reactive({ name: "", code: "", description: "", isActive: true, sortOrder: 0 });

const modalTitle = computed(() => editingId.value ? "编辑职位" : "新建职位");

const columns: TableColumnsType<AppPositionListItem> = [
  { title: "职位名称", dataIndex: "name", key: "name" },
  { title: "编码", dataIndex: "code", key: "code", width: 140 },
  { title: "描述", dataIndex: "description", key: "description", ellipsis: true },
  { title: "状态", key: "isActive", width: 90 },
  { title: "排序", dataIndex: "sortOrder", key: "sortOrder", width: 80 },
  { title: "操作", key: "actions", width: 140, fixed: "right" }
];

async function loadData() {
  if (!appId.value) return;
  loading.value = true;
  try {
    const result = await getAppPositionsPaged(appId.value, { pageIndex: Number(pagination.current), pageSize: Number(pagination.pageSize), keyword: keyword.value.trim() || undefined });
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
  form.name = ""; form.code = ""; form.description = ""; form.isActive = true; form.sortOrder = 0;
  modalOpen.value = true;
}

function openEditModal(record: AppPositionListItem) {
  editingId.value = record.id;
  form.name = record.name; form.code = record.code;
  form.description = record.description || ""; form.isActive = record.isActive; form.sortOrder = record.sortOrder;
  modalOpen.value = true;
}

async function submitForm() {
  if (!appId.value || !form.name.trim() || !form.code.trim()) { message.warning("请填写职位名称和编码"); return; }
  submitting.value = true;
  try {
    if (editingId.value) {
      await updateAppPosition(appId.value, editingId.value, { name: form.name.trim(), description: form.description.trim() || undefined, isActive: form.isActive, sortOrder: form.sortOrder });
    } else {
      await createAppPosition(appId.value, { name: form.name.trim(), code: form.code.trim(), description: form.description.trim() || undefined, isActive: form.isActive, sortOrder: form.sortOrder });
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
    await deleteAppPosition(appId.value, id);
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

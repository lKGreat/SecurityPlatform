<template>
  <div class="app-org-page">
    <a-page-header :title="t('appsProjects.pageTitle')" :sub-title="t('appsProjects.pageSubtitle')" />
    <a-card class="mt12">
      <template #extra>
        <a-space>
          <a-input-search
            v-model:value="keyword"
            style="width: 220px"
            :placeholder="t('appsProjects.searchPlaceholder')"
            allow-clear
            @search="handleSearch"
          />
          <a-button @click="loadData">{{ t("commonUi.refresh") }}</a-button>
          <a-button type="primary" @click="openCreateModal">{{ t("appsProjects.newProject") }}</a-button>
        </a-space>
      </template>
      <a-table row-key="id" :columns="columns" :data-source="rows" :loading="loading" :pagination="pagination" @change="handleTableChange">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'isActive'">
            <a-tag :color="record.isActive ? 'blue' : 'default'">
              {{ record.isActive ? t("appsProjects.active") : t("appsProjects.disabled") }}
            </a-tag>
          </template>
          <template v-else-if="column.key === 'actions'">
            <a-space>
              <a-button type="link" size="small" @click="openEditModal(record)">{{ t("common.edit") }}</a-button>
              <a-popconfirm
                :title="t('appsProjects.deleteConfirm')"
                :ok-text="t('common.delete')"
                :cancel-text="t('common.cancel')"
                @confirm="removeItem(record.id)"
              >
                <a-button type="link" size="small" danger>{{ t("common.delete") }}</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal
      v-model:open="modalOpen"
      :title="modalTitle"
      :confirm-loading="submitting"
      :ok-text="t('common.save')"
      :cancel-text="t('common.cancel')"
      @ok="submitForm"
    >
      <a-form layout="vertical">
        <a-form-item :label="t('appsProjects.labelName')" required><a-input v-model:value="form.name" maxlength="64" /></a-form-item>
        <a-form-item :label="t('appsProjects.labelCode')" required><a-input v-model:value="form.code" maxlength="64" :disabled="!!editingId" /></a-form-item>
        <a-form-item :label="t('appsProjects.labelDesc')"><a-textarea v-model:value="form.description" :rows="2" maxlength="300" /></a-form-item>
        <a-form-item :label="t('appsProjects.labelStatus')">
          <a-switch
            v-model:checked="form.isActive"
            :checked-children="t('appsProjects.active')"
            :un-checked-children="t('appsProjects.disabled')"
          />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import type { TableColumnsType, TablePaginationConfig } from "ant-design-vue";
import { message } from "ant-design-vue";
import { useRoute } from "vue-router";
import { getAppProjectsPaged, createAppProject, updateAppProject, deleteAppProject } from "@/services/api-app-members";
import type { AppProjectListItem } from "@/types/platform-v2";

const { t } = useI18n();
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

const modalTitle = computed(() => (editingId.value ? t("appsProjects.modalEdit") : t("appsProjects.modalCreate")));

const columns = computed<TableColumnsType<AppProjectListItem>>(() => [
  { title: t("appsProjects.colName"), dataIndex: "name", key: "name" },
  { title: t("appsProjects.colCode"), dataIndex: "code", key: "code", width: 140 },
  { title: t("appsProjects.colDesc"), dataIndex: "description", key: "description", ellipsis: true },
  { title: t("appsProjects.colStatus"), key: "isActive", width: 100 },
  { title: t("appsProjects.colActions"), key: "actions", width: 140, fixed: "right" }
]);

async function loadData() {
  if (!appId.value) return;
  loading.value = true;
  try {
    const result = await getAppProjectsPaged(appId.value, { pageIndex: Number(pagination.current), pageSize: Number(pagination.pageSize), keyword: keyword.value.trim() || undefined });
    rows.value = result.items;
    pagination.total = result.total;
  } catch (error) {
    message.error((error as Error).message || t("appsProjects.loadFailed"));
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
  if (!appId.value || !form.name.trim() || !form.code.trim()) { message.warning(t("appsProjects.fillNameCode")); return; }
  submitting.value = true;
  try {
    if (editingId.value) {
      await updateAppProject(appId.value, editingId.value, { name: form.name.trim(), description: form.description.trim() || undefined, isActive: form.isActive });
    } else {
      await createAppProject(appId.value, { name: form.name.trim(), code: form.code.trim(), description: form.description.trim() || undefined, isActive: form.isActive });
    }
    message.success(t("appsProjects.saveSuccess"));
    modalOpen.value = false;
    await loadData();
  } catch (error) {
    message.error((error as Error).message || t("appsProjects.saveFailed"));
  } finally {
    submitting.value = false;
  }
}

async function removeItem(id: string) {
  if (!appId.value) return;
  try {
    await deleteAppProject(appId.value, id);
    message.success(t("appsProjects.deleteSuccess"));
    await loadData();
  } catch (error) {
    message.error((error as Error).message || t("appsProjects.deleteFailed"));
  }
}

onMounted(() => { void loadData(); });
</script>

<style scoped>
.app-org-page { padding: 8px; }
.mt12 { margin-top: 12px; }
</style>

<template>
  <a-card :title="t('ai.database.listTitle')" :bordered="false">
    <div class="toolbar">
      <a-space wrap>
        <a-input-search
          v-model:value="keyword"
          :placeholder="t('ai.database.searchPlaceholder')"
          style="width: 260px"
          @search="loadData"
        />
        <a-button @click="handleReset">{{ t("common.reset") }}</a-button>
        <a-button type="primary" @click="openCreate">{{ t("ai.database.newDatabase") }}</a-button>
      </a-space>
    </div>

    <a-table
      row-key="id"
      :columns="columns"
      :data-source="list"
      :loading="loading"
      :pagination="false"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'botId'">
          <a-tag v-if="record.botId" color="blue">Bot {{ record.botId }}</a-tag>
          <span v-else>-</span>
        </template>
        <template v-if="column.key === 'action'">
          <a-space>
            <a-button type="link" @click="goDetail(record.id)">{{ t("ai.plugin.detail") }}</a-button>
            <a-button type="link" @click="openEdit(record.id)">{{ t("common.edit") }}</a-button>
            <a-button type="link" @click="openImport(record.id)">{{ t("common.import") }}</a-button>
            <a-popconfirm :title="t('ai.database.deleteConfirm')" @confirm="handleDelete(record.id)">
              <a-button type="link" danger>{{ t("common.delete") }}</a-button>
            </a-popconfirm>
          </a-space>
        </template>
      </template>
    </a-table>

    <div class="pager">
      <a-pagination
        v-model:current="pageIndex"
        v-model:page-size="pageSize"
        :total="total"
        show-size-changer
        :page-size-options="['10', '20', '50']"
        @change="loadData"
      />
    </div>

    <a-modal
      v-model:open="modalVisible"
      :title="editingId ? t('ai.database.modalEdit') : t('ai.database.modalCreate')"
      :confirm-loading="modalLoading"
      width="760px"
      @ok="submitForm"
      @cancel="closeModal"
    >
      <a-form ref="formRef" :model="form" layout="vertical" :rules="rules">
        <a-form-item :label="t('ai.promptLib.colName')" name="name">
          <a-input v-model:value="form.name" />
        </a-form-item>
        <a-form-item :label="t('ai.promptLib.labelDescription')" name="description">
          <a-textarea v-model:value="form.description" :rows="3" />
        </a-form-item>
        <a-form-item :label="t('ai.database.labelBotId')" name="botId">
          <a-input-number v-model:value="form.botId" :min="1" style="width: 100%" />
        </a-form-item>
        <a-form-item :label="t('ai.database.labelSchema')" name="tableSchema">
          <a-textarea
            v-model:value="form.tableSchema"
            :rows="8"
            :placeholder="t('ai.database.schemaPlaceholder')"
          />
        </a-form-item>
      </a-form>
    </a-modal>

    <database-import-modal
      :open="importOpen"
      :database-id="importDatabaseId"
      @cancel="closeImport"
      @success="handleImportSuccess"
    />
  </a-card>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import type { FormInstance } from "ant-design-vue";
import { message } from "ant-design-vue";
import { useRouter } from "vue-router";
import DatabaseImportModal from "@/components/ai/database/DatabaseImportModal.vue";
import {
  createAiDatabase,
  deleteAiDatabase,
  getAiDatabaseById,
  getAiDatabasesPaged,
  updateAiDatabase,
  type AiDatabaseListItem
} from "@/services/api-ai-database";

const router = useRouter();
const list = ref<AiDatabaseListItem[]>([]);
const loading = ref(false);
const keyword = ref("");
const pageIndex = ref(1);
const pageSize = ref(20);
const total = ref(0);

const columns = computed(() => [
  { title: t("ai.promptLib.colName"), dataIndex: "name", key: "name", width: 220 },
  { title: t("ai.promptLib.labelDescription"), dataIndex: "description", key: "description", ellipsis: true },
  { title: t("ai.database.colRecords"), dataIndex: "recordCount", key: "recordCount", width: 100 },
  { title: t("ai.database.colBot"), key: "botId", width: 120 },
  { title: t("ai.workflow.colUpdatedAt"), dataIndex: "updatedAt", key: "updatedAt", width: 200 },
  { title: t("ai.colActions"), key: "action", width: 260 }
]);

const modalVisible = ref(false);
const modalLoading = ref(false);
const editingId = ref<number | null>(null);
const formRef = ref<FormInstance>();
const form = reactive({
  name: "",
  description: "",
  botId: undefined as number | undefined,
  tableSchema: '[{"name":"id","type":"string"},{"name":"value","type":"string"}]'
});

const rules = computed(() => ({
  name: [{ required: true, message: t("ai.database.ruleName") }],
  tableSchema: [{ required: true, message: t("ai.database.ruleSchema") }]
}));

const importOpen = ref(false);
const importDatabaseId = ref(0);

async function loadData() {
  loading.value = true;
  try {
    const result  = await getAiDatabasesPaged(
      { pageIndex: pageIndex.value, pageSize: pageSize.value },
      keyword.value || undefined
    );

    if (!isMounted.value) return;
    list.value = result.items;
    total.value = Number(result.total);
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.database.loadFailed"));
  } finally {
    loading.value = false;
  }
}

function handleReset() {
  keyword.value = "";
  pageIndex.value = 1;
  void loadData();
}

function goDetail(id: number) {
  void router.push(`/ai/databases/${id}`);
}

function openCreate() {
  editingId.value = null;
  Object.assign(form, {
    name: "",
    description: "",
    botId: undefined,
    tableSchema: '[{"name":"id","type":"string"},{"name":"value","type":"string"}]'
  });
  modalVisible.value = true;
}

async function openEdit(id: number) {
  try {
    const detail  = await getAiDatabaseById(id);

    if (!isMounted.value) return;
    editingId.value = id;
    Object.assign(form, {
      name: detail.name,
      description: detail.description ?? "",
      botId: detail.botId,
      tableSchema: detail.tableSchema
    });
    modalVisible.value = true;
  } catch (error: unknown) {
    message.error((error as Error).message || t("crud.loadDetailFailed"));
  }
}

function closeModal() {
  modalVisible.value = false;
  formRef.value?.resetFields();
}

async function submitForm() {
  try {
    await formRef.value?.validate();

    if (!isMounted.value) return;
  } catch {
    return;
  }

  modalLoading.value = true;
  try {
    if (editingId.value) {
      await updateAiDatabase(editingId.value, {
        name: form.name,
        description: form.description || undefined,
        botId: form.botId,
        tableSchema: form.tableSchema
      });

      if (!isMounted.value) return;
      message.success(t("crud.updateSuccess"));
    } else {
      await createAiDatabase({
        name: form.name,
        description: form.description || undefined,
        botId: form.botId,
        tableSchema: form.tableSchema
      });

      if (!isMounted.value) return;
      message.success(t("crud.createSuccess"));
    }

    modalVisible.value = false;
    await loadData();

    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("crud.submitFailed"));
  } finally {
    modalLoading.value = false;
  }
}

async function handleDelete(id: number) {
  try {
    await deleteAiDatabase(id);

    if (!isMounted.value) return;
    message.success(t("crud.deleteSuccess"));
    await loadData();

    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("crud.deleteFailed"));
  }
}

function openImport(databaseId: number) {
  importDatabaseId.value = databaseId;
  importOpen.value = true;
}

function closeImport() {
  importOpen.value = false;
}

function handleImportSuccess() {
  importOpen.value = false;
  void loadData();
}

onMounted(() => {
  void loadData();
});
</script>

<style scoped>
.toolbar {
  margin-bottom: 16px;
}

.pager {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>

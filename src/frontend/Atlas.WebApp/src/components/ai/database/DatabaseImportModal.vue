<template>
  <a-modal
    :open="open"
    :title="t('ai.dbImport.title')"
    :confirm-loading="submitting"
    :ok-text="t('ai.dbImport.ok')"
    @ok="handleSubmit"
    @cancel="emit('cancel')"
  >
    <a-alert
      type="info"
      show-icon
      :message="t('ai.dbImport.alert')"
      style="margin-bottom: 16px"
    />

    <file-upload-panel
      v-model="uploadedFiles"
      :max-count="1"
      accept=".csv,text/csv"
      :button-text="t('ai.dbImport.uploadCsv')"
    />
  </a-modal>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { message } from "ant-design-vue";
import FileUploadPanel from "@/components/common/file-upload-panel.vue";
import { submitAiDatabaseImport } from "@/services/api-ai-database";
import type { FileUploadResult } from "@/types/api";

const { t } = useI18n();

const props = defineProps<{
  open: boolean;
  databaseId: number;
}>();

const emit = defineEmits<{
  (e: "success"): void;
  (e: "cancel"): void;
}>();

const uploadedFiles = ref<FileUploadResult[]>([]);
const submitting = ref(false);

async function handleSubmit() {
  const file = uploadedFiles.value[0];
  if (!file) {
    message.warning(t("ai.dbImport.warnCsv"));
    return;
  }

  submitting.value = true;
  try {
    await submitAiDatabaseImport(props.databaseId, { fileId: file.id });
    message.success(t("ai.dbImport.taskSubmitted"));
    uploadedFiles.value = [];
    emit("success");
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.dbImport.submitFailed"));
  } finally {
    submitting.value = false;
  }
}
</script>

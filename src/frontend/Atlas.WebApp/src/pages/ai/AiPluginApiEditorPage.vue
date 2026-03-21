<template>
  <a-card :title="t('ai.pluginApiEditor.pageTitle')" :bordered="false">
    <a-alert
      type="info"
      show-icon
      :message="t('ai.pluginApiEditor.alertInfo')"
      style="margin-bottom: 16px"
    />

    <a-form ref="formRef" :model="form" layout="vertical" :rules="rules">
      <a-form-item :label="t('ai.pluginApiEditor.labelPluginId')">
        <a-input-number v-model:value="pluginId" :min="1" style="width: 100%" />
      </a-form-item>
      <a-form-item :label="t('ai.pluginApiEditor.labelApiIdOptional')">
        <a-input-number v-model:value="apiId" :min="1" style="width: 100%" />
      </a-form-item>
      <a-form-item :label="t('ai.promptLib.colName')" name="name">
        <a-input v-model:value="form.name" />
      </a-form-item>
      <a-form-item :label="t('ai.promptLib.labelDescription')" name="description">
        <a-textarea v-model:value="form.description" :rows="3" />
      </a-form-item>
      <a-form-item label="Method" name="method">
        <a-select v-model:value="form.method" :options="methodOptions" />
      </a-form-item>
      <a-form-item label="Path" name="path">
        <a-input v-model:value="form.path" />
      </a-form-item>
      <a-form-item :label="t('ai.pluginApiEditor.labelRequestSchema')">
        <a-textarea v-model:value="form.requestSchemaJson" :rows="5" />
      </a-form-item>
      <a-form-item :label="t('ai.pluginApiEditor.labelResponseSchema')">
        <a-textarea v-model:value="form.responseSchemaJson" :rows="5" />
      </a-form-item>
      <a-form-item :label="t('ai.pluginApiEditor.labelTimeout')" name="timeoutSeconds">
        <a-input-number v-model:value="form.timeoutSeconds" :min="1" :max="600" style="width: 100%" />
      </a-form-item>
      <a-form-item>
        <a-space>
          <a-switch v-model:checked="form.isEnabled" />
          <span>{{ t("ai.pluginApiEditor.enabledState") }}</span>
        </a-space>
      </a-form-item>
      <a-form-item>
        <a-space>
          <a-button @click="loadFromServer">{{ t("ai.pluginApiEditor.load") }}</a-button>
          <a-button type="primary" :loading="submitting" @click="submit">{{ t("common.save") }}</a-button>
        </a-space>
      </a-form-item>
    </a-form>
  </a-card>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { useRoute } from "vue-router";
import type { FormInstance } from "ant-design-vue";
import { message } from "ant-design-vue";
import {
  createAiPluginApi,
  getAiPluginApis,
  updateAiPluginApi
} from "@/services/api-ai-plugin";

const route = useRoute();
const pluginId = ref<number | undefined>(undefined);
const apiId = ref<number | undefined>(undefined);
const formRef = ref<FormInstance>();
const submitting = ref(false);

const form = reactive({
  name: "",
  description: "",
  method: "GET",
  path: "/",
  requestSchemaJson: "{}",
  responseSchemaJson: "{}",
  timeoutSeconds: 30,
  isEnabled: true
});

const rules = computed(() => ({
  name: [{ required: true, message: t("ai.promptLib.ruleName") }],
  method: [{ required: true, message: t("ai.plugin.ruleMethod") }],
  path: [{ required: true, message: t("ai.plugin.rulePath") }]
}));

const methodOptions = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"].map((x) => ({
  label: x,
  value: x
}));

async function loadFromServer() {
  if (!pluginId.value || !apiId.value) {
    message.warning(t("ai.pluginApiEditor.warnIds"));
    return;
  }

  try {
    const apis  = await getAiPluginApis(pluginId.value);

    if (!isMounted.value) return;
    const target = apis.find((x) => x.id === apiId.value);
    if (!target) {
      message.warning(t("ai.pluginApiEditor.apiNotFound"));
      return;
    }

    Object.assign(form, {
      name: target.name,
      description: target.description ?? "",
      method: target.method,
      path: target.path,
      requestSchemaJson: target.requestSchemaJson,
      responseSchemaJson: target.responseSchemaJson,
      timeoutSeconds: target.timeoutSeconds,
      isEnabled: target.isEnabled
    });
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.pluginApiEditor.loadApiFailed"));
  }
}

async function submit() {
  if (!pluginId.value) {
    message.warning(t("ai.pluginApiEditor.warnPluginId"));
    return;
  }

  try {
    await formRef.value?.validate();

    if (!isMounted.value) return;
  } catch {
    return;
  }

  submitting.value = true;
  try {
    if (apiId.value) {
      await updateAiPluginApi(pluginId.value, apiId.value, {
        name: form.name,
        description: form.description || undefined,
        method: form.method,
        path: form.path,
        requestSchemaJson: form.requestSchemaJson || undefined,
        responseSchemaJson: form.responseSchemaJson || undefined,
        timeoutSeconds: form.timeoutSeconds,
        isEnabled: form.isEnabled
      });

      if (!isMounted.value) return;
      message.success(t("crud.updateSuccess"));
    } else {
      const newId  = await createAiPluginApi(pluginId.value, {
        name: form.name,
        description: form.description || undefined,
        method: form.method,
        path: form.path,
        requestSchemaJson: form.requestSchemaJson || undefined,
        responseSchemaJson: form.responseSchemaJson || undefined,
        timeoutSeconds: form.timeoutSeconds
      });

      if (!isMounted.value) return;
      apiId.value = newId;
      message.success(t("crud.createSuccess"));
    }
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.pluginApiEditor.saveFailed"));
  } finally {
    submitting.value = false;
  }
}

onMounted(() => {
  const routePluginId = Number(route.params.id);
  const routeApiId = Number(route.params.apiId);
  if (Number.isFinite(routePluginId) && routePluginId > 0) {
    pluginId.value = routePluginId;
  }
  if (Number.isFinite(routeApiId) && routeApiId > 0) {
    apiId.value = routeApiId;
    void loadFromServer();
  }
});
</script>

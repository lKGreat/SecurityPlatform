<template>
  <a-card :title="pageTitle" :class="{ 'runtime-mobile-card': isMobile }">
    <a-spin :spinning="loading">
      <AmisRenderer v-if="schema" :schema="schema" />
      <a-empty v-else description="未找到可运行页面，请确认应用已发布且页面存在" />
    </a-spin>
  </a-card>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute } from "vue-router";
import { message } from "ant-design-vue";
import AmisRenderer from "@/components/amis/amis-renderer.vue";
import type { AmisSchema } from "@/types/amis";
import { getLowCodeAppByKey, getLowCodeRuntimePageSchemaByKey } from "@/services/lowcode";

const route = useRoute();
const loading = ref(false);
const schema = ref<AmisSchema | null>(null);
const pageTitle = ref("运行交付面");
const isMobile = computed(() => window.innerWidth <= 768 || route.query.deviceMode === "mobile");

const appKey = computed(() => String(route.params.appKey ?? ""));
const pageKey = computed(() => String(route.params.pageKey ?? ""));

function applyRuntimeSubmitApi(schemaNode: unknown, appKeyValue: string, pageKeyValue: string) {
  if (!schemaNode || typeof schemaNode !== "object") {
    return;
  }

  const node = schemaNode as Record<string, unknown>;
  if (node.type === "form" && !node.api) {
    node.api = `post:/api/v1/runtime/apps/${encodeURIComponent(appKeyValue)}/pages/${encodeURIComponent(pageKeyValue)}/records`;
  }

  Object.values(node).forEach((child) => {
    if (Array.isArray(child)) {
      child.forEach((item) => applyRuntimeSubmitApi(item, appKeyValue, pageKeyValue));
      return;
    }
    applyRuntimeSubmitApi(child, appKeyValue, pageKeyValue);
  });
}

async function loadRuntime() {
  if (!appKey.value || !pageKey.value) {
    schema.value = null;
    return;
  }

  loading.value = true;
  try {
    const app = await getLowCodeAppByKey(appKey.value);
    const page = app.pages.find((item) => item.pageKey === pageKey.value);
    if (!page) {
      schema.value = null;
      return;
    }

    pageTitle.value = `${app.name} / ${page.name}`;
    const runtime = await getLowCodeRuntimePageSchemaByKey(appKey.value, pageKey.value);
    const parsedSchema = JSON.parse(runtime.schemaJson) as AmisSchema;
    applyRuntimeSubmitApi(parsedSchema, appKey.value, pageKey.value);
    schema.value = parsedSchema;
  } catch (error) {
    schema.value = null;
    message.error((error as Error).message || "加载运行态页面失败");
  } finally {
    loading.value = false;
  }
}

onMounted(loadRuntime);
watch([appKey, pageKey], () => {
  loadRuntime();
});
</script>

<style scoped>
.runtime-mobile-card {
  margin: 0;
  border-radius: 0;
}
</style>


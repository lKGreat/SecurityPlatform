<template>
  <a-card :title="pageTitle">
    <a-spin :spinning="loading">
      <AmisRenderer v-if="schema" :schema="schema" />
      <a-empty v-else description="未找到可运行页面，请先在设计器发布页面" />
    </a-spin>
  </a-card>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute } from "vue-router";
import { message } from "ant-design-vue";
import AmisRenderer from "@/components/amis/amis-renderer.vue";
import type { AmisSchema } from "@/types/amis";
import { getLowCodeAppDetail, getLowCodeRuntimePageSchema } from "@/services/lowcode";

const route = useRoute();
const loading = ref(false);
const schema = ref<AmisSchema | null>(null);
const pageTitle = ref("运行态页面");

const appId = computed(() => String(route.params.appId ?? ""));
const pageKey = computed(() => String(route.params.pageKey ?? ""));

async function loadRuntime() {
  if (!appId.value || !pageKey.value) {
    schema.value = null;
    return;
  }

  loading.value = true;
  try {
    const app = await getLowCodeAppDetail(appId.value);
    const page = app.pages.find((item) => item.pageKey === pageKey.value);
    if (!page) {
      schema.value = null;
      return;
    }

    pageTitle.value = `${app.name} / ${page.name}`;
    const runtime = await getLowCodeRuntimePageSchema(page.id, "published");
    schema.value = JSON.parse(runtime.schemaJson) as AmisSchema;
  } catch (error) {
    schema.value = null;
    message.error((error as Error).message || "加载运行态页面失败");
  } finally {
    loading.value = false;
  }
}

onMounted(loadRuntime);
watch([appId, pageKey], () => {
  loadRuntime();
});
</script>

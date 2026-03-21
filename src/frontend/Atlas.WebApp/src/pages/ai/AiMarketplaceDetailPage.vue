<template>
  <a-card :title="t('ai.marketplace.detailTitle', { id: productId })" :bordered="false">
    <template #extra>
      <a-space>
        <a-button @click="goBack">{{ t("ai.plugin.back") }}</a-button>
        <a-button :loading="publishLoading" @click="handlePublish">{{ t("ai.workflow.publish") }}</a-button>
        <a-button :loading="downloadLoading" @click="handleMarkDownload">{{ t("ai.marketplace.recordDownload") }}</a-button>
        <a-button :loading="favoriteLoading" @click="toggleFavorite">
          {{ detail?.isFavorited ? t("ai.marketplace.unfavorite") : t("ai.marketplace.favorite") }}
        </a-button>
      </a-space>
    </template>

    <a-spin :spinning="loading">
      <a-descriptions v-if="detail" :column="2" bordered size="small">
        <a-descriptions-item :label="t('ai.promptLib.colName')">{{ detail.name }}</a-descriptions-item>
        <a-descriptions-item :label="t('ai.promptLib.labelCategory')">{{ detail.categoryName }}</a-descriptions-item>
        <a-descriptions-item :label="t('ai.marketplace.labelVersion')">{{ detail.version }}</a-descriptions-item>
        <a-descriptions-item :label="t('ai.workflow.colStatus')">{{ formatStatus(detail.status) }}</a-descriptions-item>
        <a-descriptions-item :label="t('ai.marketplace.colDownloads')">{{ detail.downloadCount }}</a-descriptions-item>
        <a-descriptions-item :label="t('ai.marketplace.colFavorites')">{{ detail.favoriteCount }}</a-descriptions-item>
        <a-descriptions-item :label="t('ai.marketplace.labelSummary')" :span="2">{{ detail.summary || "-" }}</a-descriptions-item>
        <a-descriptions-item :label="t('ai.promptLib.labelDescription')" :span="2">{{ detail.description || "-" }}</a-descriptions-item>
        <a-descriptions-item :label="t('ai.promptLib.colTags')" :span="2">
          <a-space wrap>
            <a-tag v-for="tag in detail.tags" :key="tag">{{ tag }}</a-tag>
          </a-space>
        </a-descriptions-item>
      </a-descriptions>
      <a-empty v-else :description="t('ai.marketplace.productMissing')" />
    </a-spin>
  </a-card>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { useRoute, useRouter } from "vue-router";
import { message } from "ant-design-vue";
import {
  favoriteAiMarketplaceProduct,
  getAiMarketplaceProductById,
  markAiMarketplaceProductDownloaded,
  publishAiMarketplaceProduct,
  unfavoriteAiMarketplaceProduct,
  type AiMarketplaceProductDetail,
  type AiMarketplaceProductStatus
} from "@/services/api-ai-marketplace";

const route = useRoute();
const router = useRouter();
const productId = computed(() => Number(route.params.id));

const detail = ref<AiMarketplaceProductDetail | null>(null);
const loading = ref(false);
const publishLoading = ref(false);
const favoriteLoading = ref(false);
const downloadLoading = ref(false);

function formatStatus(status: AiMarketplaceProductStatus) {
  if (status === 1) {
    return t("ai.marketplace.statusPublished");
  }

  if (status === 2) {
    return t("ai.marketplace.statusArchived");
  }

  return t("ai.marketplace.statusDraft");
}

async function loadDetail() {
  loading.value = true;
  try {
    detail.value = await getAiMarketplaceProductById(productId.value);

    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.marketplace.loadFailed"));
    detail.value = null;
  } finally {
    loading.value = false;
  }
}

function goBack() {
  void router.push("/ai/marketplace");
}

async function handlePublish() {
  if (!detail.value) {
    return;
  }

  publishLoading.value = true;
  try {
    const version = detail.value.status === 0 ? "1.0.0" : detail.value.version;
    await publishAiMarketplaceProduct(detail.value.id, { version });

    if (!isMounted.value) return;
    message.success(t("ai.marketplace.publishSuccess"));
    await loadDetail();

    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.marketplace.publishFailed"));
  } finally {
    publishLoading.value = false;
  }
}

async function toggleFavorite() {
  if (!detail.value) {
    return;
  }

  favoriteLoading.value = true;
  try {
    if (detail.value.isFavorited) {
      await unfavoriteAiMarketplaceProduct(detail.value.id);

      if (!isMounted.value) return;
    } else {
      await favoriteAiMarketplaceProduct(detail.value.id);

      if (!isMounted.value) return;
    }

    await loadDetail();


    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.marketplace.favoriteStateFailed"));
  } finally {
    favoriteLoading.value = false;
  }
}

async function handleMarkDownload() {
  if (!detail.value) {
    return;
  }

  downloadLoading.value = true;
  try {
    await markAiMarketplaceProductDownloaded(detail.value.id);

    if (!isMounted.value) return;
    message.success(t("ai.marketplace.downloadRecorded"));
    await loadDetail();

    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.marketplace.downloadFailed"));
  } finally {
    downloadLoading.value = false;
  }
}

onMounted(() => {
  void loadDetail();
});
</script>

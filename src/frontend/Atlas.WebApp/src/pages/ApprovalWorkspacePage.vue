<template>
  <div class="approval-workspace-page">
    <div class="workspace-header">
      <h2 class="workspace-title">审批工作台</h2>
      <a-segmented v-model:value="activeTab" :options="tabOptions" size="large" />
    </div>
    
    <div class="workspace-content">
      <ApprovalTasksPage v-if="activeTab === 'pending'" :url-keyword="urlKeyword" :url-status="urlStatus" @update-filter="handleFilterUpdate" />
      <ApprovalDonePage v-else-if="activeTab === 'done'" :url-keyword="urlKeyword" :url-status="urlStatus" @update-filter="handleFilterUpdate" />
      <ApprovalInstancesPage v-else-if="activeTab === 'requests'" :url-keyword="urlKeyword" :url-status="urlStatus" @update-filter="handleFilterUpdate" />
      <ApprovalCcPage v-else-if="activeTab === 'cc'" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import ApprovalTasksPage from './ApprovalTasksPage.vue';
import ApprovalDonePage from './ApprovalDonePage.vue';
import ApprovalInstancesPage from './ApprovalInstancesPage.vue';
import ApprovalCcPage from './ApprovalCcPage.vue';

const route = useRoute();
const router = useRouter();

const activeTab = ref('pending');
const urlKeyword = ref<string>('');
const urlStatus = ref<string>('');

const tabOptions = [
  { label: '待办任务', value: 'pending' },
  { label: '已办任务', value: 'done' },
  { label: '我发起的', value: 'requests' },
  { label: '抄送我的', value: 'cc' },
];

const handleFilterUpdate = (filter: { keyword: string; status: string }) => {
  router.replace({ 
    query: { ...route.query, k: filter.keyword || undefined, s: filter.status || undefined } 
  });
};

onMounted(() => {
  const tab = route.query.tab as string;
  if (tab && tabOptions.some(t => t.value === tab)) {
    activeTab.value = tab;
  }
  urlKeyword.value = (route.query.k as string) || '';
  urlStatus.value = (route.query.s as string) || '';
});

watch(activeTab, (newVal) => {
  // Clear sub-filters when switching tabs
  urlKeyword.value = '';
  urlStatus.value = '';
  router.replace({ query: { tab: newVal } });
});
</script>

<style scoped>
.approval-workspace-page {
  display: flex;
  flex-direction: column;
  height: calc(100vh - var(--header-height) - 40px);
  padding: 0;
  background: var(--color-bg-base);
}
.workspace-header {
  padding: 16px 24px;
  background: var(--color-bg-container);
  border-bottom: 1px solid var(--color-border);
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.workspace-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: var(--color-text-primary);
}
.workspace-content {
  flex: 1;
  overflow: hidden;
  position: relative;
}
</style>

<template>
  <CrudPageLayout :title="t('approvalAgent.pageTitle')">
    <template #toolbar-actions>
      <a-button type="primary" @click="handleCreate">{{ t('approvalAgent.addAgent') }}</a-button>
    </template>
    <template #table>
    <a-table
      :columns="columns"
      :data-source="dataSource"
      :loading="loading"
      row-key="id"
      :pagination="false"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'timeRange'">
          {{ formatTime(record.startTime) }} ~ {{ formatTime(record.endTime) }}
        </template>
        <template v-else-if="column.key === 'isEnabled'">
          <a-tag :color="record.isEnabled ? 'green' : 'default'">
            {{ record.isEnabled ? t('approvalAgent.statusActive') : t('approvalAgent.statusInactive') }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'action'">
          <a-popconfirm :title="t('approvalAgent.deleteConfirm')" @confirm="handleDelete(record.id)">
            <a-button type="link" danger size="small">{{ t('approvalAgent.delete') }}</a-button>
          </a-popconfirm>
        </template>
      </template>
    </a-table>

    <a-modal
      v-model:open="modalVisible"
      :title="t('approvalAgent.modalTitle')"
      :confirm-loading="submitting"
      @ok="handleSubmit"
      @cancel="resetForm"
    >
      <a-form :model="form" layout="vertical">
        <a-form-item :label="t('approvalAgent.labelAgentUserId')" required>
          <a-input
            v-model:value="form.agentUserId"
            :placeholder="t('approvalAgent.placeholderAgentUserId')"
          />
        </a-form-item>
        <a-form-item :label="t('approvalAgent.labelValidity')" required>
          <a-range-picker
            v-model:value="form.dateRange"
            show-time
            style="width: 100%"
            format="YYYY-MM-DD HH:mm"
          />
        </a-form-item>
      </a-form>
    </a-modal>
    </template>
  </CrudPageLayout>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, onUnmounted } from 'vue';
import { useI18n } from 'vue-i18n';

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { message } from 'ant-design-vue';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import {
  getMyAgentConfigs,
  createAgentConfig,
  deleteAgentConfig,
  type ApprovalAgentConfigResponse,
} from '@/services/api';
import CrudPageLayout from "@/components/crud/CrudPageLayout.vue";

const { t } = useI18n();

const columns = computed(() => [
  { title: t('approvalAgent.colAgentUserId'), dataIndex: 'agentUserId', key: 'agentUserId' },
  { title: t('approvalAgent.colPrincipalUserId'), dataIndex: 'principalUserId', key: 'principalUserId' },
  { title: t('approvalAgent.colValidity'), key: 'timeRange' },
  { title: t('approvalAgent.colStatus'), key: 'isEnabled', width: 100 },
  { title: t('approvalAgent.colActions'), key: 'action', width: 100 },
]);

const dataSource = ref<ApprovalAgentConfigResponse[]>([]);
const loading = ref(false);
const modalVisible = ref(false);
const submitting = ref(false);

const form = reactive<{
  agentUserId: string;
  dateRange: [Dayjs, Dayjs] | null;
}>({
  agentUserId: '',
  dateRange: null,
});

const fetchData = async () => {
  loading.value = true;
  try {
    dataSource.value = await getMyAgentConfigs();

    if (!isMounted.value) return;
  } catch (err) {
    message.error(err instanceof Error ? err.message : t('approvalAgent.loadFailed'));
  } finally {
    loading.value = false;
  }
};

const handleCreate = () => {
  modalVisible.value = true;
};

const resetForm = () => {
  form.agentUserId = '';
  form.dateRange = null;
  modalVisible.value = false;
};

const handleSubmit = async () => {
  if (!form.agentUserId.trim()) {
    message.warning(t('approvalAgent.warnAgentUserId'));
    return;
  }
  if (!form.dateRange) {
    message.warning(t('approvalAgent.warnValidity'));
    return;
  }

  submitting.value = true;
  try {
    await createAgentConfig({
      agentUserId: form.agentUserId.trim(),
      startTime: form.dateRange[0].toISOString(),
      endTime: form.dateRange[1].toISOString(),
    });

    if (!isMounted.value) return;
    message.success(t('approvalAgent.addSuccess'));
    resetForm();
    await fetchData();

    if (!isMounted.value) return;
  } catch (err) {
    message.error(err instanceof Error ? err.message : t('approvalAgent.addFailed'));
  } finally {
    submitting.value = false;
  }
};

const handleDelete = async (id: string) => {
  try {
    await deleteAgentConfig(id);

    if (!isMounted.value) return;
    message.success(t('approvalAgent.deleteSuccess'));
    await fetchData();

    if (!isMounted.value) return;
  } catch (err) {
    message.error(err instanceof Error ? err.message : t('approvalAgent.deleteFailed'));
  }
};

const formatTime = (time: string) => dayjs(time).format('YYYY-MM-DD HH:mm');

onMounted(() => {
  void fetchData();
});
</script>

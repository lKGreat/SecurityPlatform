<template>
  <a-tag :color="color">{{ text }}</a-tag>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { ApprovalTaskStatus } from '@/types/api';

const { t } = useI18n();

const props = defineProps<{
  status?: ApprovalTaskStatus | string | number;
}>();

const color = computed(() => {
  switch (props.status) {
    case ApprovalTaskStatus.Pending: return "processing";
    case ApprovalTaskStatus.Approved: return "success";
    case ApprovalTaskStatus.Rejected: return "error";
    case ApprovalTaskStatus.Canceled: return "default";
    default: return "default";
  }
});

const text = computed(() => {
  switch (props.status) {
    case ApprovalTaskStatus.Pending: return t("approvalWorkspace.statusPending");
    case ApprovalTaskStatus.Approved: return t("approvalWorkspace.statusApproved");
    case ApprovalTaskStatus.Rejected: return t("approvalWorkspace.statusRejected");
    case ApprovalTaskStatus.Canceled: return t("approvalWorkspace.statusCanceled");
    default: return t("approvalWorkspace.statusUnknown");
  }
});
</script>

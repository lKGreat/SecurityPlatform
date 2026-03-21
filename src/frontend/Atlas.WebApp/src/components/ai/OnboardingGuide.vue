<template>
  <a-modal
    v-model:open="visible"
    :title="popup?.title || t('ai.onboarding.defaultTitle')"
    :ok-text="t('ai.onboarding.ok')"
    :cancel-text="t('ai.onboarding.remindLater')"
    @ok="dismiss(true)"
    @cancel="dismiss(false)"
  >
    <p>{{ popup?.content }}</p>
    <a-divider />
    <div class="shortcut-list">
      <div v-for="item in shortcuts" :key="item.id" class="shortcut-item">
        <a-tag color="blue">{{ item.commandKey }}</a-tag>
        <span>{{ item.displayName }}</span>
      </div>
    </div>
  </a-modal>
</template>

<script setup lang="ts">
import { onMounted, ref, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";
import { message } from "ant-design-vue";
import {
  dismissAiOnboardingPopup,
  getAiOnboardingPopup,
  type AiBotPopupInfoDto,
  type AiShortcutCommandItem
} from "@/services/api-ai-shortcut";

const { t } = useI18n();

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

const props = defineProps<{
  shortcuts: AiShortcutCommandItem[];
}>();

const popup = ref<AiBotPopupInfoDto | null>(null);
const visible = ref(false);

async function loadPopup() {
  try {
    popup.value = await getAiOnboardingPopup();

    if (!isMounted.value) return;
    visible.value = !popup.value.dismissed;
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.onboarding.loadFailed"));
  }
}

async function dismiss(value: boolean) {
  if (!popup.value) {
    return;
  }

  try {
    popup.value = await dismissAiOnboardingPopup(popup.value.popupCode, value);

    if (!isMounted.value) return;
    visible.value = false;
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.onboarding.updateFailed"));
  }
}

onMounted(() => {
  if (props.shortcuts.length > 0) {
    void loadPopup();
  }
});
</script>

<style scoped>
.shortcut-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.shortcut-item {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>

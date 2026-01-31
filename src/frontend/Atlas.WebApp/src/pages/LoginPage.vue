<template>
  <div class="login-page">
    <header class="login-header">
      <div class="brand-area">
        <div class="logo-circle">Atlas</div>
        <div>
          <h1>安全控制台</h1>
          <p>统一安全管理 & 运维管控</p>
        </div>
      </div>
      <div class="header-links">
        <a href="https://docs.securityplatform.local" target="_blank" rel="noopener">帮助</a>
        <a href="https://docs.securityplatform.local" target="_blank" rel="noopener">文档</a>
      </div>
    </header>

    <main class="login-main">
      <section class="promo-panel">
        <h2>智控 · 守护 · 可审计</h2>
        <p>从账号到权限，从审计到告警，提供一套可复用的控制台管理体验。</p>
        <ul>
          <li>统一身份 + 租户/组织可视化管理</li>
          <li>实时审计 + 风险策略自动落地</li>
        </ul>
      </section>

      <section class="card-panel">
        <a-card title="欢迎登录" class="login-card" :bordered="false">
          <div v-if="errorMessage" class="error-banner">
            <span class="error-dot" aria-hidden="true">!</span>
            <span>{{ errorMessage }}</span>
            <span v-if="cooldownSeconds > 0" class="cooldown">（请 {{ cooldownSeconds }} 秒后再试）</span>
          </div>

          <a-form
            layout="vertical"
            :model="form"
            class="login-form"
            @finish="handleSubmit"
          >
            <a-form-item
              label="租户 / 组织"
              name="tenantId"
              :rules="[{ required: true, message: '请输入租户 / 组织ID' }]"
            >
              <a-input
                v-model:value="form.tenantId"
                placeholder="请输入租户 / 组织 ID"
                list="tenant-history"
                allow-clear
                autocomplete="off"
                @focus="errorMessage = ''"
              />
              <datalist id="tenant-history">
                <option
                  v-for="item in tenantHistoryOptions"
                  :key="item.id"
                  :value="item.id"
                >
                  {{ item.label }}
                </option>
              </datalist>
              <div class="tenant-tags">
                <span
                  v-for="item in tenantHistoryOptions"
                  :key="item.id"
                  class="tenant-tag"
                  @click="handleSelectTenant(item.id)"
                >
                  {{ item.label }}
                </span>
              </div>
            </a-form-item>

            <a-form-item
              label="账号"
              name="username"
              :rules="[{ required: true, message: '请输入账号' }]"
            >
              <a-input
                v-model:value="form.username"
                placeholder="请输入手机号/邮箱/用户名"
                allow-clear
                autocomplete="username"
                @focus="errorMessage = ''"
              />
            </a-form-item>

            <a-form-item
              label="密码"
              name="password"
              :rules="[{ required: true, message: '请输入密码' }]"
            >
              <a-input-password
                v-model:value="form.password"
                placeholder="请输入密码"
                autocomplete="current-password"
                @keydown="handleCapsLockEvent"
                @keyup="handleCapsLockEvent"
                @blur="capsLockOn = false"
                @focus="errorMessage = ''"
              />
              <div v-if="capsLockOn" class="caps-tip">
                Caps Lock 已开启，可能影响密码输入
              </div>
            </a-form-item>

            <a-form-item
              v-if="isCaptchaVisible"
              label="图片验证码"
              name="captcha"
              :rules="[{ required: true, message: '请输入验证码' }]"
            >
              <div class="captcha-row">
                <a-input
                  v-model:value="form.captcha"
                  placeholder="请输入验证码"
                  autocomplete="off"
                  @focus="errorMessage = ''"
                />
                <div
                  class="captcha-image"
                  @click="refreshCaptcha"
                  :style="captchaStyle"
                >
                  <span>验证码</span>
                  <span class="captcha-tips">点击刷新</span>
                </div>
              </div>
            </a-form-item>

            <a-form-item>
              <a-button
                type="primary"
                block
                html-type="submit"
                :loading="loading"
                :disabled="isSubmitDisabled"
              >
                <span v-if="!loading">{{ cooldownSeconds > 0 ? `请稍候 (${cooldownSeconds}s)` : "登录" }}</span>
                <span v-else>登录中</span>
              </a-button>
            </a-form-item>

            <div class="secondary-actions">
              <a href="/password-reset">忘记密码</a>
            </div>
          </a-form>
        </a-card>
      </section>
    </main>

    <footer class="login-footer">
      <span>隐私政策</span>
      <span>用户协议</span>
      <span>版本 v1.0.2</span>
      <span>备案：沪ICP备xxxxxx号</span>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { createToken, getCurrentUser } from "@/services/api";
import type { RequestOptions } from "@/services/api";
import {
  clearAuthStorage,
  getTenantId,
  setAccessToken,
  setAuthProfile,
  setRefreshToken,
  setTenantId
} from "@/utils/auth";

interface TenantHistoryItem {
  id: string;
  label: string;
}

const TENANT_HISTORY_KEY = "atlas-login-tenant-history";
const DEFAULT_TENANT_ID = "00000000-0000-0000-0000-000000000001";
const CAPTCHA_THRESHOLD = 3;
const COOLDOWN_THRESHOLD = 5;
const COOLDOWN_DURATION = 30;

const router = useRouter();
const loading = ref(false);
const errorMessage = ref("");
const failedAttempts = ref(0);
const capsLockOn = ref(false);
const cooldownSeconds = ref(0);
const captchaSeed = ref(Date.now());
const tenantHistory = ref<TenantHistoryItem[]>([]);
let cooldownTimer: number | undefined;

const form = reactive({
  tenantId: getTenantId() ?? DEFAULT_TENANT_ID,
  username: "",
  password: "",
  captcha: ""
});

const isCaptchaVisible = computed(() => failedAttempts.value >= CAPTCHA_THRESHOLD);
const isSubmitDisabled = computed(
  () =>
    loading.value ||
    cooldownSeconds.value > 0 ||
    !form.username.trim() ||
    !form.password ||
    (isCaptchaVisible.value && !form.captcha.trim())
);
const tenantHistoryOptions = computed(() => tenantHistory.value);
const captchaStyle = computed(() => ({
  backgroundImage: `url('https://dummyimage.com/120x40/ced4da/6c757d.png&text=%E9%AA%8C%E8%AF%81%E7%A0%81&seed=${captchaSeed.value}')`,
  backgroundSize: "cover"
}));

const loadTenantHistory = () => {
  const raw = localStorage.getItem(TENANT_HISTORY_KEY);
  try {
    if (raw) {
      const parsed = JSON.parse(raw) as TenantHistoryItem[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        tenantHistory.value = parsed;
        if (!form.tenantId && parsed[0]) {
          form.tenantId = parsed[0].id;
        }
        return;
      }
    }
  } catch {
    // ignore malformed history
  }

  tenantHistory.value = [{ id: DEFAULT_TENANT_ID, label: "默认租户" }];
};

const persistTenantHistory = (tenantId: string) => {
  if (!tenantId) return;
  const label = tenantId === DEFAULT_TENANT_ID ? "默认租户" : tenantId;
  const existingIndex = tenantHistory.value.findIndex((item) => item.id === tenantId);
  const item = { id: tenantId, label };
  if (existingIndex >= 0) {
    tenantHistory.value.splice(existingIndex, 1);
  }
  tenantHistory.value.unshift(item);
  tenantHistory.value = tenantHistory.value.slice(0, 5);
  localStorage.setItem(TENANT_HISTORY_KEY, JSON.stringify(tenantHistory.value));
};

const handleCapsLockEvent = (event: KeyboardEvent) => {
  if (typeof event.getModifierState === "function") {
    capsLockOn.value = event.getModifierState("CapsLock");
  }
};

const startCooldown = () => {
  cooldownSeconds.value = COOLDOWN_DURATION;
  window.clearInterval(cooldownTimer);
  cooldownTimer = window.setInterval(() => {
    cooldownSeconds.value -= 1;
    if (cooldownSeconds.value <= 0) {
      window.clearInterval(cooldownTimer);
      cooldownSeconds.value = 0;
    }
  }, 1000);
};

const refreshCaptcha = () => {
  captchaSeed.value = Date.now();
};

const normalizeError = (error: unknown) => {
  const raw = error instanceof Error ? error.message : "登录失败";
  if (raw.includes("账号或密码")) {
    return "账号或密码错误，请重试";
  }
  if (raw.toLowerCase().includes("网络")) {
    return "网络异常，请稍后再试";
  }
  return raw;
};

const handleSelectTenant = (tenantId: string) => {
  form.tenantId = tenantId;
};

const handleSubmit = async () => {
  errorMessage.value = "";
  loading.value = true;
  try {
    clearAuthStorage();
    const tokenOptions: RequestOptions = {
      suppressErrorMessage: true
    };
    const result = await createToken(
      form.tenantId,
      form.username.trim(),
      form.password,
      tokenOptions
    );
    setAccessToken(result.accessToken);
    setRefreshToken(result.refreshToken);
    setTenantId(form.tenantId);
    const profile = await getCurrentUser();
    setAuthProfile(profile);
    persistTenantHistory(form.tenantId);
    failedAttempts.value = 0;
    cooldownSeconds.value = 0;
    errorMessage.value = "";
    router.push("/");
  } catch (error) {
    clearAuthStorage();
    failedAttempts.value += 1;
    errorMessage.value = normalizeError(error);
    if (failedAttempts.value >= COOLDOWN_THRESHOLD) {
      startCooldown();
    }
    if (isCaptchaVisible.value) {
      refreshCaptcha();
    }
  } finally {
    loading.value = false;
  }
};

onMounted(loadTenantHistory);
onBeforeUnmount(() => {
  window.clearInterval(cooldownTimer);
});
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f6f8fb;
}

.login-header {
  padding: 24px 64px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.brand-area {
  display: flex;
  align-items: center;
  gap: 16px;
}

.logo-circle {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: #1677ff;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
}

.brand-area h1 {
  margin: 0;
  font-size: 22px;
  font-weight: 600;
}

.brand-area p {
  margin: 4px 0 0;
  color: #8c8c8c;
  font-size: 14px;
}

.header-links a {
  margin-left: 24px;
  color: #595959;
  font-size: 14px;
}

.login-main {
  flex: 1;
  display: flex;
  gap: 32px;
  padding: 0 64px 48px;
  align-items: stretch;
}

.promo-panel {
  flex: 1;
  background: linear-gradient(135deg, #ffffff 0%, #f0f5ff 100%);
  border-radius: 16px;
  padding: 40px 48px;
  box-shadow: 0 16px 48px rgba(0, 0, 0, 0.08);
}

.promo-panel h2 {
  margin-top: 0;
  font-size: 28px;
  font-weight: 600;
}

.promo-panel p {
  margin: 16px 0;
  color: #53607b;
}

.promo-panel ul {
  padding-left: 20px;
  color: #1f2d3d;
  margin: 0;
}

.card-panel {
  width: 420px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.login-card {
  width: 100%;
  border-radius: 16px;
  box-shadow: 0 32px 60px rgba(2, 35, 87, 0.12);
}

.error-banner {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #fff1f0;
  border: 1px solid #ffa39e;
  color: #cf1322;
  padding: 10px 16px;
  border-radius: 6px;
  margin-bottom: 16px;
  font-size: 14px;
}

.error-dot {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: #cf1322;
  color: white;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
}

.cooldown {
  margin-left: auto;
  font-size: 12px;
  color: #595959;
}

.tenant-tags {
  margin-top: 8px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.tenant-tag {
  border: 1px dashed #d9d9d9;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 12px;
  cursor: pointer;
  color: #595959;
}

.tenant-tag:hover {
  border-color: #1677ff;
  color: #1677ff;
}

.caps-tip {
  margin-top: 4px;
  color: #fa8c16;
  font-size: 12px;
}

.captcha-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.captcha-image {
  width: 120px;
  height: 44px;
  border-radius: 8px;
  background: #f0f0f0;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  font-size: 12px;
  color: #595959;
  cursor: pointer;
}

.captcha-tips {
  margin-top: 4px;
  font-size: 10px;
  color: #8c8c8c;
}

.secondary-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 8px;
}

.secondary-actions a {
  color: #595959;
  font-size: 14px;
}

.login-footer {
  padding: 16px 64px;
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
  font-size: 12px;
  color: #8c8c8c;
  border-top: 1px solid #f0f0f0;
}

@media screen and (max-width: 1024px) {
  .login-main {
    flex-direction: column;
    padding: 0 24px 48px;
  }

  .promo-panel {
    order: 2;
  }

  .card-panel {
    width: 100%;
    order: 1;
  }
}

@media screen and (max-width: 720px) {
  .login-header,
  .login-footer {
    flex-direction: column;
    align-items: flex-start;
    padding: 16px;
  }

  .brand-area {
    gap: 12px;
  }

  .login-card {
    box-shadow: none;
  }

  .card-panel {
    padding: 0;
  }
}
</style>

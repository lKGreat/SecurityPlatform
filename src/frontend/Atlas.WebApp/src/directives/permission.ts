import type { Directive } from "vue";
import { useUserStore } from "@/stores/user";

export const hasPermi: Directive<HTMLElement, string[]> = {
  mounted(el, binding) {
    const userStore = useUserStore();
    const required = binding.value ?? [];
    const hasAdmin = userStore.roles.some((role) =>
      ["admin", "superadmin"].includes(role.toLowerCase())
    );
    if (hasAdmin) {
      return;
    }

    const has = required.some((code) => userStore.permissions.includes(code));
    if (!has) {
      el.parentNode?.removeChild(el);
    }
  }
};

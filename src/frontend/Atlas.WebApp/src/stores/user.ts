import { defineStore } from "pinia";
import type { AuthProfile } from "@/types/api";
import { clearAuthStorage, getAuthProfile, setAuthProfile } from "@/utils/auth";
import { getCurrentUser, logout as logoutApi } from "@/services/api";

interface UserState {
  profile: AuthProfile | null;
  roles: string[];
  permissions: string[];
}

export const useUserStore = defineStore("user", {
  state: (): UserState => ({
    profile: getAuthProfile(),
    roles: getAuthProfile()?.roles ?? [],
    permissions: getAuthProfile()?.permissions ?? []
  }),
  actions: {
    async getInfo() {
      const profile = await getCurrentUser();
      this.profile = profile;
      this.roles = profile.roles ?? [];
      this.permissions = profile.permissions ?? [];
      setAuthProfile(profile);
      return profile;
    },
    hydrateFromStorage() {
      const profile = getAuthProfile();
      this.profile = profile;
      this.roles = profile?.roles ?? [];
      this.permissions = profile?.permissions ?? [];
    },
    async logout() {
      try {
        await logoutApi();
      } finally {
        clearAuthStorage();
        this.profile = null;
        this.roles = [];
        this.permissions = [];
      }
    }
  }
});

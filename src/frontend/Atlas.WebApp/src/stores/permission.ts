import { defineStore } from "pinia";
import type { RouteRecordRaw, Router } from "vue-router";
import type { RouterVo } from "@/types/api";
import { getRouters } from "@/services/api";
import { buildRoutesFromRouters } from "@/utils/dynamic-router";

interface PermissionState {
  routes: RouteRecordRaw[];
  addRoutes: RouteRecordRaw[];
  sidebarRouters: RouterVo[];
  routeLoaded: boolean;
}

export const usePermissionStore = defineStore("permission", {
  state: (): PermissionState => ({
    routes: [],
    addRoutes: [],
    sidebarRouters: [],
    routeLoaded: false
  }),
  actions: {
    reset() {
      this.routes = [];
      this.addRoutes = [];
      this.sidebarRouters = [];
      this.routeLoaded = false;
    },
    async generateRoutes() {
      const routers = await getRouters();
      const routeRecords = buildRoutesFromRouters(routers);
      this.sidebarRouters = routers;
      this.addRoutes = routeRecords;
      this.routes = routeRecords;
      this.routeLoaded = true;
      return routeRecords;
    },
    registerRoutes(router: Router) {
      for (const route of this.addRoutes) {
        const name = route.name;
        if (typeof name === "string" && router.hasRoute(name)) {
          continue;
        }
        router.addRoute(route);
      }
    }
  }
});

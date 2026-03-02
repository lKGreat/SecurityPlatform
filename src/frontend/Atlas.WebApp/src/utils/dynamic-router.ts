import type { RouteRecordRaw } from "vue-router";
import type { RouterVo } from "@/types/api";

const pageModules = import.meta.glob("../pages/**/*.vue");

const componentMap: Record<string, string> = {
  home: "../pages/HomePage.vue",
  HomePage: "../pages/HomePage.vue",
  AssetsPage: "../pages/AssetsPage.vue",
  AuditPage: "../pages/AuditPage.vue",
  AlertPage: "../pages/AlertPage.vue",
  ApprovalFlowsPage: "../pages/ApprovalFlowsPage.vue",
  WorkflowDesignerPage: "../pages/WorkflowDesignerPage.vue",
  "system/UsersPage": "../pages/system/UsersPage.vue",
  "system/RolesPage": "../pages/system/RolesPage.vue",
  "system/MenusPage": "../pages/system/MenusPage.vue",
  "system/ProjectsPage": "../pages/system/ProjectsPage.vue"
};

function resolveComponent(component?: string, path?: string) {
  if (!component) {
    if (path === "/") {
      return pageModules["../pages/HomePage.vue"];
    }
    return () => import("@/pages/NotFoundPage.vue");
  }

  if (component === "Layout" || component === "ParentView") {
    return () => import("@/components/layout/RouterContainer.vue");
  }

  const mapped = componentMap[component];
  if (mapped && pageModules[mapped]) {
    return pageModules[mapped];
  }

  const candidate = `../pages/${component}.vue`;
  if (pageModules[candidate]) {
    return pageModules[candidate];
  }

  return () => import("@/pages/NotFoundPage.vue");
}

export function buildRoutesFromRouters(routers: RouterVo[]): RouteRecordRaw[] {
  return routers
    .filter((item) => item.path && item.name)
    .map((item) => toRouteRecord(item))
    .filter((item): item is RouteRecordRaw => !!item);
}

function toRouteRecord(item: RouterVo): RouteRecordRaw | null {
  const route: RouteRecordRaw = {
    path: item.path,
    name: item.name,
    component: resolveComponent(item.component, item.path),
    meta: {
      title: item.meta?.title ?? item.name,
      icon: item.meta?.icon,
      requiresAuth: true,
      requiresPermission: item.meta?.permi
    }
  };

  if (item.redirect) {
    route.redirect = item.redirect;
  }

  if (item.children && item.children.length > 0) {
    route.children = item.children
      .map((child) => toRouteRecord(child))
      .filter((child): child is RouteRecordRaw => !!child);
  }

  return route;
}

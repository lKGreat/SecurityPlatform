import { createRouter, createWebHistory } from "vue-router";
import HomePage from "@/pages/HomePage.vue";
import LoginPage from "@/pages/LoginPage.vue";
import AssetsPage from "@/pages/AssetsPage.vue";
import AuditPage from "@/pages/AuditPage.vue";
import AlertPage from "@/pages/AlertPage.vue";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/login", name: "login", component: LoginPage },
    { path: "/", name: "home", component: HomePage, meta: { requiresAuth: true } },
    { path: "/assets", name: "assets", component: AssetsPage, meta: { requiresAuth: true } },
    { path: "/audit", name: "audit", component: AuditPage, meta: { requiresAuth: true } },
    { path: "/alert", name: "alert", component: AlertPage, meta: { requiresAuth: true } }
  ]
});

router.beforeEach((to) => {
  if (!to.meta.requiresAuth) {
    return true;
  }

  const token = localStorage.getItem("access_token");
  if (!token) {
    return { name: "login" };
  }

  return true;
});

export default router;
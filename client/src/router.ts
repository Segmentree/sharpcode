import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { isAdmin } from './admin'

// Local-only authoring routes. `import.meta.env.DEV` is a literal `false` in production builds, so Vite drops
// this branch and never bundles the admin or editor views. (See ADMIN_UI_ENABLED in admin.ts for the UI gating.)
const adminRoutes: RouteRecordRaw[] = import.meta.env.DEV
  ? [
      { path: '/admin', name: 'admin', component: () => import('./views/AdminView.vue') },
      {
        path: '/problems/new',
        name: 'problem-new',
        component: () => import('./views/ProblemEditorView.vue'),
        meta: { admin: true },
      },
      {
        path: '/problems/:id/edit',
        name: 'problem-edit',
        component: () => import('./views/ProblemEditorView.vue'),
        props: true,
        meta: { admin: true },
      },
    ]
  : []

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'problems', component: () => import('./views/ProblemListView.vue') },
    { path: '/sandbox', name: 'sandbox', component: () => import('./views/SandboxView.vue') },
    ...adminRoutes,
    { path: '/problems/:slug', name: 'problem', component: () => import('./views/ProblemView.vue'), props: true },
  ],
})

router.beforeEach((to) => {
  if (to.meta.admin && !isAdmin.value) return { name: 'admin', query: { redirect: to.fullPath } }
  return true
})

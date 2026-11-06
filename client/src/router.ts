import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { isAdmin } from './admin'
import { isSignedIn } from './auth'

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
    { path: '/docs', name: 'docs', component: () => import('./views/DocsIndexView.vue') },
    { path: '/docs/:slug', name: 'doc-topic', component: () => import('./views/DocTopicView.vue'), props: true },
    { path: '/login', name: 'login', component: () => import('./views/LoginView.vue') },
    { path: '/sandbox', name: 'sandbox', component: () => import('./views/SandboxView.vue'), meta: { auth: true } },
    ...adminRoutes,
    { path: '/problems/:slug', name: 'problem', component: () => import('./views/ProblemView.vue'), props: true },
  ],
})

// main.ts validates the stored session before the router is installed, so `isSignedIn` is settled here.
router.beforeEach((to) => {
  if (to.meta.admin && !isAdmin.value) return { name: 'admin', query: { redirect: to.fullPath } }
  if (to.meta.auth && !isSignedIn.value) return { name: 'login', query: { redirect: to.fullPath } }
  if (to.name === 'login' && isSignedIn.value) return { path: typeof to.query.redirect === 'string' ? to.query.redirect : '/' }
  return true
})

import { createRouter, createWebHistory } from 'vue-router'
import { isAdmin } from './admin'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'problems', component: () => import('./views/ProblemListView.vue') },
    { path: '/sandbox', name: 'sandbox', component: () => import('./views/SandboxView.vue') },
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
    { path: '/problems/:slug', name: 'problem', component: () => import('./views/ProblemView.vue'), props: true },
  ],
})

router.beforeEach((to) => {
  if (to.meta.admin && !isAdmin.value) return { name: 'admin', query: { redirect: to.fullPath } }
  return true
})

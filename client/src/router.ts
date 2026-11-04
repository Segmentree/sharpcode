import { createRouter, createWebHistory } from 'vue-router'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'problems', component: () => import('./views/ProblemListView.vue') },
    { path: '/problems/new', name: 'problem-new', component: () => import('./views/ProblemEditorView.vue') },
    { path: '/problems/:id/edit', name: 'problem-edit', component: () => import('./views/ProblemEditorView.vue'), props: true },
    { path: '/problems/:slug', name: 'problem', component: () => import('./views/ProblemView.vue'), props: true },
  ],
})

import { createRouter, createWebHistory } from 'vue-router'
import { pinia } from '@/stores'
import { useSessionStore } from '@/stores/session'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { guest: true, titleKey: 'pageTitle.signIn' }
    },
    {
      path: '/',
      component: () => import('@/layouts/AppShell.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: '', name: 'overview', component: () => import('@/views/OverviewView.vue'), meta: { titleKey: 'pageTitle.overview' } },
        { path: 'configuration/routes', name: 'routes', component: () => import('@/views/ResourcesView.vue'), props: { resourceKind: 'routes' }, meta: { titleKey: 'pageTitle.routes' } },
        { path: 'configuration/clusters', name: 'clusters', component: () => import('@/views/ResourcesView.vue'), props: { resourceKind: 'clusters' }, meta: { titleKey: 'pageTitle.clusters' } },
        { path: 'changes', alias: '/drafts', name: 'changes', component: () => import('@/views/DraftsView.vue'), meta: { titleKey: 'pageTitle.changes' } },
        { path: 'changes/:id', alias: '/drafts/:id', name: 'studio', component: () => import('@/views/StudioView.vue'), meta: { titleKey: 'pageTitle.studio' } }
      ]
    },
    { path: '/:pathMatch(.*)*', redirect: '/' }
  ]
})

router.beforeEach(async to => {
  const session = useSessionStore(pinia)
  await session.restore()

  if (to.meta.requiresAuth && !session.user) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }
  if (to.meta.guest && session.user) return { name: 'overview' }
})

export default router
<script setup lang="ts">
import { computed, type Component } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Activity, FileStack, LayoutDashboard, LogOut, Route as RouteIcon, Server } from '@lucide/vue'
import BrandMark from '@/components/BrandMark.vue'
import LanguageSwitcher from '@/components/LanguageSwitcher.vue'
import { useSessionStore } from '@/stores/session'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()
const { t, te } = useI18n()

interface NavigationItem {
  name: string
  labelKey: string
  icon: Component
  activeNames?: string[]
}

interface NavigationSection {
  labelKey: string
  items: NavigationItem[]
}

const navigationSections: NavigationSection[] = [
  {
    labelKey: 'nav.workspace',
    items: [{ name: 'overview', labelKey: 'nav.overview', icon: LayoutDashboard }]
  },
  {
    labelKey: 'nav.configuration',
    items: [
      { name: 'routes', labelKey: 'nav.routes', icon: RouteIcon },
      { name: 'clusters', labelKey: 'nav.clusters', icon: Server }
    ]
  },
  {
    labelKey: 'nav.delivery',
    items: [{ name: 'changes', labelKey: 'nav.changes', icon: FileStack, activeNames: ['studio'] }]
  }
]
const navigation = navigationSections.flatMap(section => section.items)

const pageTitle = computed(() => t(typeof route.meta.titleKey === 'string' ? route.meta.titleKey : 'pageTitle.console'))
const initials = computed(() => (session.user?.displayName || session.user?.userName || 'U').slice(0, 2).toUpperCase())
const roleLabel = computed(() => {
  const role = session.user?.roles[0]
  return role && te(`role.${role}`) ? t(`role.${role}`) : role ?? ''
})

function isActive(item: NavigationItem) {
  return route.name === item.name || item.activeNames?.includes(String(route.name)) === true
}

async function signOut() {
  await session.logout()
  await router.replace({ name: 'login' })
}
</script>

<template>
  <div class="app-shell">
    <aside class="sidebar">
      <div class="sidebar-brand"><BrandMark inverse /></div>

      <nav class="primary-nav" :aria-label="t('nav.primary')">
        <div v-for="section in navigationSections" :key="section.labelKey" class="nav-section">
          <span class="nav-section-label">{{ t(section.labelKey) }}</span>
          <RouterLink
            v-for="item in section.items"
            :key="item.name"
            :to="{ name: item.name }"
            class="nav-link"
            :class="{ active: isActive(item) }"
          >
            <component :is="item.icon" :size="18" :stroke-width="1.8" />
            <span>{{ t(item.labelKey) }}</span>
          </RouterLink>
        </div>
      </nav>

      <div class="sidebar-status">
        <div class="status-line"><Activity :size="15" /> {{ t('shell.controlPlane') }}</div>
        <div class="status-value"><i /> {{ t('shell.connected') }}</div>
      </div>

      <div class="sidebar-language"><LanguageSwitcher inverse /></div>

      <div class="user-block">
        <span class="avatar">{{ initials }}</span>
        <span class="user-copy">
          <strong>{{ session.user?.displayName }}</strong>
          <small>{{ roleLabel }}</small>
        </span>
        <button class="icon-button" type="button" :title="t('shell.signOut')" :aria-label="t('shell.signOut')" @click="signOut">
          <LogOut :size="17" />
        </button>
      </div>
    </aside>

    <div class="shell-content">
      <header class="mobile-header">
        <BrandMark />
        <div class="mobile-user">
          <LanguageSwitcher />
          <span class="avatar">{{ initials }}</span>
          <button class="mobile-logout" type="button" :title="t('shell.signOut')" :aria-label="t('shell.signOut')" @click="signOut">
            <LogOut :size="18" />
          </button>
        </div>
      </header>
      <div class="context-bar">
        <span class="context-title">{{ pageTitle }}</span>
        <span class="context-state"><i /> {{ t('shell.apiConnected') }}</span>
      </div>
      <main><RouterView /></main>
    </div>

    <nav class="mobile-nav" :aria-label="t('nav.mobile')">
      <RouterLink
        v-for="item in navigation"
        :key="item.name"
        :to="{ name: item.name }"
        :class="{ active: isActive(item) }"
      >
        <component :is="item.icon" :size="20" />
        <span>{{ t(item.labelKey) }}</span>
      </RouterLink>
    </nav>
  </div>
</template>

<style scoped>
.app-shell { min-height: 100vh; }

.sidebar {
  position: fixed;
  inset: 0 auto 0 0;
  z-index: 20;
  display: flex;
  width: 246px;
  color: #dce6e2;
  background: var(--nav);
  border-right: 1px solid #2b3a35;
  flex-direction: column;
}

.sidebar-brand { padding: 25px 24px 23px; border-bottom: 1px solid rgb(255 255 255 / 9%); }
.primary-nav { padding: 20px 14px; }
.nav-section { display: grid; margin-bottom: 17px; gap: 4px; }
.nav-section:last-child { margin-bottom: 0; }
.nav-section-label { padding: 0 12px 6px; color: #667b73; font: 500 8px/1 'IBM Plex Mono', monospace; text-transform: uppercase; }

.nav-link {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 42px;
  padding: 0 12px;
  color: #9fb0a9;
  border: 1px solid transparent;
  border-radius: 4px;
  font-size: 13px;
  font-weight: 620;
}

.nav-link:hover { color: #fff; background: var(--nav-soft); }
.nav-link.active { color: #fff; background: #263630; border-color: #354840; }
.nav-link.active::before { width: 3px; height: 18px; margin-left: -13px; background: #55d0b3; content: ''; }

.sidebar-status {
  margin: auto 20px 12px;
  padding: 14px 0;
  color: #8da098;
  border-block: 1px solid rgb(255 255 255 / 9%);
  font: 400 10px/1.4 'IBM Plex Mono', monospace;
}

.sidebar-language { padding: 0 16px 14px; }
.sidebar-language :deep(.language-switcher) { width: 100%; justify-content: space-between; }

.status-line, .status-value { display: flex; align-items: center; gap: 7px; }
.status-value { margin-top: 8px; color: #dce6e2; }
.status-value i, .context-state i { width: 6px; height: 6px; background: #45c9a7; border-radius: 50%; box-shadow: 0 0 0 4px rgb(69 201 167 / 12%); }

.user-block { display: flex; align-items: center; gap: 10px; padding: 18px 16px; background: rgb(0 0 0 / 12%); }
.avatar { display: grid; width: 32px; height: 32px; color: #fff; background: var(--blue); border-radius: 4px; font: 650 11px/1 'IBM Plex Mono', monospace; place-items: center; }
.user-copy { display: grid; min-width: 0; flex: 1; }
.user-copy strong { overflow: hidden; color: #fff; font-size: 12px; text-overflow: ellipsis; white-space: nowrap; }
.user-copy small { overflow: hidden; color: #81938c; font-size: 10px; text-overflow: ellipsis; white-space: nowrap; }

.shell-content { min-height: 100vh; margin-left: 246px; }
.context-bar { display: flex; height: 48px; padding: 0 38px; background: rgb(255 255 255 / 78%); border-bottom: 1px solid var(--line); backdrop-filter: blur(12px); align-items: center; justify-content: space-between; }
.context-title { color: var(--ink-soft); font-size: 12px; font-weight: 650; }
.context-state { display: inline-flex; align-items: center; gap: 8px; color: var(--muted); font: 400 10px/1 'IBM Plex Mono', monospace; }
.mobile-header, .mobile-nav { display: none; }

@media (max-width: 900px) {
  .sidebar, .context-bar { display: none; }
  .shell-content { margin-left: 0; }
  .mobile-header { position: sticky; top: 0; z-index: 15; display: flex; height: 62px; padding: 0 18px; background: rgb(255 255 255 / 92%); border-bottom: 1px solid var(--line); backdrop-filter: blur(12px); align-items: center; justify-content: space-between; }
  .mobile-user { display: flex; align-items: center; gap: 6px; }
  .mobile-user :deep(.language-switcher) { height: 32px; }
  .mobile-logout { display: grid; width: 34px; height: 34px; padding: 0; color: var(--muted); background: transparent; border: 0; place-items: center; }
  .mobile-nav { position: fixed; inset: auto 0 0; z-index: 30; display: grid; height: 68px; padding-bottom: env(safe-area-inset-bottom); background: rgb(20 32 29 / 96%); border-top: 1px solid #31423c; grid-template-columns: repeat(4, 1fr); }
  .mobile-nav a { display: grid; color: #91a39c; font-size: 10px; place-items: center; align-content: center; gap: 4px; }
  .mobile-nav a.active { color: #fff; }
  .mobile-nav a.active svg { color: #55d0b3; }
}
</style>
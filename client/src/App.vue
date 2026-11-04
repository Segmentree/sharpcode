<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { locale, setLocale, SUPPORTED_LOCALES, type Locale } from './locale'
import { ADMIN_UI_ENABLED, isAdmin } from './admin'

const { t } = useI18n()

function choose(l: Locale) {
  setLocale(l)
}
</script>

<template>
  <div class="layout">
    <header class="topbar">
      <RouterLink to="/" class="brand"><span class="logo">#</span> {{ t('app.name') }}</RouterLink>
      <nav>
        <RouterLink to="/">{{ t('nav.problems') }}</RouterLink>
        <RouterLink to="/sandbox">{{ t('nav.sandbox') }}</RouterLink>
        <RouterLink v-if="isAdmin" to="/problems/new">{{ t('nav.newProblem') }}</RouterLink>
      </nav>
      <span class="spacer"></span>
      <RouterLink v-if="ADMIN_UI_ENABLED" to="/admin" class="admin-link" :class="{ on: isAdmin }">
        {{ isAdmin ? t('nav.adminSignedIn') : t('nav.admin') }}
      </RouterLink>
      <div class="lang" role="group" :aria-label="t('nav.language')">
        <button
          v-for="l in SUPPORTED_LOCALES"
          :key="l"
          type="button"
          class="sm"
          :class="{ active: locale === l }"
          :aria-pressed="locale === l"
          @click="choose(l)"
        >
          {{ l.toUpperCase() }}
        </button>
      </div>
    </header>
    <main class="content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.layout { height: 100%; display: flex; flex-direction: column; }
.topbar {
  display: flex; align-items: center; gap: 1.25rem;
  padding: 0 1.25rem; height: 52px;
  border-bottom: 1px solid var(--border); background: var(--panel);
  flex-shrink: 0;
}
.brand { font-weight: 700; color: var(--text); font-size: 1.05rem; display: flex; align-items: center; gap: 0.4rem; white-space: nowrap; }
.brand:hover { text-decoration: none; }
.logo {
  display: inline-grid; place-items: center; width: 26px; height: 26px; border-radius: 7px;
  background: var(--accent-strong); color: #fff; font-family: var(--mono); font-weight: 700;
}
nav { display: flex; gap: 1rem; }
nav a { color: var(--muted); white-space: nowrap; }
nav a.router-link-active { color: var(--text); }
.admin-link { color: var(--muted); font-size: 0.9rem; white-space: nowrap; }
.admin-link.on { color: var(--green); }
.lang { display: flex; gap: 0.25rem; }
.lang button { padding: 0.2rem 0.5rem; font-size: 0.78rem; }
.lang button.active { background: var(--accent-strong); border-color: var(--accent-strong); color: #fff; }
.content { flex: 1; min-height: 0; overflow: auto; }

@media (max-width: 640px) {
  .topbar { gap: 0.75rem; padding: 0 1rem; }
  nav { gap: 0.6rem; font-size: 0.9rem; }
}
</style>

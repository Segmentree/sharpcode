<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { signOut, user } from '../auth'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const open = ref(false)

function initials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0]?.toUpperCase() ?? '')
    .join('')
}

function doSignOut() {
  open.value = false
  signOut()
  if (route.meta.auth) router.push('/')
}
</script>

<template>
  <RouterLink v-if="!user" :to="{ name: 'login', query: route.name === 'login' ? route.query : { redirect: route.fullPath } }" class="signin">
    {{ t('auth.signIn') }}
  </RouterLink>
  <div v-else class="menu" @focusout="(e) => { if (!(e.currentTarget as HTMLElement).contains(e.relatedTarget as Node)) open = false }">
    <button type="button" class="trigger" :aria-expanded="open" :title="user.email" @click="open = !open">
      <img v-if="user.picture" :src="user.picture" alt="" class="avatar" referrerpolicy="no-referrer" />
      <span v-else class="avatar fallback">{{ initials(user.name) }}</span>
      <span class="name">{{ user.name }}</span>
    </button>
    <div v-if="open" class="dropdown panel">
      <div class="who">
        <div class="name">{{ user.name }}</div>
        <div class="muted small">{{ user.email }}</div>
      </div>
      <button type="button" class="sm" @click="doSignOut">{{ t('auth.signOut') }}</button>
    </div>
  </div>
</template>

<style scoped>
.signin { color: var(--text); font-size: 0.9rem; white-space: nowrap; padding: 0.3rem 0.75rem; border: 1px solid var(--border); border-radius: 999px; }
.signin:hover { text-decoration: none; border-color: var(--accent); }
.menu { position: relative; }
.trigger { display: flex; align-items: center; gap: 0.5rem; background: transparent; border: 0; padding: 0.2rem 0.3rem; color: var(--text); border-radius: 999px; }
.trigger:hover { background: var(--panel-2); }
.avatar { width: 28px; height: 28px; border-radius: 50%; object-fit: cover; }
.avatar.fallback { display: grid; place-items: center; background: var(--accent-strong); color: #fff; font-size: 0.75rem; font-weight: 700; }
.trigger .name { font-size: 0.9rem; max-width: 10rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.dropdown { position: absolute; right: 0; top: calc(100% + 6px); min-width: 220px; padding: 0.75rem; display: flex; flex-direction: column; gap: 0.6rem; z-index: 20; }
.who .name { font-weight: 600; }
.small { font-size: 0.8rem; }

@media (max-width: 640px) {
  .trigger .name { display: none; }
}
</style>

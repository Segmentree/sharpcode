<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { api } from '../api'
import { isAdmin, setAdminToken, clearAdminToken } from '../admin'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const token = ref('')
const checking = ref(false)
const error = ref<string | null>(null)

async function signIn() {
  const value = token.value.trim()
  if (!value) return
  checking.value = true
  error.value = null
  try {
    await api.checkAdmin(value)
    setAdminToken(value)
    token.value = ''
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : null
    if (redirect) router.push(redirect)
  } catch {
    error.value = t('admin.invalid')
  } finally {
    checking.value = false
  }
}

function signOut() {
  clearAdminToken()
}
</script>

<template>
  <div class="page">
    <h1>{{ t('admin.title') }}</h1>

    <section v-if="isAdmin" class="panel block">
      <p class="ok">{{ t('admin.signedIn') }}</p>
      <div class="row">
        <RouterLink class="btn primary" to="/problems/new">{{ t('admin.goNew') }}</RouterLink>
        <button type="button" @click="signOut">{{ t('admin.signOut') }}</button>
      </div>
    </section>

    <form v-else class="panel block" @submit.prevent="signIn">
      <p class="muted">{{ t('admin.intro') }}</p>
      <p v-if="route.query.redirect" class="muted small">{{ t('admin.needAdmin') }}</p>
      <label for="admin-token">{{ t('admin.token') }}</label>
      <input id="admin-token" v-model="token" type="password" autocomplete="off" required />
      <p v-if="error" class="error">{{ error }}</p>
      <div class="row">
        <span class="spacer"></span>
        <button type="submit" class="primary" :disabled="checking || !token.trim()">
          {{ checking ? t('admin.checking') : t('admin.signIn') }}
        </button>
      </div>
    </form>
  </div>
</template>

<style scoped>
.page { max-width: 560px; margin: 0 auto; padding: 1.5rem 1rem; }
.block { padding: 1rem 1.1rem; display: flex; flex-direction: column; gap: 0.6rem; }
.ok { color: var(--green); margin: 0; }
.small { font-size: 0.82rem; }
</style>

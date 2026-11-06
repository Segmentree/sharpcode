<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { googleClientId, renderGoogleButton } from '../auth'
import { locale } from '../locale'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const buttonHost = ref<HTMLElement | null>(null)
const error = ref<string | null>(null)
const loading = ref(true)

function redirectTarget(): string {
  const r = route.query.redirect
  return typeof r === 'string' && r.startsWith('/') ? r : '/'
}

async function mountButton() {
  if (!buttonHost.value || !googleClientId.value) {
    loading.value = false
    return
  }
  loading.value = true
  error.value = null
  try {
    await renderGoogleButton(buttonHost.value, locale.value, (err) => {
      if (err) error.value = err
      else router.replace(redirectTarget())
    })
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    loading.value = false
  }
}

onMounted(mountButton)
// The Google button is rendered in the UI language; re-render on change.
watch(locale, mountButton)
</script>

<template>
  <div class="login">
    <div class="card panel">
      <div class="logo">#</div>
      <h2>{{ t('auth.title') }}</h2>
      <p class="muted">{{ t('auth.intro') }}</p>
      <ul class="muted small perks">
        <li>{{ t('auth.perkSubmit') }}</li>
        <li>{{ t('auth.perkDrafts') }}</li>
        <li>{{ t('auth.perkSandbox') }}</li>
      </ul>

      <p v-if="googleClientId === ''" class="error">{{ t('auth.notConfigured') }}</p>
      <template v-else>
        <div ref="buttonHost" class="google-button"></div>
        <p v-if="loading" class="muted small">{{ t('app.loading') }}</p>
        <p v-if="error" class="error">{{ error }}</p>
      </template>
    </div>
  </div>
</template>

<style scoped>
.login { min-height: 100%; display: grid; place-items: center; padding: 2rem 1rem; }
.card { width: min(440px, 100%); padding: 2rem; text-align: center; display: flex; flex-direction: column; align-items: center; gap: 0.6rem; }
.logo {
  display: grid; place-items: center; width: 48px; height: 48px; border-radius: 12px;
  background: var(--accent-strong); color: #fff; font-family: var(--mono); font-weight: 700; font-size: 1.5rem;
}
h2 { margin: 0.25rem 0 0; }
p { margin: 0; }
.perks { text-align: left; margin: 0.5rem 0 0.75rem; padding-left: 1.2rem; display: flex; flex-direction: column; gap: 0.25rem; }
.small { font-size: 0.85rem; }
/* The Google button is a cross-origin iframe with a light colour scheme. Browsers paint such an iframe opaque
   white when the embedding element's scheme is dark, so the host opts back into light to keep it transparent. */
.google-button { min-height: 44px; display: flex; justify-content: center; color-scheme: light; }
.google-button :deep(iframe) { color-scheme: light; }
</style>

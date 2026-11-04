import { ref } from 'vue'

export type Locale = 'en' | 'es'
export const SUPPORTED_LOCALES: Locale[] = ['en', 'es']
const STORAGE_KEY = 'sharpcode:locale'

function readStored(): Locale {
  try {
    const v = localStorage.getItem(STORAGE_KEY)
    if (v === 'en' || v === 'es') return v
  } catch {
    /* storage unavailable */
  }
  return 'en'
}

/** Current UI + content locale. Shared by vue-i18n and the API client. */
export const locale = ref<Locale>(readStored())

export function setLocale(next: Locale) {
  if (!SUPPORTED_LOCALES.includes(next)) return
  locale.value = next
  try {
    localStorage.setItem(STORAGE_KEY, next)
  } catch {
    /* ignore */
  }
  document.documentElement.lang = next
}

import { createI18n } from 'vue-i18n'
import { watch } from 'vue'
import { en, type MessageSchema } from './en'
import { es } from './es'
import { locale, type Locale } from '../locale'

export const i18n = createI18n<[MessageSchema], Locale, false>({
  legacy: false,
  locale: locale.value,
  fallbackLocale: 'en',
  messages: { en, es },
})

// Keep vue-i18n in sync with the shared locale ref (the ref is the single source of truth).
watch(locale, (l) => {
  i18n.global.locale.value = l
})
document.documentElement.lang = locale.value

import { computed, ref } from 'vue'

const STORAGE_KEY = 'sharpcode:adminToken'

function readStored(): string {
  try {
    return localStorage.getItem(STORAGE_KEY) ?? ''
  } catch {
    return ''
  }
}

/**
 * Problem authoring is a local-only workflow: the admin page, the problem editor and the edit/delete controls
 * exist in development builds and are compiled out of production builds entirely.
 */
export const ADMIN_UI_ENABLED: boolean = import.meta.env.DEV

/** Admin token for this browser. Empty string means not signed in. */
export const adminToken = ref<string>(ADMIN_UI_ENABLED ? readStored() : '')
export const isAdmin = computed(() => ADMIN_UI_ENABLED && adminToken.value.length > 0)

export function setAdminToken(token: string) {
  adminToken.value = token
  try {
    if (token) localStorage.setItem(STORAGE_KEY, token)
    else localStorage.removeItem(STORAGE_KEY)
  } catch {
    /* ignore */
  }
}

export function clearAdminToken() {
  setAdminToken('')
}

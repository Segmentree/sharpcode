<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <div class="layout">
    <header class="topbar">
      <RouterLink to="/" class="brand"><span class="logo">#</span> SharpCode</RouterLink>
      <nav>
        <RouterLink to="/">Problems</RouterLink>
        <RouterLink to="/problems/new">New problem</RouterLink>
      </nav>
    </header>
    <main class="content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.layout { height: 100%; display: flex; flex-direction: column; }
.topbar {
  display: flex; align-items: center; gap: 1.5rem;
  padding: 0 1.25rem; height: 52px;
  border-bottom: 1px solid var(--border); background: var(--panel);
  flex-shrink: 0;
}
.brand { font-weight: 700; color: var(--text); font-size: 1.05rem; display: flex; align-items: center; gap: 0.4rem; }
.brand:hover { text-decoration: none; }
.logo {
  display: inline-grid; place-items: center; width: 26px; height: 26px; border-radius: 7px;
  background: var(--accent-strong); color: #fff; font-family: var(--mono); font-weight: 700;
}
nav { display: flex; gap: 1rem; }
nav a { color: var(--muted); }
nav a.router-link-active { color: var(--text); }
.content { flex: 1; min-height: 0; overflow: auto; }
</style>

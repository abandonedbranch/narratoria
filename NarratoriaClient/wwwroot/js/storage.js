function resolveStore(area) {
  if (area === 'session') {
    return window.sessionStorage;
  }

  return window.localStorage;
}

export function getItem(area, key) {
  const store = resolveStore(area);
  return store.getItem(key);
}

export function setItem(area, key, value) {
  const store = resolveStore(area);

  if (value === undefined || value === null) {
    store.removeItem(key);
    return;
  }

  store.setItem(key, value);
}

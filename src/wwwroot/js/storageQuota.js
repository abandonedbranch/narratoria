export async function estimate() {
  if (typeof window !== "undefined" && window.isSecureContext === false) {
    throw new Error("NotSupported: insecure context");
  }

  if (typeof navigator === "undefined" || !navigator.storage || typeof navigator.storage.estimate !== "function") {
    throw new Error("NotSupported: StorageManager unavailable");
  }

  const result = await navigator.storage.estimate();
  const usage = typeof result?.usage === "number" ? result.usage : null;
  const quota = typeof result?.quota === "number" ? result.quota : null;
  const source = result?.usageDetails?.quotaSource ?? "storage-manager";

  return { usage, quota, source };
}

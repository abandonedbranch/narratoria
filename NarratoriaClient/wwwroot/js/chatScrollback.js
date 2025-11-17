export function scrollToBottom(element, smooth) {
  if (!element) {
    return;
  }

  const behavior = smooth ? 'smooth' : 'auto';

  try {
    element.scrollTo({
      top: element.scrollHeight ?? 0,
      behavior
    });
  } catch {
    // Fallback for browsers without scrollTo options support.
    element.scrollTop = element.scrollHeight ?? 0;
  }
}

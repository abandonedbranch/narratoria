export function attachReplyEditor(element, dotNetRef) {
  if (!element) {
    return {
      dispose: () => {}
    };
  }

  const handler = (event) => {
    if (event.key !== 'Enter') {
      return;
    }

    if (event.metaKey || event.ctrlKey) {
      // Allow default behavior to insert a newline.
      return;
    }

    event.preventDefault();
    dotNetRef?.invokeMethodAsync('SubmitFromKeyboardAsync');
  };

  element.addEventListener('keydown', handler);

  return {
    dispose: () => {
      element.removeEventListener('keydown', handler);
    }
  };
}

(() => {
  const composerSelectors = [
    '#prompt-textarea',
    '#mobile-composer-prompt',
    'textarea[name="prompt"]',
    '[data-lexical-editor="true"]',
    'form [contenteditable="true"]'
  ];
  const sendButtonSelectors = [
    '[data-testid="send-button"]',
    '#composer-submit-button',
    'button[data-composer-submit]'
  ];
  const stopButtonSelectors = ['[data-testid="stop-button"]'];
  const assistantMessageSelector = '[data-message-role="assistant"], [data-message-author-role="assistant"]';
  const userMessageSelector = '[data-message-role="user"], [data-message-author-role="user"]';
  const answerContentSelectors = ['[data-assistant-markdown-root]', '.markdown'];

  const findFirst = (selectors, root = document) => {
    for (const selector of selectors) {
      const element = root.querySelector(selector);
      if (element) {
        return element;
      }
    }
    return null;
  };

  const composer = () => findFirst(composerSelectors);

  const sendButton = () => {
    const button = findFirst(sendButtonSelectors);
    if (button) {
      return button;
    }
    const form = composer()?.closest('form');
    return form ? form.querySelector('button[type="submit"]') : null;
  };

  const assistantMessages = () => document.querySelectorAll(assistantMessageSelector);

  const latestAssistantMessage = () => {
    const messages = assistantMessages();
    return messages.length > 0 ? messages[messages.length - 1] : null;
  };

  const answerText = message => {
    if (!message) {
      return '';
    }
    return (findFirst(answerContentSelectors, message) ?? message).innerText.trim();
  };

  const isGenerating = () => {
    if (findFirst(stopButtonSelectors)) {
      return true;
    }
    const message = latestAssistantMessage();
    return !!message && message.hasAttribute('data-message-role') && !message.hasAttribute('data-message-complete');
  };

  const setMessage = text => {
    const element = composer();
    if (!element) {
      return false;
    }
    element.focus();
    if (element instanceof HTMLTextAreaElement) {
      const valueSetter = Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype, 'value').set;
      valueSetter.call(element, text);
      element.dispatchEvent(new Event('input', { bubbles: true }));
    } else {
      document.execCommand('selectAll', false);
      document.execCommand('insertText', false, text);
    }
    return true;
  };

  const send = () => {
    const button = sendButton();
    if (!button || button.disabled) {
      return false;
    }
    button.click();
    return true;
  };

  const status = () => {
    const button = sendButton();
    return {
      hasComposer: !!composer(),
      canSend: !!button && !button.disabled,
      isGenerating: isGenerating(),
      userMessageCount: document.querySelectorAll(userMessageSelector).length,
      assistantMessageCount: assistantMessages().length,
      latestAnswer: answerText(latestAssistantMessage())
    };
  };

  window.voiceAssistentChatGpt = { status, setMessage, send };
})();

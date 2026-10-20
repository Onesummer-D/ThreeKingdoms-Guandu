// Native browser input for the historian's free-form question in Unity WebGL.
// A real textarea supports compositionstart/compositionend (Chinese/Japanese IME)
// without relying on TMP_InputField or Unity's legacy WebGL keyboard events.
mergeInto(LibraryManager.library, {
  GuanduHistorianInputOpen: function(textPtr, x, y, width, height, screenW, screenH) {
    if (window.__guanduHistorianInput) {
      window.__guanduHistorianInput.dispose();
    }
    var canvas = document.getElementById('unity-canvas') || document.querySelector('canvas');
    if (!canvas) return;
    var textarea = document.createElement('textarea');
    var state = {
      element: textarea,
      composing: false,
      changed: false,
      submitted: false,
      cancelled: false,
      suppressEnterUntil: 0,
      dispose: null
    };
    window.__guanduHistorianInput = state;
    textarea.value = UTF8ToString(textPtr);
    textarea.maxLength = 800;
    textarea.rows = 2;
    textarea.placeholder = '想问些什么？输入问题…';
    textarea.setAttribute('aria-label', '向史官提问');
    textarea.setAttribute('spellcheck', 'false');
    textarea.setAttribute('autocomplete', 'off');
    textarea.style.cssText = [
      'position:fixed', 'z-index:2147483000', 'box-sizing:border-box',
      'background:rgba(7,18,24,.96)', 'color:#f2ead9', 'border:0',
      'border-radius:0', 'outline:1px solid rgba(207,167,96,.2)',
      'font-family:"Microsoft YaHei","Noto Sans CJK SC",sans-serif',
      'font-size:18px', 'line-height:1.45', 'padding:2px 4px',
      'resize:none', 'overflow:auto', 'text-align:left', 'white-space:pre-wrap',
      'pointer-events:auto'
    ].join(';');
    var place = function () {
      var rect = canvas.getBoundingClientRect();
      if (rect.width === 0 || rect.height === 0) return;
      var sx = rect.width / Math.max(1, screenW);
      var sy = rect.height / Math.max(1, screenH);
      textarea.style.left = (rect.left + x * sx) + 'px';
      textarea.style.top = (rect.top + (screenH - y - height) * sy) + 'px';
      textarea.style.width = (width * sx) + 'px';
      textarea.style.height = (height * sy) + 'px';
      textarea.style.fontSize = Math.max(15, Math.min(27, height * sy * .36)) + 'px';
    };
    var onInput = function () {
      if (!state.composing) state.changed = true;
    };
    var onCompositionStart = function () { state.composing = true; };
    var onCompositionEnd = function () {
      state.composing = false;
      state.changed = true;
      // Some browsers send the same Enter key after IME candidate selection.
      state.suppressEnterUntil = performance.now() + 130;
    };
    var onKeyDown = function (event) {
      if (event.key === 'Escape' && !state.composing && !event.isComposing) {
        event.preventDefault();
        state.cancelled = true;
      }
      if (event.key === 'Enter' && !event.shiftKey &&
          !state.composing && !event.isComposing && event.keyCode !== 229) {
        event.preventDefault();
        if (performance.now() >= state.suppressEnterUntil) {
          state.changed = true;
          state.submitted = true;
        }
      }
    };
    textarea.addEventListener('input', onInput);
    textarea.addEventListener('compositionstart', onCompositionStart);
    textarea.addEventListener('compositionend', onCompositionEnd);
    textarea.addEventListener('keydown', onKeyDown);
    window.addEventListener('resize', place);
    document.addEventListener('fullscreenchange', place);
    document.body.appendChild(textarea);
    state.dispose = function () {
      window.removeEventListener('resize', place);
      document.removeEventListener('fullscreenchange', place);
      textarea.removeEventListener('input', onInput);
      textarea.removeEventListener('compositionstart', onCompositionStart);
      textarea.removeEventListener('compositionend', onCompositionEnd);
      textarea.removeEventListener('keydown', onKeyDown);
      textarea.remove();
      if (window.__guanduHistorianInput === state)
        window.__guanduHistorianInput = null;
    };
    place();
    textarea.focus();
  },
  GuanduHistorianInputChanged: function () {
    var state = window.__guanduHistorianInput;
    if (!state || !state.changed) return 0;
    state.changed = false;
    return 1;
  },
  GuanduHistorianInputRead: function () {
    var state = window.__guanduHistorianInput;
    return stringToNewUTF8(state ? state.element.value : '');
  },
  GuanduHistorianInputWasSubmitted: function () {
    var state = window.__guanduHistorianInput;
    if (!state || !state.submitted) return 0;
    state.submitted = false;
    return 1;
  },
  GuanduHistorianInputWasCancelled: function () {
    var state = window.__guanduHistorianInput;
    if (!state || !state.cancelled) return 0;
    state.cancelled = false;
    return 1;
  },
  GuanduHistorianInputSetText: function (textPtr) {
    var state = window.__guanduHistorianInput;
    if (!state) return;
    state.element.value = UTF8ToString(textPtr);
    state.changed = false;
    state.submitted = false;
  },
  GuanduHistorianInputClose: function () {
    if (window.__guanduHistorianInput)
      window.__guanduHistorianInput.dispose();
  }
});

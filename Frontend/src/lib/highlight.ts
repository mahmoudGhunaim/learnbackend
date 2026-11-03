import hljs from 'highlight.js/lib/core';
import bash from 'highlight.js/lib/languages/bash';
import csharp from 'highlight.js/lib/languages/csharp';
import css from 'highlight.js/lib/languages/css';
import javascript from 'highlight.js/lib/languages/javascript';
import json from 'highlight.js/lib/languages/json';
import markdown from 'highlight.js/lib/languages/markdown';
import php from 'highlight.js/lib/languages/php';
import typescript from 'highlight.js/lib/languages/typescript';
import xml from 'highlight.js/lib/languages/xml';

// Only the languages we actually show: keeps the bundle small.
hljs.registerLanguage('csharp', csharp);
hljs.registerLanguage('javascript', javascript);
hljs.registerLanguage('typescript', typescript);
hljs.registerLanguage('php', php);
hljs.registerLanguage('xml', xml);
hljs.registerLanguage('json', json);
hljs.registerLanguage('bash', bash);
hljs.registerLanguage('css', css);
hljs.registerLanguage('markdown', markdown);

const aliases: Record<string, string> = { cs: 'csharp', 'c#': 'csharp', js: 'javascript', ts: 'typescript', html: 'xml', sh: 'bash', powershell: 'bash' };

function escapeHtml(text: string) {
  return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

/**
 * Highlights code and returns one HTML string per line, so line-based UIs
 * (line numbers, hover explanations, highlighted anchor lines) stay simple.
 * Spans that cross a newline (e.g. multi-line comments) are closed and re-opened per line.
 */
export function highlightLines(code: string, lang: string): string[] {
  const language = aliases[lang] ?? lang;
  let html: string;
  try {
    html = hljs.getLanguage(language) ? hljs.highlight(code, { language, ignoreIllegals: true }).value : escapeHtml(code);
  } catch {
    html = escapeHtml(code);
  }

  const lines: string[] = [];
  const open: string[] = [];
  let current = '';
  const tokenRe = /(<span[^>]*>)|(<\/span>)|(\n)|([^<\n]+|<)/g;
  let m: RegExpExecArray | null;
  while ((m = tokenRe.exec(html)) !== null) {
    if (m[1]) {
      open.push(m[1]);
      current += m[1];
    } else if (m[2]) {
      open.pop();
      current += m[2];
    } else if (m[3]) {
      current += '</span>'.repeat(open.length);
      lines.push(current);
      current = open.join('');
    } else {
      current += m[4];
    }
  }
  lines.push(current);
  return lines;
}

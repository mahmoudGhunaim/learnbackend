import Editor, { loader, type OnMount } from '@monaco-editor/react';
import * as monaco from 'monaco-editor';
import editorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker';
import { useEffect, useImperativeHandle, useRef, type Ref } from 'react';
import { useTheme } from '../i18n/I18nProvider';
import type { CodeDiagnostic } from '../lib/types';

// Bundle Monaco locally (works offline) instead of loading it from a CDN.
self.MonacoEnvironment = { getWorker: () => new editorWorker() };
loader.config({ monaco });

monaco.editor.defineTheme('lb-dark', {
  base: 'vs-dark',
  inherit: true,
  rules: [],
  colors: { 'editor.background': '#0f131c', 'editor.lineHighlightBackground': '#1a2030', 'editorGutter.background': '#0f131c' },
});
monaco.editor.defineTheme('lb-light', {
  base: 'vs',
  inherit: true,
  rules: [],
  colors: { 'editor.background': '#fbfbfd', 'editor.lineHighlightBackground': '#f0f0f6' },
});

export interface CodeEditorHandle {
  revealLine: (line: number) => void;
  focus: () => void;
}

interface Props {
  value: string;
  onChange: (value: string) => void;
  diagnostics?: CodeDiagnostic[];
  onRun?: () => void;
  ref?: Ref<CodeEditorHandle>;
}

export default function CodeEditor({ value, onChange, diagnostics = [], onRun, ref }: Props) {
  const { theme } = useTheme();
  const editorRef = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);
  const runRef = useRef(onRun);
  runRef.current = onRun;

  useImperativeHandle(ref, () => ({
    revealLine: (line: number) => {
      const editor = editorRef.current;
      if (!editor) return;
      editor.revealLineInCenter(line);
      editor.setPosition({ lineNumber: line, column: 1 });
      editor.focus();
    },
    focus: () => editorRef.current?.focus(),
  }));

  const onMount: OnMount = (editor) => {
    editorRef.current = editor;
    editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter, () => runRef.current?.());
  };

  // Roslyn diagnostics → red/yellow squiggles in the editor.
  useEffect(() => {
    const model = editorRef.current?.getModel();
    if (!model) return;
    const markers = diagnostics
      .filter((d) => d.line > 0 && d.source !== 'Tests')
      .map<monaco.editor.IMarkerData>((d) => ({
        startLineNumber: d.line,
        startColumn: d.column,
        endLineNumber: d.endLine,
        endColumn: d.endLine === d.line && d.endColumn <= d.column ? d.column + 1 : d.endColumn,
        message: `${d.id}: ${d.message}${d.messageAr ? `\n${d.messageAr}` : ''}`,
        severity: d.severity === 'error' ? monaco.MarkerSeverity.Error : monaco.MarkerSeverity.Warning,
      }));
    monaco.editor.setModelMarkers(model, 'roslyn', markers);
  }, [diagnostics]);

  return (
    <div dir="ltr" className="h-full min-h-0">
      <Editor
        value={value}
        onChange={(v) => onChange(v ?? '')}
        onMount={onMount}
        language="csharp"
        theme={theme === 'dark' ? 'lb-dark' : 'lb-light'}
        options={{
          fontFamily: "'JetBrains Mono', 'IBM Plex Sans Arabic', monospace",
          // Arabic comments: without this Monaco draws each Arabic letter in a fixed-width cell (disjointed).
          disableMonospaceOptimizations: true,
          fontSize: 14,
          fontLigatures: false,
          minimap: { enabled: false },
          scrollBeyondLastLine: false,
          tabSize: 4,
          automaticLayout: true,
          padding: { top: 12, bottom: 12 },
          renderLineHighlight: 'all',
          smoothScrolling: true,
          wordWrap: 'off',
          fixedOverflowWidgets: true,
        }}
      />
    </div>
  );
}

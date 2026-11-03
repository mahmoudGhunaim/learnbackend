import { useQuery } from '@tanstack/react-query';
import clsx from 'clsx';
import { FileCode2, FileJson, FileText, Folder } from 'lucide-react';
import { useState } from 'react';
import { CodeView } from '../components/CodeView';
import { useI18n } from '../i18n/I18nProvider';
import { api } from '../lib/api';

type Node = { path: string; depth: number; folder?: boolean; ar: string; en: string };

// A curated map of THIS repository. Files are loaded live from the API's source viewer.
const TREE: Node[] = [
  { path: 'LearnBackend.slnx', depth: 0, ar: 'ملف الحل: يجمع المشاريع الثلاثة.', en: 'The solution: groups the three projects.' },
  { path: 'dotnet-tools.json', depth: 0, ar: 'أدوات ‎.NET المحلية (dotnet-ef).', en: 'Local .NET tools (dotnet-ef).' },
  { path: 'Backend', depth: 0, folder: true, ar: 'كود الخادم.', en: 'Server code.' },
  { path: 'Backend/LearnBackend.Api', depth: 1, folder: true, ar: 'مشروع ASP.NET Core Web API.', en: 'The ASP.NET Core Web API project.' },
  { path: 'Backend/LearnBackend.Api/LearnBackend.Api.csproj', depth: 2, ar: 'ملف المشروع: الإطار والحزم والمراجع.', en: 'Project file: framework, packages, references.' },
  { path: 'Backend/LearnBackend.Api/Program.cs', depth: 2, ar: 'نقطة البداية: الخدمات والـ Middleware والـ Endpoints.', en: 'Entry point: services, middleware, endpoints.' },
  { path: 'Backend/LearnBackend.Api/appsettings.json', depth: 2, ar: 'الإعدادات الأساسية.', en: 'Base configuration.' },
  { path: 'Backend/LearnBackend.Api/appsettings.Development.json', depth: 2, ar: 'إعدادات بيئة التطوير، وتستبدل الأساسية.', en: 'Development overrides.' },
  { path: 'Backend/LearnBackend.Api/Properties/launchSettings.json', depth: 2, ar: 'إعدادات التشغيل المحلي (المنفذ والبيئة).', en: 'Local launch profile (URL, environment).' },
  { path: 'Backend/LearnBackend.Api/Controllers', depth: 2, folder: true, ar: 'نقاط نهاية بأسلوب Controllers.', en: 'Controller-style endpoints.' },
  { path: 'Backend/LearnBackend.Api/Controllers/GlossaryController.cs', depth: 3, ar: 'API القاموس.', en: 'Glossary API.' },
  { path: 'Backend/LearnBackend.Api/Endpoints', depth: 2, folder: true, ar: 'نقاط نهاية بأسلوب Minimal APIs.', en: 'Minimal API endpoints.' },
  { path: 'Backend/LearnBackend.Api/Endpoints/ExerciseEndpoints.cs', depth: 3, ar: 'التمارين وتشغيل الكود.', en: 'Exercises and the code runner.' },
  { path: 'Backend/LearnBackend.Api/CodeRunner', depth: 2, folder: true, ar: 'Roslyn والتحليل الأمني والعزل.', en: 'Roslyn, safety analysis, sandbox.' },
  { path: 'Backend/LearnBackend.Api/CodeRunner/CodeCompiler.cs', depth: 3, ar: 'ترجمة كودك بـ Roslyn.', en: 'Compiles your code with Roslyn.' },
  { path: 'Backend/LearnBackend.Api/Data/AppDbContext.cs', depth: 2, ar: 'EF Core DbContext لحفظ تقدمك.', en: 'EF Core DbContext storing your progress.' },
  { path: 'Backend/LearnBackend.Sandbox/Program.cs', depth: 1, ar: 'عامل العزل: يشغّل كودك في عملية منفصلة.', en: 'Sandbox worker: runs your code in a separate process.' },
  { path: 'Backend/LearnBackend.Tests/ExerciseValidationTests.cs', depth: 1, ar: 'اختبارات xUnit تتحقق من كل تمرين.', en: 'xUnit tests validating every exercise.' },
  { path: 'content/curriculum.json', depth: 0, ar: 'ترتيب المراحل والدروس.', en: 'Phase and lesson order.' },
  { path: 'Frontend/package.json', depth: 0, ar: 'الواجهة: React + Vite (للمقارنة مع ‎.csproj).', en: 'The React + Vite frontend (compare with .csproj).' },
];

function iconFor(node: Node) {
  if (node.folder) return Folder;
  if (node.path.endsWith('.json')) return FileJson;
  if (node.path.endsWith('.cs')) return FileCode2;
  return FileText;
}

export function ProjectExplorer() {
  const { lang } = useI18n();
  const [selected, setSelected] = useState('Backend/LearnBackend.Api/LearnBackend.Api.csproj');
  const node = TREE.find((n) => n.path === selected)!;
  const { data, isLoading, error } = useQuery({
    queryKey: ['source', selected, 'full'],
    queryFn: () => api.source(selected),
    enabled: !node.folder,
    staleTime: 60_000,
  });

  return (
    <div className="grid gap-3 lg:grid-cols-[18rem_minmax(0,1fr)]">
      <ul dir="ltr" className="max-h-[28rem] overflow-y-auto rounded-xl border border-line bg-surface-2/50 p-1.5 text-sm">
        {TREE.map((n) => {
          const Icon = iconFor(n);
          return (
            <li key={n.path}>
              <button
                onClick={() => setSelected(n.path)}
                style={{ paddingLeft: `${n.depth * 14 + 8}px` }}
                className={clsx(
                  'flex w-full items-center gap-1.5 rounded-md py-1 pe-2 text-start font-mono text-[12.5px]',
                  selected === n.path ? 'bg-accent text-white' : 'hover:bg-surface-3',
                )}
              >
                <Icon className={clsx('size-3.5 shrink-0', selected !== n.path && (n.folder ? 'text-warn' : 'text-info'))} />
                <span className="truncate">{n.path.split('/').pop()}</span>
              </button>
            </li>
          );
        })}
      </ul>
      <div className="min-w-0 space-y-2">
        <p className="rounded-xl bg-surface-2 px-4 py-2.5 text-[15px]">
          <code dir="ltr" className="me-2 text-xs text-muted">
            /{node.path}
          </code>
          {lang === 'ar' ? node.ar : node.en}
        </p>
        {node.folder ? null : isLoading ? (
          <p className="p-4 text-sm text-muted">…</p>
        ) : error ? (
          <p className="p-4 text-sm text-bad">{String(error)}</p>
        ) : data ? (
          <CodeView code={data.lines.join('\n')} lang={data.language} maxHeight={420} />
        ) : null}
      </div>
    </div>
  );
}

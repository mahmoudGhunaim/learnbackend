import clsx from 'clsx';
import { ArrowLeft, ArrowRight } from 'lucide-react';
import { useState } from 'react';
import { useI18n } from '../i18n/I18nProvider';
import { CodeView } from '../components/CodeView';

type Stage = {
  id: string;
  title: string;
  when: 'build' | 'run';
  ar: string;
  en: string;
  code?: { lang: string; text: string };
};

const STAGES: Stage[] = [
  {
    id: 'source',
    title: 'Program.cs',
    when: 'build',
    ar: 'تكتب كود C# في ملفات ‎.cs. هذا نص فقط، والحاسوب لا يفهمه بعد.',
    en: 'You write C# in .cs files: just text the computer cannot run yet.',
    code: { lang: 'csharp', text: 'static int Add(int a, int b) => a + b;\n\nConsole.WriteLine(Add(2, 3));' },
  },
  {
    id: 'roslyn',
    title: 'Roslyn',
    when: 'build',
    ar: 'المترجم Roslyn (يستدعيه dotnet build عبر MSBuild) يحلل الكود ويتحقق من الأنواع ويُظهر الأخطاء، ثم يولّد IL. هذا التطبيق يستدعي Roslyn برمجياً لترجمة تمارينك!',
    en: 'Roslyn (run by dotnet build via MSBuild) parses, type-checks, reports errors and emits IL. This app calls Roslyn from code to compile your exercises!',
  },
  {
    id: 'il',
    title: 'IL (.dll)',
    when: 'build',
    ar: 'الناتج ملف ‎.dll (Assembly) يحتوي لغة وسيطة IL مستقلة عن المعالج، وبيانات وصفية عن كل الأنواع. نفس الملف يعمل على Windows و Linux و macOS.',
    en: 'The output is a .dll (assembly) holding CPU-independent IL plus type metadata. The same file runs on Windows, Linux and macOS.',
    code: { lang: 'bash', text: '.method static int32 Add(int32 a, int32 b)\n{\n  ldarg.0   // push a\n  ldarg.1   // push b\n  add       // a + b\n  ret\n}' },
  },
  {
    id: 'clr',
    title: 'CLR',
    when: 'run',
    ar: 'عند dotnet run يبدأ الـ CLR: يحمّل الـ Assembly، ويتحقق من سلامة الأنواع، ويجهّز جامع القمامة (GC) ومعالجة الاستثناءات.',
    en: 'On dotnet run the CLR starts: loads the assembly, verifies type safety, sets up the GC and exception handling.',
  },
  {
    id: 'jit',
    title: 'JIT',
    when: 'run',
    ar: 'أول مرة تُستدعى فيها الدالة يترجمها الـ JIT من IL إلى كود آلة للمعالج الحالي (x64 أو ARM64). Tiered compilation: ترجمة سريعة أولاً، ثم إعادة ترجمة محسّنة للدوال الأكثر استخداماً.',
    en: 'The first call of a method makes the JIT compile its IL into native code for this CPU. Tiered: fast first, then re-optimized when hot.',
  },
  {
    id: 'native',
    title: 'x64 / ARM64',
    when: 'run',
    ar: 'كود آلة حقيقي يعمل مباشرة على المعالج، بنفس سرعة اللغات المترجمة تقريباً.',
    en: 'Real machine code running directly on the CPU, at near-native speed.',
    code: { lang: 'bash', text: 'Add(Int32, Int32):\n    lea  eax, [rcx+rdx]\n    ret' },
  },
];

export function BuildPipeline() {
  const { lang } = useI18n();
  const [selected, setSelected] = useState('source');
  const [aot, setAot] = useState(false);
  const stage = STAGES.find((s) => s.id === selected)!;
  const Arrow = lang === 'ar' ? ArrowLeft : ArrowRight;

  return (
    <div className="space-y-4">
      <label className="flex w-fit cursor-pointer items-center gap-2 rounded-lg border border-line px-3 py-1.5 text-sm">
        <input type="checkbox" checked={aot} onChange={(e) => setAot(e.target.checked)} className="accent-[var(--accent)]" />
        Native AOT
      </label>

      <div className="flex flex-wrap items-stretch gap-1.5">
        {STAGES.map((s, i) => {
          const atBuild = s.when === 'build' || (aot && (s.id === 'jit' || s.id === 'native'));
          const skipped = aot && s.id === 'clr';
          return (
            <div key={s.id} className="flex items-center gap-1.5">
              <button
                onClick={() => setSelected(s.id)}
                className={clsx(
                  'min-w-24 rounded-xl border px-3 py-2 text-start transition-all',
                  selected === s.id ? 'border-accent bg-accent-soft shadow-sm' : 'border-line bg-surface hover:border-accent/50',
                  skipped && 'opacity-40',
                )}
              >
                <div className="font-mono text-sm font-semibold" dir="ltr">
                  {s.id === 'jit' && aot ? 'AOT compiler' : s.title}
                </div>
                <div className={clsx('text-[11px]', atBuild ? 'text-warn' : 'text-info')}>
                  {atBuild ? (lang === 'ar' ? 'وقت البناء' : 'build time') : lang === 'ar' ? 'وقت التشغيل' : 'run time'}
                </div>
              </button>
              {i < STAGES.length - 1 && <Arrow className="size-4 shrink-0 text-muted" />}
            </div>
          );
        })}
      </div>

      <div key={stage.id + String(aot)} className="animate-fade-up grid gap-3 md:grid-cols-2">
        <p className="rounded-xl bg-surface-2 p-4 text-[15px] leading-8">
          {aot && stage.id === 'jit'
            ? lang === 'ar'
              ? 'مع Native AOT تحدث هذه الترجمة مرة واحدة عند dotnet publish، فيبدأ التطبيق فوراً ولا يحتاج Runtime على الجهاز، لكن مع قيود على Reflection الديناميكي.'
              : 'With Native AOT this compilation happens once at dotnet publish: instant startup, no runtime needed, but limits on dynamic reflection.'
            : lang === 'ar'
              ? stage.ar
              : stage.en}
        </p>
        {stage.code && <CodeView code={stage.code.text} lang={stage.code.lang} lineNumbers={false} />}
      </div>
    </div>
  );
}

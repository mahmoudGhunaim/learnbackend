import clsx from 'clsx';
import { useState } from 'react';
import { useI18n } from '../i18n/I18nProvider';
import { highlightLines } from '../lib/highlight';
import { StepControls } from './Visual';

type StackVar = { name: string; type: string; value?: string; ref?: number };
type HeapObj = { id: number; type: string; fields: Record<string, string> };
type State = { stack: StackVar[]; heap: HeapObj[]; changed?: string; note: { ar: string; en: string } };

const CODE = [
  'int a = 5;',
  'int b = a;',
  'b = 10;',
  'var p1 = new Point { X = 1 };',
  'var p2 = p1;',
  'p2.X = 99;',
];

// One state per executed line (index 0 = before anything runs).
const STATES: State[] = [
  { stack: [], heap: [], note: { ar: 'لم يُنفّذ أي سطر بعد. اضغط «التالي».', en: 'Nothing has run yet. Press Next.' } },
  {
    stack: [{ name: 'a', type: 'int', value: '5' }],
    heap: [],
    changed: 'a',
    note: { ar: 'a نوع قيمة (Value type): الرقم 5 مخزّن داخل المتغير نفسه في الـ Stack.', en: 'a is a value type: 5 lives inside the variable on the stack.' },
  },
  {
    stack: [{ name: 'a', type: 'int', value: '5' }, { name: 'b', type: 'int', value: '5' }],
    heap: [],
    changed: 'b',
    note: { ar: 'الإسناد b = a **نسخ** القيمة. الآن هناك رقمان 5 منفصلان تماماً.', en: 'b = a COPIED the value: two independent 5s.' },
  },
  {
    stack: [{ name: 'a', type: 'int', value: '5' }, { name: 'b', type: 'int', value: '10' }],
    heap: [],
    changed: 'b',
    note: { ar: 'تغيير b لا يؤثر على a إطلاقاً.', en: 'Changing b does not touch a.' },
  },
  {
    stack: [{ name: 'a', type: 'int', value: '5' }, { name: 'b', type: 'int', value: '10' }, { name: 'p1', type: 'Point', ref: 1 }],
    heap: [{ id: 1, type: 'Point', fields: { X: '1' } }],
    changed: 'p1',
    note: { ar: 'new أنشأت كائناً في الـ Heap. المتغير p1 في الـ Stack يحمل **عنوانه** فقط (#1).', en: 'new created an object on the heap; p1 only holds its reference (#1).' },
  },
  {
    stack: [
      { name: 'a', type: 'int', value: '5' },
      { name: 'b', type: 'int', value: '10' },
      { name: 'p1', type: 'Point', ref: 1 },
      { name: 'p2', type: 'Point', ref: 1 },
    ],
    heap: [{ id: 1, type: 'Point', fields: { X: '1' } }],
    changed: 'p2',
    note: { ar: 'p2 = p1 نسخ **العنوان** وليس الكائن: المتغيران يشيران لنفس الكائن #1.', en: 'p2 = p1 copied the REFERENCE: both point to object #1.' },
  },
  {
    stack: [
      { name: 'a', type: 'int', value: '5' },
      { name: 'b', type: 'int', value: '10' },
      { name: 'p1', type: 'Point', ref: 1 },
      { name: 'p2', type: 'Point', ref: 1 },
    ],
    heap: [{ id: 1, type: 'Point', fields: { X: '99' } }],
    changed: 'heap-1',
    note: { ar: 'عدّلنا الكائن عبر p2، فـ p1.X أصبحت 99 أيضاً لأنه نفس الكائن! لو كان Point من نوع struct لما حدث ذلك.', en: 'We changed the object through p2, so p1.X is 99 too: it is the same object. With a struct this would not happen.' },
  },
];

export function StackHeap() {
  const { lang } = useI18n();
  const [step, setStep] = useState(0);
  const state = STATES[step];

  return (
    <div className="space-y-4">
      <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)]">
        <pre dir="ltr" className="hljs rounded-xl border border-line bg-code py-2 text-[13px] leading-7">
          {CODE.map((line, i) => (
            <div key={i} className={clsx('flex gap-3 px-3 transition-colors', i + 1 === step && 'bg-accent-soft', i + 1 > step && 'opacity-40')}>
              <span className="w-4 text-end text-muted">{i + 1 === step ? '▶' : i + 1}</span>
              <code dangerouslySetInnerHTML={{ __html: highlightLines(line, 'csharp')[0] }} />
            </div>
          ))}
        </pre>

        <div className="grid grid-cols-2 gap-3" dir="ltr">
          <Region title="Stack" subtitle={lang === 'ar' ? 'المكدس' : 'call frame'}>
            {state.stack.map((v) => (
              <div
                key={v.name}
                className={clsx(
                  'flex items-center justify-between rounded-lg border px-2.5 py-1.5 font-mono text-[13px] transition-all',
                  state.changed === v.name ? 'animate-fade-up border-accent bg-accent-soft' : 'border-line bg-surface',
                )}
              >
                <span>
                  <span className="text-muted">{v.type}</span> {v.name}
                </span>
                {v.ref ? (
                  <span className={clsx('rounded px-1.5 text-xs font-semibold', state.changed === 'heap-1' ? 'bg-warn text-black' : 'bg-info text-white')}>
                    → #{v.ref}
                  </span>
                ) : (
                  <span className="font-semibold">{v.value}</span>
                )}
              </div>
            ))}
          </Region>
          <Region title="Heap" subtitle={lang === 'ar' ? 'الكومة' : 'managed by the GC'}>
            {state.heap.map((o) => (
              <div
                key={o.id}
                className={clsx(
                  'rounded-lg border p-2 font-mono text-[13px] transition-all',
                  state.changed === `heap-${o.id}` ? 'animate-fade-up border-warn bg-warn-soft' : 'border-info/50 bg-info-soft/50',
                )}
              >
                <div className="mb-1 text-xs text-muted">
                  #{o.id} {o.type}
                </div>
                {Object.entries(o.fields).map(([k, v]) => (
                  <div key={k}>
                    {k} = <span className="font-semibold">{v}</span>
                  </div>
                ))}
              </div>
            ))}
          </Region>
        </div>
      </div>

      <p className="min-h-12 rounded-xl bg-surface-2 px-4 py-3 text-[15px]" dangerouslySetInnerHTML={{ __html: (lang === 'ar' ? state.note.ar : state.note.en).replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>') }} />

      <StepControls step={step} total={STATES.length - 1} onPrev={() => setStep((s) => s - 1)} onNext={() => setStep((s) => s + 1)} onReset={() => setStep(0)} />
    </div>
  );
}

function Region({ title, subtitle, children }: { title: string; subtitle: string; children: React.ReactNode }) {
  return (
    <div className="min-h-40 rounded-xl border border-dashed border-line p-2.5">
      <div className="mb-2 flex items-baseline justify-between">
        <span className="text-sm font-semibold">{title}</span>
        <span className="text-[11px] text-muted">{subtitle}</span>
      </div>
      <div className="space-y-1.5">{children}</div>
    </div>
  );
}

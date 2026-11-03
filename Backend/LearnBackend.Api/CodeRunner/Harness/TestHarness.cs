// =====================================================================
// Mini test harness — compiled TOGETHER with your code and the hidden
// tests into one assembly, then executed inside the sandbox process.
//
// It is intentionally tiny (a few xUnit-like asserts). It is NOT compiled
// into the API itself: the .csproj embeds this file as a resource
// (see <EmbeddedResource> in LearnBackend.Api.csproj).
//
// Protocol: every line written to the REAL stdout starting with the
// marker "@@LEARN@@" is a JSON message the API parses. Your own
// Console.WriteLine output is captured per test instead.
// =====================================================================
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LearnHarness
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute
    {
        public TestAttribute(string ar, string en) { Ar = ar; En = en; }
        public string Ar { get; }
        public string En { get; }
    }

    public sealed class AssertFailedException : Exception
    {
        public AssertFailedException(string message, string? expected = null, string? actual = null)
            : base(message) { Expected = expected; Actual = actual; }
        public string? Expected { get; }
        public string? Actual { get; }
    }

    public static class Assert
    {
        public static void Equal<T>(T expected, T actual, string? because = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new AssertFailedException(because ?? "Values are not equal", Fmt(expected), Fmt(actual));
        }

        public static void NotEqual<T>(T notExpected, T actual, string? because = null)
        {
            if (EqualityComparer<T>.Default.Equals(notExpected, actual))
                throw new AssertFailedException(because ?? "Values should differ", "not " + Fmt(notExpected), Fmt(actual));
        }

        public static void Near(double expected, double actual, double tolerance = 1e-6, string? because = null)
        {
            if (double.IsNaN(actual) || Math.Abs(expected - actual) > tolerance)
                throw new AssertFailedException(because ?? $"Values differ by more than {tolerance}", Fmt(expected), Fmt(actual));
        }

        public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T>? actual, string? because = null)
        {
            if (actual is null || !expected.SequenceEqual(actual))
                throw new AssertFailedException(because ?? "Sequences are not equal", Fmt(expected), Fmt(actual));
        }

        public static void True(bool condition, string? because = null)
        {
            if (!condition) throw new AssertFailedException(because ?? "Expected true", "true", "false");
        }

        public static void False(bool condition, string? because = null)
        {
            if (condition) throw new AssertFailedException(because ?? "Expected false", "false", "true");
        }

        public static void Null(object? value, string? because = null)
        {
            if (value is not null) throw new AssertFailedException(because ?? "Expected null", "null", Fmt(value));
        }

        public static T NotNull<T>(T? value, string? because = null) where T : class
        {
            if (value is null) throw new AssertFailedException(because ?? "Expected a non-null value", "not null", "null");
            return value;
        }

        public static void Contains(string expectedSubstring, string? actual, string? because = null)
        {
            if (actual is null || !actual.Contains(expectedSubstring, StringComparison.Ordinal))
                throw new AssertFailedException(because ?? "String does not contain the expected text", "…" + Fmt(expectedSubstring) + "…", Fmt(actual));
        }

        public static TException Throws<TException>(Action action, string? because = null) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
            catch (Exception ex)
            {
                throw new AssertFailedException(because ?? "Wrong exception type", typeof(TException).Name, ex.GetType().Name + ": " + ex.Message);
            }
            throw new AssertFailedException(because ?? "No exception was thrown", typeof(TException).Name, "(no exception)");
        }

        public static async Task<TException> ThrowsAsync<TException>(Func<Task> action, string? because = null) where TException : Exception
        {
            try { await action(); }
            catch (TException ex) { return ex; }
            catch (Exception ex)
            {
                throw new AssertFailedException(because ?? "Wrong exception type", typeof(TException).Name, ex.GetType().Name + ": " + ex.Message);
            }
            throw new AssertFailedException(because ?? "No exception was thrown", typeof(TException).Name, "(no exception)");
        }

        public static void Fail(string message) => throw new AssertFailedException(message);

        internal static string Fmt(object? value) => value switch
        {
            null => "null",
            string s => "\"" + s + "\"",
            char c => "'" + c + "'",
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            IEnumerable e => "[" + string.Join(", ", e.Cast<object?>().Take(20).Select(Fmt)) + "]",
            _ => value.ToString() ?? value.GetType().Name,
        };
    }

    /// <summary>A TextWriter that silently stops storing text after a limit.</summary>
    internal sealed class CappedWriter : System.IO.StringWriter
    {
        private const int Limit = 4000;
        public bool Truncated { get; private set; }
        public override Encoding Encoding => Encoding.UTF8;
        public override void Write(char value)
        {
            if (GetStringBuilder().Length < Limit) base.Write(value); else Truncated = true;
        }
        public override void Write(string? value)
        {
            if (value is null) return;
            var room = Limit - GetStringBuilder().Length;
            if (room <= 0) { Truncated = true; return; }
            if (value.Length > room) { base.Write(value.Substring(0, room)); Truncated = true; }
            else base.Write(value);
        }
        public override void Write(char[] buffer, int index, int count) => Write(new string(buffer, index, count));
    }

    public static class HarnessEntry
    {
        private const string Marker = "@@LEARN@@";
        private const int PerTestTimeoutMs = 2000;

        public static int Main(string[] args)
        {
            var realOut = Console.Out;
            var tests = typeof(HarnessEntry).Assembly.GetTypes()
                .Where(t => t.Namespace != "LearnHarness")
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Select(m => (Method: m, Attr: m.GetCustomAttribute<TestAttribute>()))
                .Where(x => x.Attr is not null)
                .OrderBy(x => x.Method.MetadataToken)
                .ToList();

            Emit(realOut, new { kind = "plan", tests = tests.Select(t => new { name = t.Method.Name, ar = t.Attr!.Ar, en = t.Attr.En }) });

            foreach (var (method, attr) in tests)
            {
                var writer = new CappedWriter();
                Console.SetOut(writer);
                var sw = Stopwatch.StartNew();
                string status; string? message = null, expected = null, actual = null, exceptionType = null; int? line = null;

                var run = Task.Run(async () =>
                {
                    var instance = method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType!);
                    var result = method.Invoke(instance, null);
                    if (result is Task t) await t;
                });

                try
                {
                    if (!run.Wait(PerTestTimeoutMs))
                    {
                        status = "timeout";
                        message = $"Test did not finish within {PerTestTimeoutMs} ms (infinite loop or deadlock?)";
                    }
                    else status = "passed";
                }
                catch (Exception ex)
                {
                    var inner = Unwrap(ex);
                    exceptionType = inner.GetType().FullName;
                    line = UserLine(inner);
                    if (inner is AssertFailedException af)
                    {
                        status = "failed"; message = af.Message; expected = af.Expected; actual = af.Actual;
                    }
                    else
                    {
                        status = "error"; message = inner.Message;
                    }
                }
                sw.Stop();
                Console.SetOut(realOut);

                var output = writer.ToString() + (writer.Truncated ? "\n…(output truncated)" : "");
                Emit(realOut, new
                {
                    kind = "result", name = method.Name, status, message, expected, actual,
                    exceptionType, line, output, ms = sw.ElapsedMilliseconds,
                });
            }

            Emit(realOut, new { kind = "done" });
            return 0;
        }

        private static Exception Unwrap(Exception ex)
        {
            while (true)
            {
                if (ex is TargetInvocationException { InnerException: { } tie }) ex = tie;
                else if (ex is AggregateException { InnerExceptions.Count: 1 } ae) ex = ae.InnerExceptions[0];
                else return ex;
            }
        }

        /// <summary>Finds the first stack frame that points into the learner's file (Solution.cs).</summary>
        private static int? UserLine(Exception ex)
        {
            var trace = new StackTrace(ex, fNeedFileInfo: true);
            foreach (var frame in trace.GetFrames())
            {
                var file = frame.GetFileName();
                if (file is not null && file.EndsWith("Solution.cs", StringComparison.Ordinal) && frame.GetFileLineNumber() > 0)
                    return frame.GetFileLineNumber();
            }
            return null;
        }

        private static void Emit(System.IO.TextWriter output, object payload)
        {
            output.WriteLine(Marker + JsonSerializer.Serialize(payload));
            output.Flush();
        }
    }
}

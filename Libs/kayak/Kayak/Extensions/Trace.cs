namespace Kayak.Extensions
{
    public static class Extensions
    {
        public static void DebugStackTrace(this System.Exception exception)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(GetStackTrace(exception));
#endif
        }

        public static void WriteStackTrace(this System.IO.TextWriter writer, System.Exception exception)
        {
            writer.WriteLine(GetStackTrace(exception));
        }

        public static string GetStackTrace(System.Exception exception)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            int i = 0;
            for (System.Exception e = exception; e != null; e = e.InnerException)
            {
                //if (e is TargetInvocationException || e is AggregateException) continue;

                sb.AppendLine(i++ == 0 ? "____________________________________________________________________________" : "Caused by:");

                sb.AppendLine($"[{e.GetType().Name}] {e.Message}");
                sb.AppendLine(e.StackTrace);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    // janky!
    internal static class Trace
    {
        public static void Write(string format, params object[] args)
        {
#if TRACE
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            System.Diagnostics.StackFrame stackFrame = stackTrace.GetFrame(1);
            System.Reflection.MethodBase methodBase = stackFrame.GetMethod();
            if (methodBase.DeclaringType != null)
                System.Console.WriteLine("[thread " + System.Threading.Thread.CurrentThread.ManagedThreadId + ", " +
                                         methodBase.DeclaringType.Name + "." + methodBase.Name + "] " +
                                         string.Format(format, args));
#endif
        }
    }
}
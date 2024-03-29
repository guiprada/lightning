using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace lightningTools
{
    struct LogEntry
    {
        public string message;
        public string path;
        public bool isLine;
        public bool append;
        public LogEntry(string p_message, string p_path, bool p_isLine, bool p_append)
        {
            message = p_message;
            path = p_path;
            isLine = p_isLine;
            append = p_append;
        }
    }
    public class Logger
    {
        private static ConcurrentQueue<LogEntry> queue;
        private static bool isProcessing;
        private static object isProcessingLock = new object();
        private static Task queueProcessing;

        static Logger()
        {
            queue = new ConcurrentQueue<LogEntry>();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CloseLogger);
            isProcessing = false;
            queueProcessing = null;
        }

        static void CloseLogger(object sender, EventArgs e)
        {
            queueProcessing.Wait();
            Console.WriteLine("Logger has exited :)");
            Console.Out.Flush();
        }

        static void ProcessQueue()
        {
            LogEntry entry;
            bool keep_going = true;

            do
            {
                if (queue.TryDequeue(out entry))
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(entry.path, entry.append))
                    {
                        if (entry.isLine)
                            file.WriteLine(entry.message);
                        else
                            file.Write(entry.message);
                    }
                }
                lock(isProcessingLock)
                {
                    isProcessing = !queue.IsEmpty;
                    keep_going = isProcessing;
                }
            } while (keep_going);
        }

        private static void Add(LogEntry entry)
        {
            queue.Enqueue(entry);
#if VERBOSE
            Console.WriteLine(entry.message);
#endif
            lock(isProcessingLock)
                if (isProcessing == false)
                {
                    isProcessing = true;
                    queueProcessing = Task.Run(ProcessQueue);
                }
        }

        public static void LogLine(string p_line, string p_path)
        {
            Add(new LogEntry(p_line, p_path, true, true));
        }
        public static void Log(string p_line, string p_path)
        {
            Add(new LogEntry(p_line, p_path, false, true));
        }
        public static void LogNew(string p_contents, string p_path)
        {
            Add(new LogEntry(p_contents, p_path, true, false));
        }
    }
}
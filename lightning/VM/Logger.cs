using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace lightning
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
            bool is_processing;
            lock(queue)
                is_processing = isProcessing;

            if(is_processing)
            {
                Console.WriteLine("Waiting for Logger to finish!");
                Console.Out.Flush();
                queueProcessing.Wait();
            }

            Console.WriteLine("Logger has exited :)");
            Console.Out.Flush();
        }

        static void ProcessQueue()
        {
            LogEntry entry;
            bool has_entry;
            do
            {
                lock(queue)
                {
                    if (queue.Count == 0)
                        isProcessing = false;
                    else
                        isProcessing = true;
                    has_entry = queue.TryDequeue(out entry);
                }
                if (has_entry)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(entry.path, entry.append))
                    {
                        if (entry.isLine)
                            file.WriteLine(entry.message);
                        else
                            file.Write(entry.message);
                    }
                }
            } while (isProcessing);
        }
        private static async void Add(LogEntry entry)
        {
            lock(queue)
            {
                queue.Enqueue(entry);
#if VERBOSE
			    Console.WriteLine(entry.message);
#endif
                if (isProcessing == false)
                    queueProcessing = Task.Run(ProcessQueue);
            }

            await queueProcessing;
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
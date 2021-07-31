using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace lightning
{
    struct LogEntry{
        public string message;
        public string path;
        public bool isLine;
        public bool append;

        public LogEntry(string p_message, string p_path, bool p_isLine, bool p_append){
            message = p_message;
            path = p_path;
            isLine = p_isLine;
            append = p_append;
        }
    }
    public class Logger{
        private static ConcurrentQueue<LogEntry> queue;
        private static volatile bool isProcessing;

        static Logger(){
            queue = new ConcurrentQueue<LogEntry>();
            isProcessing = false;
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CloseLogger);
        }

        static void CloseLogger(object sender, EventArgs e){
            if(isProcessing){
                Console.WriteLine("Waiting for Logger to exit :/");
                while(isProcessing){
                    Console.Write(".");
                    Thread.Sleep(100);
                }
            }
            Console.WriteLine("Logger has exited :)");
        }

        static async Task ProcessQueue(){
            isProcessing = true;
            LogEntry entry =  default(LogEntry);
            bool has_entry = false;
            do{
                if(has_entry)
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(entry.path, entry.append)){
                        if(entry.isLine)
                            await file.WriteLineAsync(entry.message);
                        else
                            await file.WriteAsync(entry.message);
                    }
                has_entry = queue.TryDequeue(out entry);
            }while(has_entry);
            isProcessing = false;
        }
        private static void Add(LogEntry entry){
            queue.Enqueue(entry);
            if(isProcessing == false){
                Task.Run(ProcessQueue);
            }
        }

        public static void LogLine(string p_line, string p_path){
            Add(new LogEntry(p_line, p_path, true, true));
        }
        public static void Log(string p_line, string p_path){
            Add(new LogEntry(p_line, p_path, false, true));
        }
        public static void LogNew(string p_contents, string p_path){
            Add(new LogEntry(p_contents, p_path, true, false));
        }
    }
}
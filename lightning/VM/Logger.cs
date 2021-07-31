using System;
using System.Collections.Generic;
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
        private static Queue<LogEntry> queue;
        private static bool isProcessing;

        static Logger(){
            queue = new Queue<LogEntry>();
            isProcessing = false;
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CloseLogger);
        }

        static void CloseLogger(object sender, EventArgs e){
            lock(isProcessing as object)
                if(isProcessing)
                    Console.WriteLine("ERROR: Logger has unflushed data!!!");
                else
                    Console.WriteLine("Logger has exited!");
        }

        static async Task ProcessQueue(){
            lock(isProcessing as object)
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
                lock(queue){
                    if (queue.Count > 0){
                        entry = queue.Dequeue();
                        has_entry = true;
                    }else{
                        has_entry = false;
                        lock(isProcessing as object)
                            isProcessing = false;
                    }
                }
            }while(has_entry);
        }
        private static void Add(LogEntry entry){
            lock(queue){
                queue.Enqueue(entry);
            }
            bool is_processing;
            lock(isProcessing as object)
                is_processing = isProcessing;
            if(is_processing == false){
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
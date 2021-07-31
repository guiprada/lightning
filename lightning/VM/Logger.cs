using System;
using System.Collections.Generic;
using System.Threading;

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
        private static bool isRunning;
        private static Thread queueProcessing;

        static Logger(){
            queue = new Queue<LogEntry>();
            isProcessing = false;
            isRunning = true;
            queueProcessing = new Thread(ProcessQueue);
            queueProcessing.IsBackground = true;
            AppDomain.CurrentDomain.ProcessExit += FileWriterClose;// Subscribe to Event
        }

        private static void FileWriterClose(object sender, EventArgs e){
            lock(isRunning as object)
                isRunning = false;
            queueProcessing.Join();
        }

        static async void ProcessQueue(){
            bool is_running;
            lock(isRunning as object)
                is_running = isRunning;
            while(is_running){
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
                Thread.Sleep(0);
                lock(isRunning as object)
                    is_running = isRunning;
            }
            Console.WriteLine("FileWriter Stopped!");
        }
        private static void Add(LogEntry entry){
            lock(queue){
                queue.Enqueue(entry);
            }
            bool is_processing;
            lock(isProcessing as object)
                is_processing = isProcessing;
            if(is_processing == false){
                queueProcessing.Start();
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
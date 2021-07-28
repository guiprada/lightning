using System.Collections.Generic;
using System.Threading;

namespace lightning
{
    struct LogEntry{
        public string message;
        public string path;
        public LogEntry(string p_message, string p_path){
            message = p_message;
            path = p_path;
        }
    }
    public class Logger{
        private static Queue<LogEntry> queue;
        private static bool isProcessing;
        static Logger(){
            queue = new Queue<LogEntry>();
            isProcessing = false;
        }

        static async void ProcessQueue(){
            lock(isProcessing as object)
                isProcessing = true;

            LogEntry entry =  default(LogEntry);
            bool has_entry = false;
            do{
                if(has_entry)
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(entry.path, true))
                        await file.WriteLineAsync(entry.message);
                lock(queue){
                    if (queue.Count > 0){
                        entry = queue.Dequeue();
                        has_entry = true;
                    }else
                        has_entry = false;
                }
            }while(has_entry);

            lock(isProcessing as object)
                isProcessing = false;
        }
        public static void AddLine(string p_message, string p_path){
            lock(queue)
                queue.Enqueue(new LogEntry(p_message, p_path));

            bool is_processing;
            lock(isProcessing as object)
                is_processing = isProcessing;
            if(is_processing == false){
                Thread dequeue = new Thread(ProcessQueue);
                dequeue.Start();
            }
        }
    }
}
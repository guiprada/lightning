using System.Collections.Generic;

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
            isProcessing = true;
            while(queue.Count > 0){
                LogEntry entry = queue.Dequeue();
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(entry.path, true)){
                    await file.WriteLineAsync(entry.message);
                }
            }
            isProcessing = false;
        }
        public static void AddLine(string p_message, string p_path){
            queue.Enqueue(new LogEntry(p_message, p_path));
            if(isProcessing == false)
                ProcessQueue();
        }
    }
}
using System.Collections.Generic;
using System.Threading;

namespace lightning
{
    struct Entry{
        public string message;
        public string path;
        public bool isLine;
        public bool append;
        public Entry(string p_message, string p_path, bool p_isLine, bool p_append){
            message = p_message;
            path = p_path;
            isLine = p_isLine;
            append = p_append;
        }
    }
    public class FileWriter{
        private static Queue<Entry> queue;
        private static bool isProcessing;
        static FileWriter(){
            queue = new Queue<Entry>();
            isProcessing = false;
        }

        static async void ProcessQueue(){
            lock(isProcessing as object)
                isProcessing = true;

            Entry entry =  default(Entry);
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
        private static void Add(Entry entry){
            lock(queue){
                queue.Enqueue(entry);
            }
            bool is_processing;
            lock(isProcessing as object)
                is_processing = isProcessing;
            if(is_processing == false){
                Thread dequeue = new Thread(ProcessQueue);
                dequeue.Start();
            }
        }
        public static void WriteLine(string p_line, string p_path){
            Add(new Entry(p_line, p_path, true, true));
        }
        public static void Write(string p_line, string p_path){
            Add(new Entry(p_line, p_path, false, true));
        }
        public static void Create(string p_contents, string p_path){
            Add(new Entry(p_contents, p_path, true, false));
        }
    }
}
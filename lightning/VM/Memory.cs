
using System.Collections.Generic;

namespace lightning
{
    public class Memory<T>{
        List<T> values;
        List<int> markers;

        public int Env{
            get{
                return markers.Count - 1;
            }
        }

        public int Marker{
            get{
                return markers[^1];
            }
        }

        public int Count{
            get{
                return values.Count;
            }
        }

        public int Capacity{
            get{
                return values.Capacity;
            }
        }

        public Memory(){
            values = new List<T>();
            markers = new List<int>();
        }

        public void Trim(){
            markers.TrimExcess();
            values.TrimExcess();
        }

        public void Clear(){
            values.Clear();
            markers.Clear();
        }

        public T Get(int p_index){
            return values[p_index];
        }

        public T GetAt(int p_index, int p_env){
            int this_base = markers[p_env];
            return values[this_base + p_index];
        }

        public T GetAt(int p_index){
            return values[Marker + p_index];
        }

        public void SetAt(T p_new_value, int p_index, int p_env){
            int this_base = markers[p_env];
            values[this_base + p_index] = p_new_value;
        }

        public void Set(T p_new_value, int p_index){
            values[p_index] = p_new_value;
        }

        public int Add(T p_new_value){
            values.Add(p_new_value);
            return values.Count - 1;
        }

        public void PushEnv(){
            markers.Add(values.Count);
        }

        public void PopEnv(){
            int last_index = markers.Count -1;
            values.RemoveRange(markers[last_index], values.Count - markers[last_index]);
            markers.RemoveAt(last_index);
        }

    }
}

using System.Collections.Generic;
using Operand = System.UInt16;
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

        public Memory(){
            values = new List<T>();
            markers = new List<int>();
            PushEnv();
        }

        public void Trim(){
            markers.TrimExcess();
            values.TrimExcess();
        }

        public T Get(int index){
            return values[index];
        }

        public T GetAt(int index, int env){
            int this_base = markers[env];
            return values[this_base + index];
        }

        public T GetAt(int index){
            return values[Marker + index];
        }

        public void SetAt(T new_value, int index, int env){
            int this_base = markers[env];
            values[this_base + index] = new_value;
        }

        public void Set(T new_value, int index){
            values[index] = new_value;
        }

        public int Add(T new_value){
            values.Add(new_value);
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
    public class GlobalMemory{
        List<Unit> values;

        List<int> markers;
        public GlobalMemory(){
            values = new List<Unit>();
            markers = new List<int>();
        }

        public Unit Get(int index){
            return values[index];
        }

        public void Set(Unit new_value, int index){
            values[index] = new_value;
        }

        public void AtomicSet(Unit new_value, int index){
            values[index] = new_value;
        }

        public void AtomicInc(Unit increment, int index){
            lock(values){
                values[index] = new Unit(values[index].number + increment.number);
            }
        }

        public void AtomicDec(Unit decrement, int index){
            lock(values){
                values[index] = new Unit(values[index].number - decrement.number);
            }
        }
        public void AtomicMult(Unit factor, int index){
            lock(values){
                values[index] = new Unit(values[index].number * factor.number);
            }
        }
        public void AtomicDiv(Unit factor, int index){
            lock(values){
                values[index] = new Unit(values[index].number / factor.number);
            }
        }
        public int Add(Unit new_value){
            values.Add(new_value);
            return values.Count - 1;
        }
        public void Trim(){
            markers.TrimExcess();
            values.TrimExcess();
        }

        public int Count(){
            return values.Count;
        }
    }
}
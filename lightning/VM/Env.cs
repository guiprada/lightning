
using System.Collections.Generic;
using Operand = System.UInt16;
namespace lightning
{
	public class Variables{
		List<Unit> values; // used for scoped variables
        int variablesTop;

        int[] variablesBases; // used to control the address used by each scope
        int variablesBasesTop;

		public int BasesTop{
			get{
				return variablesBasesTop;
			}
		}

		public Variables(int p_function_deepness){
            values = new List<Unit>();
			variablesTop = 0;
            variablesBases = new int[3 * p_function_deepness];
            variablesBasesTop = 0;
            variablesBases[variablesBasesTop] = 0;
            variablesBasesTop++;
		}

		public void Trim(){
			values.TrimExcess();
		}

		public Unit VarAt(Operand address, Operand n_env)
        {
            int this_BP = variablesBases[n_env];
            return values[this_BP + address];
        }

        public void VarSet(Unit new_value, Operand address, Operand n_env)
        {
            values[address + variablesBases[n_env]] = new_value;
        }

		public void PushEnv(){
			variablesBases[variablesBasesTop] = variablesTop;
			variablesBasesTop++;
		}

		public void PopEnv(){
			int this_basePointer = variablesBases[variablesBasesTop - 1];
            variablesBasesTop--;
            variablesTop = this_basePointer;

            values.RemoveRange(this_basePointer, values.Count - this_basePointer);
		}

        public void Add(Unit new_value){
            if (variablesTop >= (values.Count))
            {
                values.Add(new_value);
                variablesTop++;
            }
            else
            {
                values[variablesTop] = new_value;
                variablesTop++;
            }
        }

	}
	public class Memory<T>{
		List<T> values;

        List<int> markers;
        int marker;

		public int Marker{
			get{
				return marker;
			}
		}

		public int Count{
			get{
				return values.Count;
			}
		}

		public int Env{
			get{
				return markers.Count - 1;
			}
		}

        public Memory(){
            values = new List<T>();
            markers = new List<int>();
            marker = 0;
			PushEnv();
        }

        public T Get(int index){
            return values[index];
        }

		public T GetAt(int index, int env){
			int this_base = markers[env];
			return values[this_base + index];
		}

		public T GetAt(int index){
			return values[marker + index];
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
        public void Trim(){
            markers.TrimExcess();
            values.TrimExcess();
        }
        public void PushEnv(){
            markers.Add(marker);
            marker = values.Count;
        }

        public void PopEnv(){
            values.RemoveRange(marker, values.Count - marker);
            int last_index = markers.Count -1;
            marker = markers[last_index];
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
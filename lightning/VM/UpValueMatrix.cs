using System.Collections.Generic;

using Operand = System.UInt16;
namespace lightning
{
	public class UpValueMatrix{
		List<Dictionary<Operand, Dictionary<Operand, UpValueUnit>>> env;
		Dictionary<Operand, Dictionary<Operand, UpValueUnit>> values;

		public UpValueMatrix(){
			values = new Dictionary<Operand, Dictionary<Operand, UpValueUnit>>();
			env = new List<Dictionary<Operand, Dictionary<Operand, UpValueUnit>>>();
			env.Add(values);
		}

		public void Push(){
			values = new Dictionary<Operand, Dictionary<Operand, UpValueUnit>>();
			env.Add(values);
		}

		public void Pop(){
			values = env[^1];
			env.RemoveAt(env.Count -1);
		}

		public UpValueUnit Get(Operand x, Operand y){
			Dictionary<Operand, UpValueUnit> this_line;
			if(values.TryGetValue(x, out this_line)){
				UpValueUnit this_up_value;
				if(values[x].TryGetValue(y, out this_up_value))
					return this_up_value;
			}

			return null;
		}

		public void Set(UpValueUnit p_value, Operand x, Operand y){
			Dictionary<Operand, UpValueUnit> this_line;
			if(!values.TryGetValue(x, out this_line)){
				values[x] = new Dictionary<Operand, UpValueUnit>();
				this_line = values[x];
			}
			this_line.Add(y, p_value);
		}

		public void Capture(){
			foreach(KeyValuePair<Operand, Dictionary<Operand, UpValueUnit>> line in values){
				foreach(KeyValuePair<Operand, UpValueUnit> item in line.Value)
					item.Value.Capture();
				line.Value.Clear();
			}
		}

		public void Clear(){
			foreach(KeyValuePair<Operand, Dictionary<Operand, UpValueUnit>> line in values){
				line.Value.Clear();
			}
		}
	}
}
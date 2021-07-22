using System.Collections.Generic;

using Operand = System.UInt16;
namespace lightning
{
	public class UpValueMatrix{
		Dictionary<Operand, Dictionary<Operand, UpValueUnit>> values;
		public UpValueMatrix(){
			values = new Dictionary<Operand, Dictionary<Operand, UpValueUnit>>();
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

		}

		public void Clear(){
			foreach(KeyValuePair<Operand, Dictionary<Operand, UpValueUnit>> item in values){
				item.Value.Clear();
			}
		}
	}
}
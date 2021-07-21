using System.Collections.Generic;

using Operand = System.UInt16;
namespace lightning
{
	public class SparseMatrix{
		Dictionary<Operand, Dictionary<Operand, UpValueUnit>> values;
		public SparseMatrix(){
			values = new Dictionary<Operand, Dictionary<Operand, UpValueUnit>>();
		}

		public UpValueUnit Get(Operand x, Operand y){
			if(values.ContainsKey(x))
				if(values[x].ContainsKey(y))
					return values[x][y];
			return null;
		}

		public void Set(UpValueUnit p_value, Operand x, Operand y){
			if(!values.ContainsKey(x))
				values[x] = new Dictionary<Operand, UpValueUnit>();
			values[x].Add(y, p_value);
		}

		public void Clear(){
			foreach(KeyValuePair<Operand, Dictionary<Operand, UpValueUnit>> item in values){
				item.Value.Clear();
			}
		}
	}
}
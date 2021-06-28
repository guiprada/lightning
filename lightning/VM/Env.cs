using Operand = System.UInt16;

namespace lightning{
    public struct Env
    {
        Memory<Unit> variables; // used for scoped variables
		public int Current{
			get{
				return variables.Env;
			}
		}

		public Memory<Unit> Variables{
			get{
				return variables;
			}
		}

		public Env(Memory<Unit> p_variables){
			variables = p_variables;
		}

		public void Push(){
			variables.PushEnv();
		}

		public void Pop(){
			variables.PopEnv();
		}

		public void ResourcesTrim(){
			variables.Trim();
		}

		Operand CalculateShift(Operand n_shift){
            return (Operand)(variables.Env - n_shift);
        }

        public Operand CalculateShiftUpVal(Operand env){
            return (Operand)(variables.Env + 1 - env);
        }

		public Unit GetVarAt(Operand address, Operand n_shift){
			return variables.GetAt(address, CalculateShift(n_shift));
		}

		public void SetVarAt(Unit p_new_value, Operand p_address, Operand p_n_shift){
			variables.SetAt(p_new_value, p_address, CalculateShift(p_n_shift));
		}

		public void AddVar(Unit new_var){
			variables.Add(new_var);
		}
	}
}
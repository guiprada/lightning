using System.Collections.Generic;

using Operand = System.UInt16;
namespace lightning
{
    public class UpValueEnv{
        List<Dictionary<Operand, UpValueUnit>> env;
        int top;

        public UpValueEnv(){
            env = new List<Dictionary<Operand, UpValueUnit>>();
            top = 0;
        }

        public void PushEnv(){
            if(top > (env.Count - 1))
                env.Add(new Dictionary<Operand, UpValueUnit>());
            top++;
        }

        public void PopEnv(){
            top--;
            foreach(KeyValuePair<Operand,UpValueUnit> item in env[top])
                item.Value.Capture();
            env[top].Clear();
        }

        public UpValueUnit Get(Operand p_address, Operand p_env){
            UpValueUnit this_upvalue;
            if(env[p_env].TryGetValue(p_address, out this_upvalue))
                return this_upvalue;
            else{
                this_upvalue = new UpValueUnit(p_address,(Operand)(p_env + 1));// We shift back to global env.
                Set(this_upvalue, p_address, p_env);
                return this_upvalue;
            }
        }

        public void Set(UpValueUnit p_value, Operand p_address, Operand p_env){
            env[p_env].Add(p_address, p_value);
        }

        public void Clear(){
            for(int i=0; i<(env.Count -1); i++)
                env[i].Clear();
        }
    }
}
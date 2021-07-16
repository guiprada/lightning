using System.Collections.Generic;

using Operand = System.UInt16;
namespace lightning{
    public struct Instructions
    {
        int currentInstructionsIndex;
        FunctionUnit[] functions;
        int returnAdressTop;
        Operand[] returnAdress;
        int[] funCallEnv;

        public int TargetEnv{
            get{
                return funCallEnv[currentInstructionsIndex];
            }
        }

        public FunctionUnit ExecutingFunction{
            get {
                return functions[currentInstructionsIndex];
            }
        }

        public List<Instruction> ExecutingInstructions{
            get{
                return functions[currentInstructionsIndex].Body;
            }
        }

        public int ExecutingInstructionsIndex{
            get{
                return currentInstructionsIndex;
            }
        }

        public Instructions(int p_function_deepness, FunctionUnit p_main, out List<Instruction> p_instructions_cache){
            functions = new FunctionUnit[p_function_deepness];
            returnAdress = new Operand[2 * p_function_deepness];
            funCallEnv = new int[p_function_deepness];

            returnAdressTop = 0;
            currentInstructionsIndex = 0;

            PushRET((Operand)(p_main.Body.Count - 1));
            functions[currentInstructionsIndex] = p_main;

            p_instructions_cache = ExecutingInstructions;
        }

        public void Reset(){
            returnAdressTop = 1;
            currentInstructionsIndex = 0;
        }

        public Operand PopFunction(out List<Instruction> p_instructions_cache){
            returnAdressTop--;
            currentInstructionsIndex--;

            p_instructions_cache = ExecutingInstructions;

            return returnAdress[returnAdressTop];
        }

        public void PushFunction(FunctionUnit p_function, int p_env, out List<Instruction> p_instructions_cache){
            currentInstructionsIndex++;

            funCallEnv[currentInstructionsIndex] = p_env;
            functions[currentInstructionsIndex] = p_function;

            p_instructions_cache = ExecutingInstructions;
        }

        public Operand PopRET(){
            returnAdressTop--;
            return returnAdress[returnAdressTop];
        }

        public void PushRET(Operand p_address){
            returnAdress[returnAdressTop] = p_address;
            returnAdressTop ++;
        }
    }
}
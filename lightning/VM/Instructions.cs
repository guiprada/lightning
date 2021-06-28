using System;
using System.Collections.Generic;

using Operand = System.UInt16;
namespace lightning{
    public struct Instructions
    {
        List<Instruction>[] stack;
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
                return stack[currentInstructionsIndex];
            }
        }

        public int ExecutingInstructionsIndex{
            get{
                return currentInstructionsIndex;
            }
        }

        public Instructions(int p_function_deepness, Chunk p_chunk){
            stack = new List<Instruction>[p_function_deepness];
            functions = new FunctionUnit[p_function_deepness];
            returnAdress = new Operand[2 * p_function_deepness];
            funCallEnv = new int[p_function_deepness];

            returnAdressTop = 0;

            stack[0] = p_chunk.Program;
            currentInstructionsIndex = 0;

            returnAdress[returnAdressTop] = (Operand)(p_chunk.ProgramSize - 1);
            returnAdressTop++;
        }

        public Operand PopFunction(){
            returnAdressTop--;

            currentInstructionsIndex--;

            return returnAdress[returnAdressTop];
        }

        public List<Instruction> PushFunction(FunctionUnit p_function, int p_env){
            currentInstructionsIndex++;

            funCallEnv[currentInstructionsIndex] = p_env;
            stack[currentInstructionsIndex] = p_function.body;
            functions[currentInstructionsIndex] = p_function;

            return ExecutingInstructions;
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
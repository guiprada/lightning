using System;
using System.Collections.Generic;

using Operand = System.UInt16;
namespace lightning{
    public struct Instructions
    {
        List<Instruction>[] stack;

        int runningInstructionsIndex;// contains the currently executing instructions
        FunctionUnit[] functionCallStack;
        int returnAdressTop;
        Operand[] returnAdress;
        int[] funCallEnv;

        public int TargetEnv{
            get{
                return funCallEnv[runningInstructionsIndex];
            }
        }

        public FunctionUnit RunningFunction{
            get {
                return functionCallStack[runningInstructionsIndex];
            }
        }

        public List<Instruction> RunningInstructions{
            get{
                return stack[runningInstructionsIndex];
            }
        }

        public int RunningInstructionsIndex{
            get{
                return runningInstructionsIndex;
            }
        }

        public Instructions(int p_function_deepness, Chunk p_chunk){
            stack = new List<Instruction>[p_function_deepness];
            functionCallStack = new FunctionUnit[p_function_deepness];
            returnAdress = new Operand[2 * p_function_deepness];
            funCallEnv = new int[p_function_deepness];

            returnAdressTop = 0;

            stack[0] = p_chunk.Program;
            runningInstructionsIndex = 0;

            returnAdress[returnAdressTop] = (Operand)(p_chunk.ProgramSize - 1);
            returnAdressTop++;
        }

        public Operand PopFunction(){
            returnAdressTop--;

            runningInstructionsIndex--;

            return returnAdress[returnAdressTop];
        }

        public void PushFunction(FunctionUnit p_function, int p_env){
            runningInstructionsIndex++;

            funCallEnv[runningInstructionsIndex] = p_env;
            stack[runningInstructionsIndex] = p_function.body;
            functionCallStack[runningInstructionsIndex] = p_function;
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
using System.Collections.Generic;


using lightningChunk;
using lightningUnit;
namespace lightningVM
{
    public struct InstructionStack
    {
        int currentInstructionsIndex;
        FunctionUnit[] functions;
        int returnAddressTop;
        Operand[] returnAddress;
        int[] funCallEnv;

        public int TargetEnv
        {
            get
            {
                return funCallEnv[currentInstructionsIndex];
            }
        }

        public FunctionUnit ExecutingFunction
        {
            get
            {
                return functions[currentInstructionsIndex];
            }
        }

        public List<Instruction> ExecutingInstructions
        {
            get
            {
                return functions[currentInstructionsIndex].Body;
            }
        }

        public int ExecutingInstructionsIndex
        {
            get
            {
                return currentInstructionsIndex;
            }
        }

        public InstructionStack(int p_callStackSize, FunctionUnit p_main, out List<Instruction> p_instructionsCache)
        {
            functions = new FunctionUnit[p_callStackSize];
            returnAddress = new Operand[2 * p_callStackSize];
            funCallEnv = new int[p_callStackSize];

            returnAddressTop = 0;
            currentInstructionsIndex = 0;

            PushRET((Operand)(p_main.Body.Count - 1));
            functions[currentInstructionsIndex] = p_main;

            p_instructionsCache = ExecutingInstructions;
        }

        public void Reset()
        {
            returnAddressTop = 1;
            currentInstructionsIndex = 0;
        }

        public Operand PopFunction(out List<Instruction> p_instructionsCache)
        {
            returnAddressTop--;
            currentInstructionsIndex--;

            p_instructionsCache = ExecutingInstructions;

            return returnAddress[returnAddressTop];
        }

        public void PushFunction(FunctionUnit p_function, int p_env, out List<Instruction> p_instructionsCache)
        {
            currentInstructionsIndex++;

            funCallEnv[currentInstructionsIndex] = p_env;
            functions[currentInstructionsIndex] = p_function;

            p_instructionsCache = ExecutingInstructions;
        }

        public Operand PopRET()
        {
            returnAddressTop--;
            return returnAddress[returnAddressTop];
        }

        public void PushRET(Operand p_address)
        {
            returnAddress[returnAddressTop] = p_address;
            returnAddressTop++;
        }
    }
}
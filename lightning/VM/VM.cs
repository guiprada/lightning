using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Operand = System.UInt16;
#if DOUBLE
using Number = System.Double;
#else
    using Number = System.Single;
#endif

namespace lightning
{
    public enum VMResultType
    {
        OK,
        ERROR
    }

    public struct VMResult
    {
        public VMResultType status;
        public Value value;

        public VMResult(VMResultType p_status, Value p_value)
        {
            status = p_status;
            value = p_value;
        }
    }

    public class VM
    {
        Chunk chunk;
        List<Instruction>[] instructionsStack;
        int executingInstructions;// contains the currently executing instructions
        ValFunction[] functionCallStack;
        Value[] stack; // used for operations
        int stackTop;
        List<Value> globals; // used for global variables
        List<Value> variables; // used for scoped variables
        int variablesTop;

        int[] variablesBases; // used to control the address used by each scope
        int variablesBasesTop;

        List<ValUpValue> upValues; // used to store upvalues
        int[] upValuesBases; //
        int upValuesBasesTop;

        List<ValUpValue> upValuesRegistry;
        int[] upValuesRegistryBases;
        int upValuesRegistryBasesTop;

        Operand[] ret;
        int ret_count;
        int[] funCallEnv;
        Value[] stash;
        int stashTop;

        Operand IP;
        List<ValIntrinsic> Intrinsics { get; set; }
        public Dictionary<string, int> loadedModules { get; private set; }
        public List<ValModule> modules;

        public VM(Chunk p_chunk, int function_deepness = 25)
        {          
            chunk = p_chunk;

            instructionsStack = new List<Instruction>[function_deepness];
            instructionsStack[0] = chunk.Program;
            executingInstructions = 0;
            functionCallStack = new ValFunction[function_deepness];

            stack = new Value[3 * function_deepness];
            stackTop = 0;
            globals = new List<Value>();
            variables = new List<Value>();
            variablesTop = 0;

            variablesBases = new int[3 * function_deepness];
            variablesBasesTop = 0;
            variablesBases[variablesBasesTop] = 0;
            variablesBasesTop++;

            upValues = new List<ValUpValue>();
            upValuesBases = new int[function_deepness];
            upValuesBasesTop = 0;
            upValuesBases[upValuesBasesTop] = 0;
            upValuesBasesTop++;

            upValuesRegistry = new List<ValUpValue>();
            upValuesRegistryBases = new int[function_deepness];
            upValuesRegistryBasesTop = 0;

            ret = new Operand[2 * function_deepness];
            ret[executingInstructions] = (Operand)(chunk.ProgramSize - 1);
            ret_count = 1;
            funCallEnv = new int[function_deepness];
            stash = new Value[function_deepness];
            stashTop = 0;
            IP = 0;

            Intrinsics = chunk.Prelude.intrinsics;
            foreach (ValIntrinsic v in Intrinsics)
            {
                globals.Add(v);
            }
            foreach (KeyValuePair<string, ValTable> entry in chunk.Prelude.tables)
            {
                globals.Add(entry.Value);
            }

            loadedModules = new Dictionary<string, int>();
            modules = new List<ValModule>();
        }

        public Operand AddModule(ValModule this_module)
        {
            loadedModules.Add(this_module.name, loadedModules.Count);
            modules.Add(this_module);
            return (Operand)(loadedModules.Count - 1);
        }

        public Chunk GetChunk()
        {
            return chunk;
        }

        public Value GetGlobal(Operand address)
        {
            return globals[address];
        }

        void StackPush(Value p_value)
        {
            stack[stackTop] = p_value;
            stackTop++;
        }

        Value StackPop()
        {            
            stackTop--;
            Value popped = stack[stackTop];
            return popped;
        }

        Value StackPeek()
        {            
            return stack[stackTop - 1];
        }

        public Value StackPeek(int n)
        {
            if (n < 0 || n > (stackTop - 1)) return null;            
            return stack[stackTop - n - 1];
        }

        Value VarAt(Operand address, Operand n_env)
        {
            //Console.WriteLine(address + " " + n_env + " this env" + (variablesBases.Count - 1));
            int this_BP = variablesBases[n_env];
            return variables[this_BP + address];
        }

        void VarSet(Value new_value, Operand address, Operand n_env)
        {
            variables[address + variablesBases[n_env]] = new_value;
        }

        void RegisterUpValue(ValUpValue u)
        {
            //Console.WriteLine("upvalue registered " + u.address + " " + u.env);
            upValuesRegistry.Add(u);
        }

        void EnvPush()
        {            
            variablesBases[variablesBasesTop] = variablesTop;
            variablesBasesTop++;
            upValuesRegistryBases[upValuesRegistryBasesTop] = upValuesRegistry.Count;
            upValuesRegistryBasesTop++;
            //Console.WriteLine("env add " + (variablesBases.Count - 1));
        }

        void EnvPop()
        {
            // capture closures
            int upvalues_start = upValuesRegistryBases[upValuesRegistryBasesTop - 1];
            upValuesRegistryBasesTop--;
            int upvalues_end = upValuesRegistry.Count;
            for (int i = upvalues_start; i < upvalues_end; i++)
            {
                upValuesRegistry[i].Capture();
            }
            if (upvalues_start < (upValuesRegistry.Count * 0.25))
            {
                int medium = (upvalues_start + upValuesRegistry.Count) / 2;
                upValuesRegistry.RemoveRange(medium, upValuesRegistry.Count - medium);
            }
            //upValuesRegistry.RemoveRange(upvalues_start, upValuesRegistry.Count - upvalues_start);

            int this_basePointer = variablesBases[variablesBasesTop - 1];
            variablesBasesTop--;
            variablesTop = this_basePointer;


            if (this_basePointer < (variables.Count * 0.25))
            {
                int medium = (this_basePointer + variables.Count) / 2;
                variables.RemoveRange(medium, variables.Count - medium);
            }
            //variables.RemoveRange(this_basePointer, variables.Count - this_basePointer);
        }

        void EnvSet(int target_env)
        {
            while ((variablesBasesTop - 1) > target_env)
            {
                EnvPop();
            }
        }

        void UpValuesPush()
        {
            upValuesBases[upValuesBasesTop] = upValues.Count;
            upValuesBasesTop++;
        }

        void UpValuesPop()
        {
            int this_UpvaluesBase = upValuesBases[upValuesBasesTop - 1];
            upValuesBasesTop--;
            if (this_UpvaluesBase < (upValues.Count * 0.25))
            {
                int medium = (this_UpvaluesBase + upValues.Count) / 2;
                upValues.RemoveRange(medium, upValues.Count - medium);
            }
            //upValues.RemoveRange(this_UpvaluesBase, upValues.Count - this_UpvaluesBase);
        }

        ValUpValue UpValuesAt(int address)
        {
            int this_UpvalueBasePointer = upValuesBases[upValuesBasesTop -1];
            return upValues[this_UpvalueBasePointer + address];
        }

        void Error(string msg)
        {
            if (executingInstructions == 0)
                Console.WriteLine("Error: " + msg + chunk.GetLine(IP));
            else
            {
                Console.Write("Error: " + msg);
                Console.Write(" on function: " + functionCallStack[executingInstructions].name);
                Console.Write(" from module: " + functionCallStack[executingInstructions].module);
                Console.WriteLine(" on line: " + chunk.GetLine(IP + functionCallStack[executingInstructions].originalPosition));
            }
        }

        public Value CallFunction(Value this_callable, List<Value> stack)
        {
            if (stack != null)
                foreach (Value v in stack)
                    StackPush(v);

            Type this_type = this_callable.GetType();
            ret[ret_count] = (Operand)(chunk.ProgramSize - 1);
            ret_count += 1;
            funCallEnv[executingInstructions + 1] = variablesBasesTop - 1;
            if (this_type == typeof(ValFunction))
            {
                ValFunction this_func = (ValFunction)this_callable;
                instructionsStack[executingInstructions + 1] = this_func.body;
                functionCallStack[executingInstructions + 1] = this_func;
                executingInstructions = executingInstructions + 1;
                IP = 0;
                //Console.WriteLine(this_func.name + " " + this_func.address);
                //Console.WriteLine("call");              
            }
            else if (this_type == typeof(ValClosure))
            {
                ValClosure this_closure = (ValClosure)this_callable;
                UpValuesPush();

                foreach (ValUpValue u in this_closure.upValues)
                {
                    upValues.Add(u);
                }
                //Console.WriteLine(this_closure.function.name + " " + this_closure.function.address);
                instructionsStack[executingInstructions + 1] = this_closure.function.body;
                functionCallStack[executingInstructions + 1] = this_closure.function;
                executingInstructions = executingInstructions + 1;
                IP = 0;
            }
            VMResult result = Run();
            if (result.status == VMResultType.OK)
                return result.value;
            return Value.Nil;
        }

        public VMResult Run()
        {
            Instruction instruction;
            //Console.WriteLine(System.Runtime.InteropServices.Marshal.SizeOf(chunk.ReadInstruction(IP)));
            //Console.WriteLine("All set, let's go!\n");
            while (true)
            {
                instruction = instructionsStack[executingInstructions][IP];
                //Console.Write(IP + " ");
                //Chunk.PrintInstruction(instruction);
                //Console.WriteLine();
                switch (instruction.opCode)
                {
                    case OpCode.LOADC:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Value constant = chunk.GetConstant(address);
                            //Console.WriteLine("loadc " + "address: " + address + " value: " + constant);
                            StackPush(constant);
                            break;
                        }
                    case OpCode.LOADV:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand n_shift = instruction.opB;
                            var variable = VarAt(address, (Operand)(variablesBasesTop - 1 - n_shift));
                            //Console.WriteLine("loadv " + "address: " + address + " env: " + (Operand)(variablesBases.Count - 1 - n_shift) + " value:" + variable);
                            StackPush(variable);
                            break;
                        }
                    case OpCode.LOADTABLEV:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand n_shift = instruction.opB;

                            Value this_table = VarAt(address, (Operand)(variablesBasesTop - 1 - n_shift)) as ValTable;

                            StackPush(this_table);
                            break;
                        }
                    case OpCode.LOADG:
                        {
                            IP++;
                            //Console.WriteLine("loadg in");
                            Operand address = instruction.opA;
                            Value global = globals[address];
                            //Console.WriteLine("loadg " + "address: " + address + " value: " + global);
                            StackPush(global);
                            break;
                        }
                    case OpCode.LOADI:
                        {
                            IP++;
                            //Console.WriteLine("loadg in");
                            Operand address = instruction.opA;
                            Operand module = instruction.opB;
                            Value global = modules[module].globals[address];
                            //Console.WriteLine("loadg " + "address: " + address + " value: " + global);
                            StackPush(global);
                            break;
                        }
                    case OpCode.LOADTABLEG:
                        {
                            IP++;
                            //Console.WriteLine("loadg in");
                            Operand address = instruction.opA;

                            Value this_table = globals[address] as ValTable;

                            StackPush(this_table);
                            break;
                        }
                    case OpCode.LOADTABLEI:
                        {
                            IP++;
                            //Console.WriteLine("loadg in");
                            Operand address = instruction.opA;
                            Operand module_index = instruction.opB;

                            Value this_table = modules[module_index].globals[address] as ValTable;

                            StackPush(this_table);
                            break;
                        }
                    case OpCode.LOADUPVAL:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            ValUpValue up_val = UpValuesAt(address);
                            //Console.WriteLine("loadupval " + up_val);
                            StackPush(up_val.Val);
                            break;
                        }
                    case OpCode.LOADTABLEUPVAL:
                        {
                            IP++;
                            Operand address = instruction.opA;

                            ValUpValue up_val = UpValuesAt(address);
                            Value this_table = up_val.Val;

                            StackPush(this_table);
                            break;
                        }
                    case OpCode.TABLEGET:
                        {
                            IP++;
                            Operand indexes_counter = instruction.opA;

                            Value[] indexes = new Value[indexes_counter];
                            for (int i = indexes_counter - 1; i >= 0; i--)
                                indexes[i] = StackPop();

                            Value value = StackPop();
                            foreach (Value v in indexes)
                            {
                                if (v.GetType() == typeof(ValNumber))
                                {
                                    value = (value as ValTable).elements[(int)((ValNumber)v).content];
                                }
                                else
                                {
                                    //Console.WriteLine(v.GetType());
                                    //Console.WriteLine(value);
                                    value = (value as ValTable).table[(ValString)v];
                                }
                            }
                            //Console.WriteLine(value);
                            StackPush(value);
                            break;
                        }

                    case OpCode.TABLESET:
                        {
                            IP++;
                            Operand indexes_counter = instruction.opA;

                            Value[] indexes = new Value[indexes_counter];
                            for (int i = indexes_counter - 1; i >= 0; i--)
                                indexes[i] = StackPop();

                            Value this_table = StackPop();

                            for (int i = 0; i < indexes_counter - 1; i++)
                            {
                                Value v = indexes[i];
                                if (v.GetType() == typeof(ValNumber))
                                {
                                    this_table = (this_table as ValTable).elements[(int)((ValNumber)v).content];
                                }
                                else
                                {
                                    this_table = (this_table as ValTable).table[(ValString)v];
                                }
                            }
                            Value new_value = StackPeek();
                            //Console.WriteLine("added tableset");
                            if (indexes[indexes_counter - 1].GetType() == typeof(ValNumber))
                            {
                                (this_table as ValTable).ElementSet((int)((ValNumber)indexes[indexes_counter - 1]).content, new_value);
                            }
                            else
                            {
                                (this_table as ValTable).TableSet((ValString)indexes[indexes_counter - 1], new_value);
                            }

                            break;
                        }

                    case OpCode.LOADNIL:
                        {
                            IP++;
                            StackPush(Value.Nil);
                            //Console.WriteLine("push at: " + (stack.Count - 1)+ " value:  " + value);
                            break;
                        }
                    case OpCode.LOADTRUE:
                        {
                            IP++;
                            StackPush(Value.True);
                            //Console.WriteLine("push at: " + (stack.Count - 1)+ " value:  " + value);
                            break;
                        }
                    case OpCode.LOADFALSE:
                        {
                            IP++;
                            StackPush(Value.False);
                            //Console.WriteLine("push at: " + (stack.Count - 1)+ " value:  " + value);
                            break;
                        }
                    case OpCode.LOADINTR:
                        {
                            IP++;
                            Operand value = instruction.opA;
                            StackPush(Intrinsics[value]);
                            //Console.WriteLine("push at: " + (stack.Count - 1)+ " value:  " + value);
                            break;
                        }
                    case OpCode.VARDCL:
                        {
                            IP++;
                            Value new_value = StackPop();
                            
                            if (variablesTop > (variables.Count - 1))
                            {
                                //Console.WriteLine("new");
                                variables.Add(new_value);
                                variablesTop++;
                            }
                            else
                            {
                                variables[variablesTop] = new_value;
                                variablesTop++;
                            }
                            //Console.WriteLine("vardcl adress: " + (variables.Count - 1) + " env:" + (variablesBases.Count - 1) + " value: " + new_var);
                            break;
                        }
                    case OpCode.GLOBALDCL:
                        {
                            IP++;
                            Value new_value = StackPop();
                            globals.Add(new_value);
                            //Console.WriteLine("globaldcl adress: " + (globals.Count - 1) + " value: " + new_var);
                            break;
                        }
                    case OpCode.FUNDCL:
                        {
                            //Console.WriteLine("fundcl in");
                            IP++;
                            Operand env = instruction.opA;
                            Operand lambda = instruction.opB;
                            int new_fun_adress = (int)((ValNumber)StackPop()).content;
                            Value this_callable = chunk.GetConstant((Operand)new_fun_adress);
                            if (this_callable.GetType() == typeof(ValFunction))
                            {
                                ValFunction this_function = (ValFunction)this_callable;
                                if (lambda == 0)
                                    if (env == 0)// Global
                                    {
                                        globals.Add(this_function);
                                    }
                                    else
                                    {
                                        if (variablesTop >= (variables.Count))
                                        {
                                            variables.Add(this_function);
                                            variablesTop++;
                                        }
                                        else
                                        {
                                            variables[variablesTop] = this_function;
                                            variablesTop++;
                                        }
                                    }
                                else
                                    StackPush(this_function);
                                //Console.WriteLine("fundcl " + this_function);
                            }
                            else
                            {
                                //Console.WriteLine("fundcl declaring closure");
                                ValClosure this_closure = (ValClosure)this_callable;

                                // new upvalues
                                List<ValUpValue> new_upValues = new List<ValUpValue>();
                                foreach (ValUpValue u in this_closure.upValues)
                                {
                                    // here we convert env from shift based to absolute based
                                    ValUpValue new_upvalue = new ValUpValue(u.address, (Operand)(variablesBasesTop - u.env));
                                    //VarAt(u.address, (Operand)(variablesBasesTop -u.env)).references++;
                                    new_upValues.Add(new_upvalue);
                                }
                                //Console.WriteLine("fundcl declaring closure mid");
                                ValClosure new_closure = new ValClosure(this_closure.function, new_upValues);

                                //Console.WriteLine("fundcl declaring closure mid 2");

                                new_closure.Register(variables, variablesBases);
                                foreach (ValUpValue u in new_closure.upValues)
                                {
                                    RegisterUpValue(u);                                    
                                    //Console.WriteLine("added " + u.env.ToString() + " " + u.address + " value " + u.Val);
                                }
                                //Console.WriteLine(new_closure.upValues.Count);
                                if (lambda == 0)
                                    if (env == 0)// yes they exist!
                                    {
                                        globals.Add(new_closure);
                                    }
                                    else
                                    {
                                        if (variablesTop >= (variables.Count))
                                        {
                                            variables.Add(new_closure);
                                            variablesTop++;
                                        }
                                        else
                                        {
                                            variables[variablesTop] = new_closure;
                                            variablesTop++;
                                        }
                                    }
                                else
                                    StackPush(new_closure);
                                //Console.WriteLine("fundcl closure " + new_closure);
                            }
                            break;
                        }
                    case OpCode.ASSIGN:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Operand n_shift = instruction.opB;
                            Value new_value = StackPeek();
                            Value old_value = VarAt(address, (Operand)(variablesBasesTop - 1 - n_shift));
                            VarSet(new_value, address, (Operand)(variablesBasesTop - 1 - n_shift));


                            //valPool.Recycle(old_value);

                            //Console.WriteLine("assign adress: " + address + " env:" + (Operand)(variablesBases.Count - 1 - n_shift) + " value: " + new_value);
                            break;
                        }
                    case OpCode.ASSIGNG:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            Value new_value = StackPeek();
                            Value old_value = globals[address];
                            globals[address] = new_value;


                            //valPool.Recycle(old_value);

                            //Console.WriteLine("assigng adress: " + address + " value: " + globals[address]);
                            break;
                        }
                    case OpCode.ASSIGNUPVAL:
                        {
                            IP++;
                            Operand address = instruction.opA;
                            ValUpValue this_value = UpValuesAt(address);
                            //Console.WriteLine(this_value.GetType() + " " + this_value + StackPeek());
                            Value new_value = StackPeek();
                            Value old_value = this_value.Val;
                            this_value.Val = new_value;

                            //new_value.references++;
                            //old_value.references--;
                            //valPool.Recycle(old_value);
                            break;
                        }
                    case OpCode.PUSH:
                        {
                            IP++;
                            Operand value = instruction.opA;
                            Value new_value = new ValNumber(value);
                            StackPush(new_value);
                            //Console.WriteLine("push at: " + (stack.Count - 1)+ " value:  " + value);
                            break;
                        }
                    case OpCode.POP:
                        {
                            //Console.WriteLine("in pop");
                            IP++;
                            StackPop();
                            //Console.WriteLine("pop at:" + stack.Count);
                            break;
                        }
                    case OpCode.JMP:
                        {
                            Operand value = instruction.opA;
                            IP += value;
                            //Console.WriteLine("jmp " + value);
                            break;
                        }
                    case OpCode.JNT:
                        {
                            bool value = StackPop().ToBool();
                            if (value == false)
                            {
                                //Console.WriteLine("false");
                                IP += instruction.opA;
                            }
                            else
                            {
                                //Console.WriteLine("true");
                                IP++;
                            }
                            break;
                        }
                    case OpCode.JMPB:
                        {
                            Operand value = instruction.opA;
                            IP -= value;
                            break;
                        }
                    case OpCode.NENV:
                        {
                            IP++;
                            EnvPush();
                            //Console.WriteLine("nenv " + (variablesBases.Count -1));
                            break;
                        }
                    case OpCode.CENV:
                        {
                            IP++;
                            EnvPop();
                            //Console.WriteLine("cenv " + variablesBases.Count);
                            break;
                        }
                    case OpCode.RET:
                        {
                            IP = ret[ret_count - 1];
                            ret_count -= 1;
                            //Console.WriteLine("ret to: " + IP + " Count: " + ret.Count);
                            break;
                        }
                    case OpCode.RETSREL:
                        {
                            Operand value = instruction.opA;
                            ret[ret_count] = ((Operand)(value + IP));
                            ret_count += 1;
                            //Console.WriteLine("retsrel " + (value + IP);
                            IP++;
                            break;
                        }
                    case OpCode.ADD:
                        {
                            //Console.WriteLine("add");
                            IP++;
                            ValNumber opB = (ValNumber)StackPop();
                            ValNumber opA = (ValNumber)StackPop();

                            Number result = opA.content + opB.content;
                            Value new_value = new ValNumber(result);
                            StackPush(new_value);

                            break;
                        }
                    case OpCode.APP:
                        {
                            //Console.WriteLine("app");
                            IP++;
                            Value opB = StackPop();
                            Value opA = StackPop();

                            string result = opA.ToString() + opB.ToString();
                            Value new_value = new ValString(result);
                            StackPush(new_value);

                            break;
                        }
                    case OpCode.SUB:
                        {
                            //Console.WriteLine("sub");
                            IP++;
                            ValNumber opB = (ValNumber)StackPop();
                            ValNumber opA = (ValNumber)StackPop();
                            Number result = opA.content - opB.content;
                            Value new_value = new ValNumber(result);
                            StackPush(new_value);
                            break;
                        }
                    case OpCode.MUL:
                        {
                            //Console.WriteLine("mul");
                            IP++;
                            ValNumber opB = (ValNumber)StackPop();
                            ValNumber opA = (ValNumber)StackPop();
                            Number result = opA.content * opB.content;
                            Value new_value = new ValNumber(result);
                            StackPush(new_value);
                            break;
                        }
                    case OpCode.DIV:
                        {
                            //Console.WriteLine("div");
                            IP++;
                            ValNumber opB = (ValNumber)StackPop();
                            ValNumber opA = (ValNumber)StackPop();
                            Number result = opA.content / opB.content;
                            Value new_value = new ValNumber(result);
                            StackPush(new_value);
                            break;
                        }
                    case OpCode.NEG:
                        {
                            //Console.WriteLine("neg");
                            IP++;
                            ValNumber opA = (ValNumber)StackPop();
                            Value new_value = new ValNumber(opA.content * -1);
                            StackPush(new_value);                            
                            break;
                        }
                    case OpCode.EQ:
                        {
                            //Console.WriteLine("eq");
                            IP++;
                            Value opB = StackPop();
                            Value opA = StackPop();
                            if (opA.Equals(opB))
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }

                            break;
                        }
                    case OpCode.NEQ:
                        {
                            //Console.WriteLine("neq");
                            IP++;
                            Value opB = StackPop();
                            Value opA = StackPop();

                            if (!opA.Equals(opB))
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }

                            break;
                        }
                    case OpCode.GTQ:
                        {
                            //Console.WriteLine("gtq");
                            IP++;
                            ValNumber opB = (ValNumber)StackPop();
                            ValNumber opA = (ValNumber)StackPop();
                            bool truthness = opA.content >= opB.content;
                            if (truthness == true)
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }
                            break;
                        }
                    case OpCode.LTQ:
                        {
                            //Console.WriteLine("ltq");
                            IP++;
                            ValNumber opB = (ValNumber)StackPop();
                            ValNumber opA = (ValNumber)StackPop();
                            bool truthness = opA.content <= opB.content;
                            if (truthness == true)
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }
                            break;
                        }
                    case OpCode.GT:
                        {
                            //Console.WriteLine("gt");
                            IP++;
                            ValNumber opB = (ValNumber)StackPop();
                            ValNumber opA = (ValNumber)StackPop();
                            bool truthness = opA.content > opB.content;
                            if (truthness == true)
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }
                            break;
                        }
                    case OpCode.LT:
                        {
                            //Console.WriteLine("lt");
                            IP++;
                            ValNumber opB = (ValNumber)StackPop();
                            ValNumber opA = (ValNumber)StackPop();
                            bool truthness = opA.content < opB.content;
                            if (truthness == true)
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }
                            break;
                        }
                    case OpCode.NOT:
                        {
                            //Console.WriteLine("not");
                            IP++;
                            Value opA = StackPop();
                            if (opA.ToBool() == true)
                                StackPush(Value.False);
                            else if (opA.ToBool() == false)
                                StackPush(Value.True);
                            else
                            {
                                Error("NOT is insane!");
                                return new VMResult(VMResultType.ERROR, Value.Nil);
                            }

                            break;
                        }
                    case OpCode.AND:
                        {
                            //Console.WriteLine("and");
                            IP++;
                            Value opB = StackPop();
                            Value opA = StackPop();
                            bool result = opA.ToBool() && opB.ToBool();
                            if (result == true)
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }
                            break;
                        }
                    case OpCode.OR:
                        {
                            //Console.WriteLine("or");
                            IP++;
                            Value opB = StackPop();
                            Value opA = StackPop();

                            bool result = opA.ToBool() || opB.ToBool();
                            if (result == true)
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }
                            break;
                        }
                    case OpCode.XOR:
                        {
                            //Console.WriteLine("xor");
                            IP++;
                            Value opB = StackPop();
                            Value opA = StackPop();
                            bool result = opA.ToBool() ^ opB.ToBool();
                            if (result == true)
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }
                            break;
                        }
                    case OpCode.NAND:
                        {
                            //Console.WriteLine("nand");
                            IP++;
                            Value opB = StackPop();
                            Value opA = StackPop();
                            bool result = !(opA.ToBool() && opB.ToBool());
                            if (result == true)
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }
                            break;
                        }
                    case OpCode.NOR:
                        {
                            //Console.WriteLine("nor");
                            IP++;
                            Value opB = StackPop();
                            Value opA = StackPop();
                            bool result = !(opA.ToBool() || opB.ToBool());
                            if (result == true)
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }
                            break;
                        }
                    case OpCode.XNOR:
                        {
                            //Console.WriteLine("xnor");
                            IP++;
                            Value opB = StackPop();
                            Value opA = StackPop();
                            bool result = !(opA.ToBool() ^ opB.ToBool());
                            if (result == true)
                            {
                                StackPush(Value.True);
                            }
                            else
                            {
                                StackPush(Value.False);
                            }
                            break;
                        }
                    case OpCode.CLOSURECLOSE:
                        {
                            UpValuesPop();
                            int target_env = funCallEnv[executingInstructions];
                            EnvSet(target_env);
                            IP = ret[ret_count - 1];
                            ret_count -= 1;
                            executingInstructions = executingInstructions - 1;
                            break;
                        }
                    case OpCode.FUNCLOSE:
                        {
                            int target_env = funCallEnv[executingInstructions];
                            EnvSet(target_env);
                            //instructionsStack.Pop();
                            //functionCallStack.Pop();
                            //executingInstructions = instructionsStack.Peek();
                            IP = ret[ret_count - 1];
                            ret_count -= 1;
                            executingInstructions = executingInstructions - 1;
                            break;
                        }
                    case OpCode.NTABLE:
                        {
                            IP++;

                            ValTable new_table = new ValTable(null, null);

                            int n_table = instruction.opB;                            
                            for (int i = 0; i < n_table; i++)
                            {
                                Value val = StackPop();
                                Value key = StackPop();
                                new_table.table.Add((ValString)key, val);
                            }

                            int n_elements = instruction.opA;
                            for (int i = 0; i < n_elements; i++)
                            {
                                Value new_value = StackPop();
                                new_table.elements.Add(new_value);
                            }
                                                        
                            //new_table.references++;
                            StackPush(new_table);
                            break;
                        }
                    case OpCode.CALL:
                        {
                            IP++;

                            Value this_callable = StackPop();
                            Type this_type = this_callable.GetType();

                            if (this_type == typeof(ValFunction))
                            {
                                ret[ret_count] = IP;// add return address to stack
                                ret_count += 1;
                                funCallEnv[executingInstructions + 1] = variablesBasesTop - 1;// sets the return env to funclose/closureclose                                
                                ValFunction this_func = (ValFunction)this_callable;
                                instructionsStack[executingInstructions + 1] = this_func.body;
                                functionCallStack[executingInstructions + 1] = this_func;
                                executingInstructions = executingInstructions + 1;
                                IP = 0;
                                //Console.WriteLine(this_func.name + " " + this_func.address);
                                //Console.WriteLine("call");              
                            }
                            else if (this_type == typeof(ValClosure))
                            {
                                ret[ret_count] = IP;// add return address to stack
                                ret_count += 1;
                                funCallEnv[executingInstructions + 1] = variablesBasesTop - 1;// sets the return env to funclose/closureclose 

                                ValClosure this_closure = (ValClosure)this_callable;
                                UpValuesPush();

                                foreach (ValUpValue u in this_closure.upValues)
                                {
                                    upValues.Add(u);
                                }
                                //Console.WriteLine(this_closure.function.name + " " + this_closure.function.address);
                                instructionsStack[executingInstructions + 1] = this_closure.function.body;
                                functionCallStack[executingInstructions + 1] = this_closure.function;
                                executingInstructions = executingInstructions + 1;
                                IP = 0;
                            }
                            else if (this_type == typeof(ValIntrinsic))
                            {
                                ValIntrinsic this_intrinsic = (ValIntrinsic)this_callable;
                                Value result = this_intrinsic.function(this);
                                //stack.RemoveRange(stack.Count - this_intrinsic.arity, this_intrinsic.arity);
                                stackTop -= this_intrinsic.arity;
                                StackPush(result);
                                break;
                            }
                            else
                            {
                                Error("Trying to call a " + this_callable.GetType());
                                return new VMResult(VMResultType.OK, Value.Nil);
                            }
                            //Console.WriteLine("call out");
                            break;
                        }
                    case OpCode.STASHTOP:
                        {
                            IP++;

                            stash[stashTop] = StackPop();
                            stashTop++;
                            break;
                        }
                    case OpCode.POPSTASH:
                        {
                            IP++;
                            StackPush(stash[stashTop - 1]);
                            stashTop--;
                            break;
                        }
                    case OpCode.EXIT:
                        {
                            Value result = Value.Nil;
                            if (stackTop > 0)
                                result = StackPop();

                            return new VMResult(VMResultType.OK, result);
                        }
                    default:
                        Error("Unkown OpCode: " + instruction.opCode);
                        return new VMResult(VMResultType.ERROR, Value.Nil);
                }
            }
        }

        void Stats()
        {
            Console.WriteLine("\nStack:");
            int counter = 0;
            foreach (Value v in stack)
            {
                Console.WriteLine(counter + ": " + v);
                counter++;
            }
            if (counter == 0) Console.WriteLine("empty :)");

            Console.WriteLine("\nVariables:");
            counter = 0;
            foreach (Value v in variables)
            {
                Console.WriteLine(counter + ": " + v);
                counter++;
            }
            if (counter == 0) Console.WriteLine("empty :)");

            Console.WriteLine("Envs: " + variablesBasesTop);

            Console.WriteLine("\nUpvalues:");
            counter = 0;
            foreach (Value v in upValues)
            {
                Console.WriteLine(counter + ": " + v);
                counter++;
            }
            if (counter == 0) Console.WriteLine("empty :)");

            Console.WriteLine("Envs: " + upValuesBasesTop);

            Console.WriteLine("\nUpvaluesRegistry:");
            counter = 0;
            foreach (Value v in upValuesRegistry)
            {
                Console.WriteLine(counter + ": " + v);
                counter++;
            }
            if (counter == 0) Console.WriteLine("empty :)");

            Console.WriteLine("Envs: " + upValuesRegistryBasesTop);
        }
    }
}

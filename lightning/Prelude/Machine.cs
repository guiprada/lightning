using System;
using System.Collections.Generic;

using lightningUnit;
using lightningVM;
namespace lightningPrelude
{
    public class Machine
    {
        public static TableUnit GetTableUnit()
        {
            TableUnit machine = new TableUnit(null);

            Unit MemoryUse(VM p_vm){
                TableUnit mem_use = new TableUnit(null);
                mem_use.Set("stack_count", p_vm.StackCount());
                mem_use.Set("globals_count", p_vm.GlobalsCount());
                mem_use.Set("variables_count", p_vm.VariablesCount());
                mem_use.Set("variables_capacity", p_vm.VariablesCapacity());
                mem_use.Set("upvalues_count", p_vm.UpValuesCount());
                mem_use.Set("upvalue_capacity", p_vm.UpValueCapacity());

                return new Unit(mem_use);
            }
            machine.Set("memory_use", new IntrinsicUnit("memory_use", MemoryUse, 0));

            //////////////////////////////////////////////////////
            Unit Modules(VM p_vm)
            {
                string modules = "";
                bool first = true;
                foreach (KeyValuePair<string, int> entry in p_vm.LoadedModules)
                {
                    if (first == true)
                    {
                        modules += entry.Key;
                        first = false;
                    }
                    else
                    {
                        modules += " " + entry.Key;
                    }
                }

                return new Unit(modules);
            }
            machine.Set("modules", new IntrinsicUnit("modules", Modules, 0));

            //////////////////////////////////////////////////////
            Unit ResourcesTrim(VM p_vm)
            {
                p_vm.ResoursesTrim();
                return new Unit(true);
            }
            machine.Set("trim", new IntrinsicUnit("trim", ResourcesTrim, 0));

            //////////////////////////////////////////////////////
            Unit ReleaseAllVMs(VM p_vm)
            {
                p_vm.ReleaseVMs();
                return new Unit(true);
            }
            machine.Set("release_all_vms", new IntrinsicUnit("release_all_vms", ReleaseAllVMs, 0));

            //////////////////////////////////////////////////////
            Unit ReleaseVMs(VM p_vm)
            {
                p_vm.ReleaseVMs((int)p_vm.GetInteger(0));
                return new Unit(true);
            }
            machine.Set("release_vms", new IntrinsicUnit("release_vms", ReleaseVMs, 1));

            //////////////////////////////////////////////////////
            Unit CountVMs(VM p_vm)
            {
                return new Unit(p_vm.CountVMs());
            }
            machine.Set("count_vms", new IntrinsicUnit("count_vms", CountVMs, 0));

            //////////////////////////////////////////////////////
            Unit ProcessorCount(VM p_vm)
            {
                return new Unit(Environment.ProcessorCount);
            }
            machine.Set("processor_count", new IntrinsicUnit("processor_count", ProcessorCount, 0));

            return machine;
        }
    }
}
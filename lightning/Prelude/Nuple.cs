#if DOUBLE
    using Float = System.Double;
    using Integer = System.Int64;
    using Operand = System.UInt16;
#else
    using Float = System.Single;
    using Integer = System.Int32;
    using Operand = System.UInt16;
#endif

namespace lightning
{
    public class Nuple
    {
        public static TableUnit GetTableUnit()
        {
            TableUnit nuple = new TableUnit(null);
            TableUnit nupleMethods = new TableUnit(null);

            Unit NupleNew(VM p_vm)
            {
                Integer size = p_vm.GetInteger(0);
                Unit[] new_nuple = new Unit[size];
                for(int i = 0; i < size; i++)
                    new_nuple[i] = new Unit(UnitType.Null);
                WrapperUnit<Unit[]> new_nuple_object = new WrapperUnit<Unit[]>(new_nuple, nupleMethods);

                return new Unit(new_nuple_object);
            }
            nuple.Set("new", new IntrinsicUnit("tuple_new", NupleNew, 1));

            //////////////////////////////////////////////////////
            Unit NupleFromList(VM p_vm)
            {
                ListUnit this_list = p_vm.GetList(0);
                int size = this_list.Count;
                Unit[] new_nuple = new Unit[size];
                for(int i = 0; i < size; i++)
                    new_nuple[i] = this_list.Elements[i];
                WrapperUnit<Unit[]> new_nuple_object = new WrapperUnit<Unit[]>(new_nuple, nupleMethods);

                return new Unit(new_nuple_object);
            }
            nuple.Set("from_list", new IntrinsicUnit("tuple_from_list", NupleFromList, 1));

            //////////////////////////////////////////////////////
            Unit NupleGet(VM p_vm)
            {
                Unit[] this_nuple = p_vm.GetWrappedContent<Unit[]>(0);

                return this_nuple[(int)p_vm.GetInteger(1)];
            }
            nupleMethods.Set("get", new IntrinsicUnit("nuple_get", NupleGet, 2));

            //////////////////////////////////////////////////////
            Unit NupleSet(VM p_vm)
            {
                p_vm.GetWrappedContent<Unit[]>(0)[(int)p_vm.GetInteger(1)] = p_vm.GetUnit(2);

                return new Unit(UnitType.Null);
            }
            nupleMethods.Set("set", new IntrinsicUnit("nuple_set", NupleSet, 3));

            //////////////////////////////////////////////////////
            Unit NupleSize(VM p_vm)
            {
                Unit[] this_nuple = p_vm.GetWrappedContent<Unit[]>(0);

                return new Unit(this_nuple.Length);
            }
            nupleMethods.Set("size", new IntrinsicUnit("nuple_size", NupleSize, 1));

            return nuple;
        }
    }
}
namespace lightning
{
    public class Tuple
    {
        public static TableUnit GetTableUnit()
        {
            TableUnit tuple = new TableUnit(null);
            TableUnit tupleMethods = new TableUnit(null);

            Unit TupleNew(VM p_vm)
            {
                Unit[] new_tuple = new Unit[2];
                new_tuple[0] = p_vm.GetUnit(0);
                new_tuple[1] = p_vm.GetUnit(1);

                WrapperUnit<Unit[]> tuple_object = new WrapperUnit<Unit[]>(new_tuple, tupleMethods);

                return new Unit(tuple_object);
            }
            tuple.Set("new", new IntrinsicUnit("tuple_new", TupleNew, 2));

            //////////////////////////////////////////////////////
            Unit TupleGetX(VM p_vm)
            {
                Unit[] this_tuple = p_vm.GetWrappedContent<Unit[]>(0);

                return this_tuple[0];
            }
            tupleMethods.Set("get_x", new IntrinsicUnit("tuple_get_x", TupleGetX, 1));

            //////////////////////////////////////////////////////
            Unit TupleGetY(VM p_vm)
            {
                Unit[] this_tuple = p_vm.GetWrappedContent<Unit[]>(0);

                return this_tuple[1];
            }
            tupleMethods.Set("get_y", new IntrinsicUnit("tuple_get_y", TupleGetY, 1));

            //////////////////////////////////////////////////////
            Unit TupleSetX(VM p_vm)
            {
                p_vm.GetWrappedContent<Unit[]>(0)[0] = p_vm.GetUnit(1);

                return new Unit(UnitType.Null);
            }
            tupleMethods.Set("set_x", new IntrinsicUnit("tuple_set_x", TupleSetX, 2));

            //////////////////////////////////////////////////////
            Unit TupleSetY(VM p_vm)
            {
                p_vm.GetWrappedContent<Unit[]>(0)[1] = p_vm.GetUnit(1);

                return new Unit(UnitType.Null);
            }
            tupleMethods.Set("set_y", new IntrinsicUnit("tuple_set_y", TupleSetY, 2));

            return tuple;
        }
    }
}
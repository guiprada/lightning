using System;
using System.Collections.Generic;

using lightningVM;

namespace lightningUnit
{
    public class ResultUnit : HeapUnit
    {
        public Unit Value { get; private set; }
        public bool IsOK { get; private set; }
        public Exception E { get; private set;}

        public override UnitType Type
        {
            get
            {
                return UnitType.Result;
            }
        }

        public ResultUnit(Exception p_e)
        {
            E = p_e;
            IsOK = false;
        }
        public ResultUnit(string p_error_string)
        {
            E = new Exception(p_error_string);
            IsOK = false;
        }
        public ResultUnit(Unit p_value)
        {
            Value = p_value;
            IsOK = true;
        }

        public override String ToString()
        {
            if (IsOK)
                return "Result OK: " +  Value.ToString();
            else
                return "Result Error: " + E.ToString();
        }

        public override bool Equals(object other)
        {
            throw new Exception("Trying to check equality of ResultUnit!");
        }

        public override int CompareTo(object p_compareTo)
        {
            throw new Exception("Trying to compare with ResultUnit!");
        }

        public override int GetHashCode()
        {
            if (IsOK)
                return "Result OK".GetHashCode() +  Value.GetHashCode();
            else
                return "Result Error".GetHashCode() + Value.GetHashCode();
        }

        public override Unit Get(Unit p_key)
        {
            return methodTable.Get(p_key);
        }

        public bool OK()
        {
            return IsOK;
        }

        public Unit UnWrap()
        {
            if (!IsOK)
                throw new AggregateException(
                    new Exception[]{
                        new Exception("Trying to get result out of failed operation!"),
                        E
                    });
            else if (Value.Type == UnitType.Empty)
                throw new Exception("Trying to get result out of void operation!");
            else
                return Value;
        }

        public static Unit UnWrap(Unit p_value)
        {
            if (p_value.Type == UnitType.Result)
            {
                return ((ResultUnit)p_value.heapUnitValue).UnWrap();
            }
            return p_value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////// Table
        private static TableUnit methodTable = new TableUnit(null);
        public static TableUnit ClassMethodTable { get { return methodTable; } }
        static ResultUnit()
        {
            initMethodTable();
        }
        private static void initMethodTable()
        {
            {
                Unit OK (VM vm)
                {
                    ResultUnit this_option = vm.GetResultUnit(0);

                    return new Unit(this_option.OK());
                }
                methodTable.Set("is_ok", new IntrinsicUnit("result_is_ok", OK, 1));

                //////////////////////////////////////////////////////
                Unit Error (VM vm)
                {
                    ResultUnit this_option = vm.GetResultUnit(0);

                    return new Unit(!this_option.OK());
                }
                methodTable.Set("is_error", new IntrinsicUnit("result_is_error", Error, 1));

                //////////////////////////////////////////////////////
                Unit Unwrap (VM vm)
                {
                    ResultUnit this_option = vm.GetResultUnit(0);

                    return this_option.UnWrap();
                }
                methodTable.Set("unwrap", new IntrinsicUnit("result_unwrap", Unwrap, 1));

                //////////////////////////////////////////////////////
            }
        }
    }
}
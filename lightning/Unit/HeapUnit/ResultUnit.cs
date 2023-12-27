using System;
using System.Collections.Generic;

using lightningVM;

namespace lightningUnit
{
    public class ResultUnit : HeapUnit
    {
        public Unit Value { get; private set; }
        public Exception E { get; private set;}
        public bool IsOK { get; private set; }
        public bool HasResult {
            get
            {
                return !(Value.Type == UnitType.Empty);
            }
        }

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
            Value = new Unit(UnitType.Empty);
        }
        public ResultUnit(string p_error_string)
        {
            E = new Exception(p_error_string);
            IsOK = false;
            Value = new Unit(UnitType.Empty);
        }
        public ResultUnit(Unit p_value)
        {
            if(p_value.Type == UnitType.Void)
            {
                E = new Exception("Trying to assign a void value");
                IsOK = true;
                Value = new Unit(UnitType.Void);
            }
            else
            {
                E = null;
                IsOK = true;
                Value = p_value;
            }
        }
        public ResultUnit()
        {
            E = null;
            IsOK = true;
            Value = new Unit(UnitType.Empty);
        }

        public override String ToString()
        {
            if (IsOK)
                if (HasResult)
                    return "Result OK: " +  Value.ToString();
                else
                    return "Result OK: no value";
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
                Unit IsOK (VM vm)
                {
                    ResultUnit this_result = vm.GetResultUnit(0);

                    return new Unit(this_result.IsOK);
                }
                methodTable.Set("is_ok", new IntrinsicUnit("result_is_ok", IsOK, 1));

                //////////////////////////////////////////////////////
                Unit IsError (VM vm)
                {
                    ResultUnit this_result = vm.GetResultUnit(0);

                    return new Unit(!this_result.IsOK);
                }
                methodTable.Set("is_error", new IntrinsicUnit("result_is_error", IsError, 1));

                //////////////////////////////////////////////////////
                Unit HasResult (VM vm)
                {
                    ResultUnit this_result = vm.GetResultUnit(0);

                    return new Unit(this_result.HasResult);
                }
                methodTable.Set("has_result", new IntrinsicUnit("result_has_result", HasResult, 1));

                //////////////////////////////////////////////////////
                Unit Unwrap (VM vm)
                {
                    ResultUnit this_result = vm.GetResultUnit(0);

                    return this_result.UnWrap();
                }
                methodTable.Set("unwrap", new IntrinsicUnit("result_unwrap", Unwrap, 1));

                //////////////////////////////////////////////////////
                Unit GetError (VM vm)
                {
                    ResultUnit this_result = vm.GetResultUnit(0);

                    return new Unit(new StringUnit(this_result.E.ToString()));
                }
                methodTable.Set("get_error", new IntrinsicUnit("result_get_error", GetError, 1));
            }
        }
    }
}
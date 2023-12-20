#if DOUBLE
    using Float = System.Double;
    using Integer = System.Int64;
    using Operand = System.UInt16;
#else
    using Float = System.Single;
    using Integer = System.Int32;
    using Operand = System.UInt16;
#endif

using System;

using lightningUnit;
using lightningVM;
namespace lightningPrelude
{
    public class LightningMath
    {
        public static TableUnit GetTableUnit()
        {
            TableUnit math = new TableUnit(null);

            math.Set("pi", (Float)Math.PI);
            math.Set("e", (Float)Math.E);
#if DOUBLE
            math.Set("double", true);
#else
            math.Set("double", false);
#endif

            //////////////////////////////////////////////////////
            Unit Sin(VM p_vm)
            {
                return new Unit((Float)Math.Sin(p_vm.GetNumber(0)));
            }
            math.Set("sin", new IntrinsicUnit("sin", Sin, 1));

            //////////////////////////////////////////////////////
            Unit Cos(VM p_vm)
            {
                return new Unit((Float)Math.Cos(p_vm.GetNumber(0)));
            }
            math.Set("cos", new IntrinsicUnit("cos", Cos, 1));

            //////////////////////////////////////////////////////
            Unit Tan(VM p_vm)
            {
                return new Unit((Float)Math.Tan(p_vm.GetNumber(0)));
            }
            math.Set("tan", new IntrinsicUnit("tan", Tan, 1));

            //////////////////////////////////////////////////////
            Unit Sec(VM p_vm)
            {
                return new Unit((Float)(1 / Math.Cos(p_vm.GetNumber(0))));
            }
            math.Set("sec", new IntrinsicUnit("sec", Sec, 1));

            //////////////////////////////////////////////////////
            Unit Cosec(VM p_vm)
            {
                return new Unit((Float)(1 / Math.Sin(p_vm.GetNumber(0))));
            }
            math.Set("cosec", new IntrinsicUnit("cosec", Cosec, 1));

            //////////////////////////////////////////////////////
            Unit Cotan(VM p_vm)
            {
                return new Unit((Float)(1 / Math.Tan(p_vm.GetNumber(0))));
            }
            math.Set("cotan", new IntrinsicUnit("cotan", Cotan, 1));

            //////////////////////////////////////////////////////
            Unit Asin(VM p_vm)
            {
                return new Unit((Float)Math.Asin(p_vm.GetNumber(0)));
            }
            math.Set("asin", new IntrinsicUnit("asin", Asin, 1));

            //////////////////////////////////////////////////////
            Unit Acos(VM p_vm)
            {
                return new Unit((Float)Math.Acos(p_vm.GetNumber(0)));
            }
            math.Set("acos", new IntrinsicUnit("acos", Acos, 1));

            //////////////////////////////////////////////////////
            Unit Atan(VM p_vm)
            {
                return new Unit((Float)Math.Atan(p_vm.GetNumber(0)));
            }
            math.Set("atan", new IntrinsicUnit("atan", Atan, 1));

            //////////////////////////////////////////////////////
            Unit Sinh(VM p_vm)
            {
                return new Unit((Float)Math.Sinh(p_vm.GetNumber(0)));
            }
            math.Set("sinh", new IntrinsicUnit("sinh", Sinh, 1));

            //////////////////////////////////////////////////////
            Unit Cosh(VM p_vm)
            {
                return new Unit((Float)Math.Cosh(p_vm.GetNumber(0)));
            }
            math.Set("cosh", new IntrinsicUnit("cosh", Cosh, 1));

            //////////////////////////////////////////////////////
            Unit Tanh(VM p_vm)
            {
                return new Unit((Float)Math.Tanh(p_vm.GetNumber(0)));
            }
            math.Set("tanh", new IntrinsicUnit("tanh", Tanh, 1));

            //////////////////////////////////////////////////////
            Unit Pow(VM p_vm)
            {
                Float value = p_vm.GetNumber(0);
                Float exponent = p_vm.GetNumber(1);
                return new Unit((Float)Math.Pow(value, exponent));
            }
            math.Set("pow", new IntrinsicUnit("pow", Pow, 2));

            //////////////////////////////////////////////////////
            Unit Root(VM p_vm)
            {
                Float value = p_vm.GetNumber(0);
                Float exponent = p_vm.GetNumber(1);
                return new Unit((Float)Math.Pow(value, 1 / exponent));
            }
            math.Set("root", new IntrinsicUnit("root", Root, 2));

            //////////////////////////////////////////////////////
            Unit Sqroot(VM p_vm)
            {
                Float value = p_vm.GetNumber(0);
                return new Unit((Float)Math.Sqrt(value));
            }
            math.Set("sqroot", new IntrinsicUnit("sqroot", Sqroot, 1));
            //////////////////////////////////////////////////////
            Unit Exp(VM p_vm)
            {
                Float exponent = p_vm.GetNumber(0);
                return new Unit((Float)Math.Exp(exponent));
            }
            math.Set("exp", new IntrinsicUnit("exp", Exp, 1));

            //////////////////////////////////////////////////////
            Unit Log(VM p_vm)
            {
                Float value = p_vm.GetNumber(0);
                Float this_base = p_vm.GetNumber(1);
                return new Unit((Float)Math.Log(value, this_base));
            }
            math.Set("log", new IntrinsicUnit("log", Log, 2));

            //////////////////////////////////////////////////////
            Unit Ln(VM p_vm)
            {
                Float value = p_vm.GetNumber(0);
                return new Unit((Float)Math.Log(value, Math.E));
            }
            math.Set("ln", new IntrinsicUnit("ln", Ln, 1));

            //////////////////////////////////////////////////////
            Unit Log10(VM p_vm)
            {
                Float value = p_vm.GetNumber(0);
                return new Unit((Float)Math.Log(value, (Float)10));
            }
            math.Set("log10", new IntrinsicUnit("log10", Log10, 1));

            //////////////////////////////////////////////////////
            Unit Mod(VM p_vm)
            {
                Float value1 = p_vm.GetNumber(0);
                Float value2 = p_vm.GetNumber(1);
                return new Unit(value1 % value2);
            }
            math.Set("mod", new IntrinsicUnit("mod", Mod, 2));

            //////////////////////////////////////////////////////
            Unit Idiv(VM p_vm)
            {
                Float value1 = p_vm.GetNumber(0);
                Float value2 = p_vm.GetNumber(1);
                return new Unit((Integer)(value1 / value2));
            }
            math.Set("idiv", new IntrinsicUnit("idiv", Idiv, 2));

            return math;
        }
    }
}
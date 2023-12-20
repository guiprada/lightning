using System;

using lightningUnit;
using lightningVM;
namespace lightningPrelude
{
    public class Rand
    {
        public static TableUnit GetTableUnit()
        {
            TableUnit rand = new TableUnit(null);

            var rng = new Random();

            Unit NextInt(VM p_vm)
            {
                int max = (int)(p_vm.GetInteger(0));
                return new Unit(rng.Next(max));
            }
            rand.Set("int", new IntrinsicUnit("int", NextInt, 1));

            Unit NextFloat(VM p_vm)
            {
                return new Unit((Float)rng.NextDouble());
            }
            rand.Set("float", new IntrinsicUnit("float", NextFloat, 0));

            return rand;
        }
    }
}
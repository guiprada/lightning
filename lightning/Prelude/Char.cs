using System;

using lightningUnit;
using lightningVM;
namespace lightningPrelude
{
    public class LightningChar
    {
        public static TableUnit GetTableUnit()
        {
            TableUnit char_table = new TableUnit(null);

            //////////////////////////////////////////////////////

            Unit IsAlpha(VM p_vm)
            {
                string input_string = p_vm.GetString(0);
                if (1 <= input_string.Length)
                {
                    char head = input_string[0];
                    if (Char.IsLetter(head))
                    {
                        return new Unit(true);
                    }
                }
                return new Unit(false);
            }
            char_table.Set("is_alpha", new IntrinsicUnit("char_is_alpha", IsAlpha, 1));

            //////////////////////////////////////////////////////

            Unit IsDigit(VM p_vm)
            {
                string input_string = p_vm.GetString(0);
                if (1 <= input_string.Length)
                {
                    char head = input_string[0];
                    if (Char.IsDigit(head))
                    {
                        return new Unit(true);
                    }
                    return new Unit(false);
                }
                return new Unit(UnitType.Null);
            }
            char_table.Set("is_digit", new IntrinsicUnit("char_is_digit", IsDigit, 1));

            return char_table;
        }
    }
}
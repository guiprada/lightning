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
using System.IO;

namespace lightning
{
    public class FileLightning
    {
        public static TableUnit GetTableUnit()
        {
            TableUnit file = new TableUnit(null);
            Unit LoadFile(VM p_vm)
            {
                string path = p_vm.GetString(0);
                string input;
                using (var sr = new StreamReader(path))
                {
                    input = sr.ReadToEnd();
                }
                if (input != null)
                    return new Unit(input);

                return new Unit(UnitType.Null);
            }
            file.Set("load", new IntrinsicUnit("file_load_file", LoadFile, 1));

            //////////////////////////////////////////////////////
            Unit WriteFile(VM p_vm)
            {
                string path = p_vm.GetString(0);
                string output = p_vm.GetString(1);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, false))
                {
                    file.Write(output);
                }

                return new Unit(UnitType.Null);
            }
            file.Set("write", new IntrinsicUnit("file_write_file", WriteFile, 2));

            //////////////////////////////////////////////////////
            Unit AppendFile(VM p_vm)
            {
                string path = p_vm.GetString(0);
                string output = p_vm.GetString(1);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
                {
                    file.Write(output);
                }

                return new Unit(UnitType.Null);
            }
            file.Set("append", new IntrinsicUnit("file_append_file", AppendFile, 2));

            return file;
        }
    }
}
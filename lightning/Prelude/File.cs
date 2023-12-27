using System;
using System.IO;

using lightningUnit;
using lightningVM;
namespace lightningPrelude
{
    public class LightningFile
    {
        public static TableUnit GetTableUnit()
        {
            TableUnit file = new TableUnit(null);
            Unit LoadFile(VM p_vm)
            {
                string path = p_vm.GetString(0);
                string contents;
                try
                {
                    using (var sr = new StreamReader(path))
                    {
                        contents = sr.ReadToEnd();
                    }
                }
                catch(Exception e)
                {
                    return new Unit(new ResultUnit(e));
                }
                return new Unit(new ResultUnit(new Unit(contents)));
            }
            file.Set("load", new IntrinsicUnit("file_load_file", LoadFile, 1));

            //////////////////////////////////////////////////////
            Unit WriteFile(VM p_vm)
            {
                string path = p_vm.GetString(0);
                string output = p_vm.GetString(1);
                try
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, false))
                    {
                        file.Write(output);
                    }
                }
                catch(Exception e)
                {
                    return new Unit(new ResultUnit(e));
                }
                return new Unit(new ResultUnit(new Unit(UnitType.Empty)));
            }
            file.Set("write", new IntrinsicUnit("file_write_file", WriteFile, 2));

            //////////////////////////////////////////////////////
            Unit AppendFile(VM p_vm)
            {
                string path = p_vm.GetString(0);
                string output = p_vm.GetString(1);
                try
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
                    {
                        file.Write(output);
                    }
                }
                catch(Exception e)
                {
                    return new Unit(new ResultUnit(e));
                }
                return new Unit(new ResultUnit(new Unit(UnitType.Empty)));
            }
            file.Set("append", new IntrinsicUnit("file_append_file", AppendFile, 2));

            return file;
        }
    }
}
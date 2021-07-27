namespace lightning
{
        public class Path{
        public static string ModuleName(string p_path){
            string name = System.IO.Path.ChangeExtension(p_path, null);
            string current_dir_string = "." + System.IO.Path.DirectorySeparatorChar;
            if (name.StartsWith(current_dir_string))
                name = name.Substring(2);
            name = name.Replace(System.IO.Path.DirectorySeparatorChar, '.');
            return name;
        }
        public static string ToPath(string p_moduleName){
            return p_moduleName.Replace('.', System.IO.Path.DirectorySeparatorChar);
        }
    }
}
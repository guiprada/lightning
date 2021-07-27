namespace lightning
{
        public class Utils{
        public static string ModuleName(string path){
            string name = System.IO.Path.ChangeExtension(path, null);
            if (name.StartsWith("./") || name.StartsWith(".\\"))
                name = name.Substring(2);
            name = name.Replace('\\', '.');
            name = name.Replace('/', '.');
            return name;
        }
    }
}
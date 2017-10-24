using System;
using System.IO;
using ThreadPool;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!CheckArgs(args)) return;
            switch (args.Length)
            {
                case 3: StaticPool(args); break;
                case 4: DynamicPool(args); break;
            }
        }

        static bool CheckArgs(string[] args)
        {
            int i = new int();
            if (args.Length != 3 && args.Length != 4) return PrintFinalError("Incorrect number of arguments.");
            if (!int.TryParse(args[2], out i) || !(args.Length == 4 && int.TryParse(args[3], out i)))
            { return PrintFinalError("Please, check nums correctness."); }
            if (!CheckDirectories(args[0], args[1]))
            { return PrintFinalError("Please, check directories correctness."); }
            return true;
        }

        static bool PrintFinalError(string error)
        {
            Console.WriteLine(error);
            return false;
        }

        static bool CheckNums(string[] nums)
        {
            int i = new int();
            foreach (string s in nums)
            { if (!int.TryParse(s, out i)) return false; }
            return true;
        }

        static bool CheckDirectories(string from, string to)
        {
            try { Directory.CreateDirectory(to); }
            catch { return false; }
            return (Directory.Exists(from) && Directory.Exists(to));
        }

        static void StaticPool(string[] args)
        {
            Pool<bool> staticPool = new Pool<bool>(Math.Abs(int.Parse(args[2])));
            MakePoolWork(staticPool, args[0], args[1]);
        }

        static void DynamicPool(string[] args)
        {
            Pool<bool> dynamicPool = new Pool<bool>(Math.Abs(int.Parse(args[2])), Math.Abs(int.Parse(args[3])));
            MakePoolWork(dynamicPool, args[0], args[1]);
        }

        static void MakePoolWork(Pool<bool> pool, string from, string to)
        {
            Console.WriteLine("Task started");
            CopyDirectory(pool, from, to);
            pool.Stop();
            Console.WriteLine("Completed");
        }

        static void CopyDirectory(Pool<bool> pool, string from, string to)
        {
            DirectoryInfo currentDir = new DirectoryInfo(from);
            foreach (DirectoryInfo dir in currentDir.GetDirectories())
            { CopyItemsInDir(pool, dir.FullName, Path.Combine(to, dir.Name)); }
            foreach (string file in Directory.GetFiles(from))
            {
                Task<bool> task = new Task<bool>(() => CopyFile(file, from, to));
                pool.Execute(task);
                if (!task.Get()) return;
            }
        }

        private static bool CopyFile(string file, string from, string to)
        {
            try { File.Copy(file, Path.Combine(to, Path.GetFileName(file))); return true; }
            catch { return false; }
        }

        static void CopyItemsInDir(Pool<bool> pool, string fullName, string path)
        {
            if (!Directory.Exists(path))
            { Directory.CreateDirectory(path); }
            CopyDirectory(pool, fullName, path);
        }
    }
}

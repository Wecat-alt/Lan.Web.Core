using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Onvif
{
    public class Library_o
    {
        public static string ResolveLinuxNativeFolder()
        {
            return ResolveLinuxNativeFolder("NovaPlayer");
        }

        public static string ResolveLinuxNativeFolder(string folder)
        {
            if (string.Equals(folder, "NovaPlayer", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(Environment.CurrentDirectory, "NovaPlayer", "lib");
            }

            return Path.Combine(Environment.CurrentDirectory, folder, "x64");
        }

        public static bool EnsureRestartWithLdLibraryPath(string nativeFolder)
        {
            return EnsureRestartWithLdLibraryPath(new[] { nativeFolder });
        }

        public static bool EnsureRestartWithLdLibraryPath(IEnumerable<string> nativeFolders)
        {
            if (Environment.GetEnvironmentVariable("NATIVE_BOOTSTRAPPED") == "1")
            {
                return false;
            }

            string[] existingFolders = nativeFolders
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(Path.GetFullPath)
                .Where(Directory.Exists)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (existingFolders.Length == 0)
            {
                return false;
            }

            string text = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? string.Empty;
            List<string> values = text.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (string folder in existingFolders)
            {
                if (!values.Any(p => string.Equals(p, folder, StringComparison.Ordinal)))
                {
                    values.Insert(0, folder);
                }
            }

            string value = string.Join(":", values);

            string processPath = Environment.ProcessPath;
            if (processPath == null)
            {
                throw new InvalidOperationException("无法获取当前进程路径");
            }

            string[] commandLineArgs = Environment.GetCommandLineArgs();
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false
            };

            if (processPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase) ||
                processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                processStartInfo.FileName = processPath;
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                string text2 = entryAssembly != null ? entryAssembly.Location : null;
                if (text2 == null)
                {
                    throw new InvalidOperationException("无法获取入口程序集路径");
                }

                processStartInfo.ArgumentList.Add(text2);
            }
            else
            {
                processStartInfo.FileName = processPath;
            }

            for (int i = 1; i < commandLineArgs.Length; i++)
            {
                processStartInfo.ArgumentList.Add(commandLineArgs[i]);
            }

            processStartInfo.Environment["LD_LIBRARY_PATH"] = value;
            processStartInfo.Environment["NATIVE_BOOTSTRAPPED"] = "1";

            Process process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException("无法启动子进程");
            }

            using (process)
            {
                process.WaitForExit();
                Environment.ExitCode = process.ExitCode;
                return true;
            }
        }

        //根据不同平台加载不同dll路径
        public static bool LoadPath(string folder)
        {
            try
            {
                //库文件路径
                string libPath = System.IO.Path.Combine(Environment.CurrentDirectory, folder);

                //根据软件运行模式设置库文件的路径
                if (Environment.Is64BitProcess)
                {
                    libPath = System.IO.Path.Combine(libPath, "X64");
                }
                else
                {
                    libPath = System.IO.Path.Combine(libPath, "X86");
                }
                //获取当前环境变量Path的值
                var path = Environment.GetEnvironmentVariable("Path");
                //添加库路径到环境变量中
                path += (";" + libPath + ";");
                //设置环境变量
                Environment.SetEnvironmentVariable("Path", path);

                Console.WriteLine("添加了环境变量" + libPath);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

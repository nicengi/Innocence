using Innocence.Security;
using InnocenceService;
using InnocenceService.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace Innocence
{
    internal class Program
    {
        #region Fields

        #region ARGS
        const string ARG_QUIT = "quit";
        const string ARG_PARAM = "param";
        //const string ARG_GLOBAL_REFRESH = "refresh";
        const string ARG_GLOBAL_CHECKALL = "checkall";

        const string ARG_INSTALL_SERVICE = "install";
        const string ARG_UNINSTALL_SERVICE = "uninstall";

        const string ARG_START_SERVICE = "start";
        const string ARG_STOP_SERVICE = "stop";

        const string ARG_CREATE_KEY = "createkey";
        const string ARG_CREATE_KEY_SIZE = "size";

        const string ARG_INSTALL_KEY = "installkey";
        const string ARG_INSTALL_KEY_INCLUDE_PRIVATE = "private";

        const string ARG_SET = "set";
        const string ARG_SET_DIRECTORY = "dir";
        const string ARG_SET_DIRECTORY_REMOVE = "remove";
        const string ARG_SET_RANGE = "range";

        const string ARG_DISPLAY = "display";
        const string ARG_DISPLAY_LAST_SHUTDOWN = "lastshutdown";
        const string ARG_DISPLAY_DIRS = "dirs";
        const string ARG_DISPLAY_RANGE = "range";
        const string ARG_DISPLAY_RECOVERY = "recovery";
        const string ARG_DISPLAY_RECOVERY_KEY = "key";

        const string ARG_EXECUTE_CUSTOM_COMMAND = "service";

        const string ARG_SAVE_CONFIGS = "saveconfigs";
        const string ARG_SAVE_CONFIGS_YES = "y";
        const string ARG_SAVE_CONFIGS_NO = "n";
        #endregion

        static readonly IDictionary SavedState = new Hashtable();
        static readonly InnocenceServiceConfigs Configs = InnocenceServiceConfigs.Create(InnocenceServiceConfigs.DefaultConfigsFile);
        static readonly string ServiceFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "InnocenceService.exe");
        static readonly string ServiceName = "Innocence Service";
        #endregion

        #region Constructor
        static Program()
        {

        }
        #endregion

        #region Properties

        #endregion

        #region Methods
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine($"{GetAssemblyTitle(Assembly.GetExecutingAssembly())} [Version {GetAssemblyVersion(Assembly.GetExecutingAssembly())}]");
                Console.WriteLine();

                if (args.Length == 0)
                {
                    do
                    {
                        Console.Write("Innocence>");
                        args = Console.ReadLine().Split(' ');

                        CommandProcess(args);

                    } while (args[0] != ARG_QUIT);
                }
                else
                {
                    CommandProcess(args);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void CommandProcess(string[] args)
        {
            try
            {
                if (args.Length == 0 || args[0] == string.Empty)
                {
                    return;
                }

                Dictionary<string, string> Args = new Dictionary<string, string>();

                #region Parse Args
                bool paramFlag = false;

                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];

                    if (arg == string.Empty)
                    {
                        continue;
                    }

                    int markIndex = arg.IndexOf(":");

                    if (markIndex == -1)
                    {
                        markIndex = arg.IndexOf("-", 1);
                        paramFlag = markIndex > 0;
                    }

                    int startIndex = 0;

                    if (arg[0] == '-' || arg[0] == '/')
                    {
                        startIndex = 1;
                    }

                    string key = arg.Substring(startIndex, markIndex == -1 ? arg.Length - startIndex : markIndex - startIndex).ToLower();
                    string value = arg.Substring(markIndex + 1, arg.Length - markIndex - 1);

                    if (markIndex == -1)
                    {
                        value = string.Empty;
                    }

                    Args.Add(key, value);

                    if (paramFlag)
                    {
                        if (args.Length > i + 1)
                        {
                            Args.Add(ARG_PARAM, args[++i]); //skip next.
                        }

                        paramFlag = false;
                    }
                }
                #endregion

                string firstArg = Args.Keys.First();

                switch (firstArg)
                {
                    case ARG_INSTALL_SERVICE:
                        InstallService(ServiceFile);
                        break;

                    case ARG_UNINSTALL_SERVICE:
                        UninstallService(ServiceFile);
                        break;

                    case ARG_START_SERVICE:
                        StartService();
                        break;

                    case ARG_STOP_SERVICE:
                        StopService();
                        break;

                    case ARG_CREATE_KEY:
                        int keySize = 2048;

                        if (Args.ContainsKey(ARG_CREATE_KEY_SIZE))
                        {
                            keySize = int.Parse(Args[ARG_CREATE_KEY_SIZE]);
                        }

                        CreateKey(Args[ARG_CREATE_KEY], keySize);
                        break;

                    case ARG_INSTALL_KEY:
                        InstallKey(Args[ARG_INSTALL_KEY], Args.ContainsKey(ARG_INSTALL_KEY_INCLUDE_PRIVATE));
                        break;

                    #region ARG_SET
                    case ARG_SET:
                        switch (Args[ARG_SET])
                        {
                            case ARG_SET_DIRECTORY:
                                string dir = Path.GetFullPath(Args[ARG_PARAM]);

                                if (Args.ContainsKey(ARG_SET_DIRECTORY_REMOVE))
                                {
                                    List<string> dirs = Configs.Directories.FindAll(d => d == dir);

                                    if (dirs.Count > 0)
                                    {
                                        Configs.Directories.RemoveAll(d => d == dir);
                                    }
                                    else
                                    {
                                        Console.WriteLine(string.Format("Directory is not found.", dir));
                                        break;
                                    }
                                }
                                else
                                {
                                    /*
                                    if (!Directory.Exists(dir))
                                    {
                                        Console.WriteLine(string.Format("Directory is not found.", dir));
                                        break;
                                    }
                                    */

                                    if (Configs.Directories.FindIndex(d => d == dir) == -1)
                                    {
                                        Configs.Directories.Add(dir);
                                    }
                                    else
                                    {
                                        Console.WriteLine(string.Format("Directory already exists.", dir));
                                        break;
                                    }
                                }
                                break;

                            case ARG_SET_RANGE:
                                Configs.RangeString = Args[ARG_PARAM];
                                break;
                        }
                        break;
                    #endregion

                    #region ARG_DISPLAY
                    case ARG_DISPLAY:
                        switch (Args[ARG_DISPLAY])
                        {
                            case ARG_DISPLAY_LAST_SHUTDOWN:
                                Console.WriteLine(Configs.LastShutdown.ToString());
                                break;

                            case ARG_DISPLAY_DIRS:
                                Configs.Directories.ForEach(d => Console.WriteLine(d));
                                break;

                            case ARG_DISPLAY_RANGE:
                                Console.WriteLine(Configs.RangeString);
                                break;

                            case ARG_DISPLAY_RECOVERY:
                                string file = Args[ARG_PARAM];

                                if (!File.Exists(file))
                                {
                                    file = Path.Combine(InnocenceServiceConfigs.RecoveryDirectory, $"{file}.recovery");

                                    if (!File.Exists(file))
                                    {
                                        Console.WriteLine(string.Format("File is not found.", file));
                                        break;
                                    }
                                }

                                RecoveryInfo recoveryInfo = Util.Deserialize<RecoveryInfo>(file);

                                Console.WriteLine();
                                Console.WriteLine($"GUID: {recoveryInfo.Guid:B}");
                                Console.WriteLine($"Directory Name: {recoveryInfo.DirectoryName}");
                                Console.WriteLine($"Directory Root: {recoveryInfo.DirectoryRoot}");
                                Console.WriteLine();

                                foreach (var item in recoveryInfo.Datas)
                                {
                                    string value = item.Value;

                                    if (Args.ContainsKey(ARG_DISPLAY_RECOVERY_KEY))
                                    {
                                        using (StreamReader streamReader = new StreamReader(Args[ARG_DISPLAY_RECOVERY_KEY]))
                                        {
                                            string xmlString = streamReader.ReadToEnd();

                                            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                                            {
                                                rsa.FromXmlString(xmlString);
                                                RSAParameters keyInfo = rsa.ExportParameters(true); ;
                                                byte[] dataToDecrypt = Convert.FromBase64String(value);
                                                value = Encoding.UTF8.GetString(Security.Security.RSADecrypt(dataToDecrypt, keyInfo, false));
                                            }
                                        }
                                    }

                                    Console.WriteLine(item.Key);
                                    Console.WriteLine(value);
                                    Console.WriteLine();
                                }
                                break;
                        }
                        break;
                    #endregion

                    case ARG_EXECUTE_CUSTOM_COMMAND:
                        if (Enum.TryParse(Args[ARG_EXECUTE_CUSTOM_COMMAND], true, out InnocenceServiceCustomCommands command))
                        {
                            ExecuteCommand(command);
                        }
                        break;

                    case ARG_SAVE_CONFIGS:
                        InnocenceServiceConfigs.Save(Configs);

                        if (Args.ContainsKey(ARG_SAVE_CONFIGS_YES) || Args.ContainsKey(ARG_SAVE_CONFIGS_NO))
                        {
                            if (Args.ContainsKey(ARG_SAVE_CONFIGS_YES))
                            {
                                ExecuteCommand(InnocenceServiceCustomCommands.Refresh);
                                ExecuteCommand(InnocenceServiceCustomCommands.CheckAll);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Do you want to notify the service and start checking? ");
                            Console.Write("Please enter [Y/N]:");

                            if (Console.ReadLine().ToLower() == ARG_SAVE_CONFIGS_YES)
                            {
                                ExecuteCommand(InnocenceServiceCustomCommands.Refresh);
                                ExecuteCommand(InnocenceServiceCustomCommands.CheckAll);
                            }
                        }
                        break;

                    case ARG_QUIT:
                        break;
                    default:
                        Console.WriteLine("Invalid command.");
                        break;
                }

                #region Global Args
                if (Args.ContainsKey(ARG_GLOBAL_CHECKALL))
                {
                    ExecuteCommand(InnocenceServiceCustomCommands.Refresh);
                    ExecuteCommand(InnocenceServiceCustomCommands.CheckAll);
                }
                #endregion
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine();
        }

        static void ExecuteCommand(InnocenceServiceCustomCommands command)
        {
            using (ServiceController service = new ServiceController(ServiceName))
            {
                if (service.Status != ServiceControllerStatus.Running)
                {
                    StartService();
                }

                service.ExecuteCommand((int)command);
            }
        }

        static void InstallKey(string path, bool includePrivate)
        {
            using (StreamReader streamReader = new StreamReader(path))
            {
                string xmlString = streamReader.ReadToEnd();

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(xmlString);
                    xmlString = rsa.ToXmlString(includePrivate);
                }

                Configs.RSAPublicKeyXmlString = xmlString;
            }
        }

        static void CreateKey(string path, int keySize)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(keySize))
            {
                using (StreamWriter streamWriter = new StreamWriter(path))
                {
                    streamWriter.Write(rsa.ToXmlString(true));
                }
            }
        }

        static void InstallService(string servicePath)
        {
            AssemblyInstaller installer = new AssemblyInstaller
            {
                Path = servicePath,
                //CommandLine = new string[] { $"/logFile=InnocenceService_install.log" },
                //UseNewContext = true
            };
            installer.Install(SavedState);
            installer.Commit(SavedState);
        }

        static void UninstallService(string servicePath)
        {
            AssemblyInstaller installer = new AssemblyInstaller
            {
                Path = servicePath,
                //CommandLine = new string[] { $"/logFile=InnocenceService_uninstall.log" },
                //UseNewContext = true
            };
            installer.Uninstall(SavedState);
        }

        static void StartService()
        {
            ServiceController service = new ServiceController(ServiceName);

            if (service.Status == ServiceControllerStatus.Stopped)
            {
                service.Start();
                while (service.Status != ServiceControllerStatus.Running)
                {
                    Thread.Sleep(1000);
                    service.Refresh();
                    Console.WriteLine(string.Format("Service Status: {0}", service.Status));
                }
            }
        }

        static void StopService()
        {
            ServiceController service = new ServiceController(ServiceName);

            if (service.Status == ServiceControllerStatus.Running)
            {
                service.Stop();
                while (service.Status != ServiceControllerStatus.Stopped)
                {
                    Thread.Sleep(1000);
                    service.Refresh();
                    Console.WriteLine(string.Format("Service Status: {0}", service.Status));
                }
            }
        }

        #region Assembly Info
        public static string GetAssemblyTitle(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                if (titleAttribute.Title != "")
                {
                    return titleAttribute.Title;
                }
            }
            return Path.GetFileNameWithoutExtension(assembly.CodeBase);
        }

        public static string GetAssemblyVersion(Assembly assembly)
        {
            return assembly.GetName().Version.ToString();
        }

        public static string GetAssemblyDescription(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            if (attributes.Length == 0)
            {
                return "";
            }
            return ((AssemblyDescriptionAttribute)attributes[0]).Description;
        }

        public static string GetAssemblyProduct(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            if (attributes.Length == 0)
            {
                return "";
            }
            return ((AssemblyProductAttribute)attributes[0]).Product;
        }

        public static string GetAssemblyCopyright(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            if (attributes.Length == 0)
            {
                return "";
            }
            return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }

        public static string GetAssemblyCompany(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            if (attributes.Length == 0)
            {
                return "";
            }
            return ((AssemblyCompanyAttribute)attributes[0]).Company;
        }

        #endregion

        #endregion
    }
}

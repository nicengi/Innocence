using Innocence.Security;
using InnocenceService.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;

namespace InnocenceService
{
    public partial class InnocenceService : ServiceBase
    {
        #region Fields
        #endregion

        #region Constructor
        public InnocenceService()
        {
            InitializeComponent();
        }
        #endregion

        #region Properties
        private InnocenceServiceConfigs Configs { get; set; }
        #endregion

        #region Methods
        protected override void OnStart(string[] args)
        {
            try
            {
                Configs = InnocenceServiceConfigs.Create(InnocenceServiceConfigs.DefaultConfigsFile);
                string rangeString = Configs.RangeString;
                DateTime lastShutdown = Configs.LastShutdown;

                if (IsEncryptStart(rangeString, lastShutdown))
                {
                    EncryptStart(Configs.Directories.ToArray(), Configs.RSAPublicKeyXmlString);
                }
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(e.ToString(), EventLogEntryType.Error, (int)EventID.OnStart);
            }
        }

        protected override void OnStop()
        {
            try
            {
                InnocenceServiceConfigs.Save(Configs);

                string message = string.Format("配置文件已成功保存。位置：{0}", InnocenceServiceConfigs.DefaultConfigsFile);
                EventLog.WriteEntry(message, EventLogEntryType.Information, (int)EventID.OnStop);
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(e.ToString(), EventLogEntryType.Error, (int)EventID.OnStop);
            }
        }

        protected override void OnShutdown()
        {
            try
            {
                Configs.LastShutdown = DateTime.Now;
                InnocenceServiceConfigs.Save(Configs);

                string message = string.Format("配置文件已成功保存。位置：{0}", InnocenceServiceConfigs.DefaultConfigsFile);
                EventLog.WriteEntry(message, EventLogEntryType.Information, (int)EventID.OnShutdown);
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(e.ToString(), EventLogEntryType.Error, (int)EventID.OnShutdown);
            }
        }

        protected override void OnCustomCommand(int command)
        {
            try
            {
                string messageStart = string.Format("开始处理服务命令。命令：{0}。", Enum.GetName(typeof(InnocenceServiceCustomCommands), command));
                EventLog.WriteEntry(messageStart, EventLogEntryType.Information, (int)EventID.OnCustomCommand);

                switch ((InnocenceServiceCustomCommands)command)
                {
                    case InnocenceServiceCustomCommands.Encrypt:
                    case InnocenceServiceCustomCommands.CheckAll:
                        string rangeString = Configs.RangeString;
                        DateTime lastShutdown = Configs.LastShutdown;

                        if (IsEncryptStart(rangeString, lastShutdown) || (InnocenceServiceCustomCommands)command == InnocenceServiceCustomCommands.Encrypt)
                        {
                            EncryptStart(Configs.Directories.ToArray(), Configs.RSAPublicKeyXmlString);
                        }
                        break;
                    case InnocenceServiceCustomCommands.Refresh:
                        Configs = InnocenceServiceConfigs.Create(InnocenceServiceConfigs.DefaultConfigsFile);
                        break;
                }

                string message = string.Format("已成功处理服务命令。命令：{0}。", Enum.GetName(typeof(InnocenceServiceCustomCommands), command));
                EventLog.WriteEntry(message, EventLogEntryType.Information, (int)EventID.OnCustomCommand);
            }
            catch (Exception e)
            {
                string message = string.Format("服务命令引发了异常。命令：{0}。\r\n{1}", Enum.GetName(typeof(InnocenceServiceCustomCommands), command), e);
                EventLog.WriteEntry(message, EventLogEntryType.Information, (int)EventID.OnCustomCommand);
            }
        }

        /// <summary>
        /// 返回一个布尔值，指示是否应该开始加密文件。
        /// </summary>
        /// <param name="rangeString"><see cref="double"/> 或 <see cref="DateTime"/> 类型的字符串，指示开始加密文件所经过的时间。</param>
        /// <param name="lastShutdown">范围为 <see cref="double"/> 时指定系统最后一次关闭的时间。</param>
        /// <returns></returns>
        private bool IsEncryptStart(string rangeString, DateTime lastShutdown = default)
        {
            if (double.TryParse(rangeString, out double rangeSecondes))
            {
                return DateTime.Now.CompareTo(Configs.LastShutdown.AddSeconds(rangeSecondes)) >= 0;
            }
            else if (DateTime.TryParse(rangeString, out DateTime rangeDate))
            {
                return DateTime.Now.CompareTo(rangeDate) >= 0;
            }

            return false;
        }

        private void EncryptStart(string[] dirs, string keyXmlString)
        {
            try
            {
                EventLog.WriteEntry(string.Format("已开始处理。目录数量：{0}\r\n密钥信息：\r\n{1}", dirs.Length, keyXmlString), EventLogEntryType.Information, (int)EventID.EncryptStart);

                RecoveryInfo recoveryInfo = null;

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(keyXmlString);

                    foreach (string dir in dirs)
                    {
                        EventLog.WriteEntry(string.Format("开始处理目录。位置：{0}", dir), EventLogEntryType.Information, (int)EventID.EncryptStart);

                        recoveryInfo = new RecoveryInfo(Guid.NewGuid())
                        {
                            DirectoryName = Path.GetFileName(dir),
                            DirectoryRoot = dir,
                        };

                        try
                        {
                            try
                            {
                                string newDirRoot = Path.Combine(Path.GetDirectoryName(dir), recoveryInfo.Guid.ToString("N"));
                                Directory.Move(dir, newDirRoot);
                                recoveryInfo.DirectoryRoot = newDirRoot;
                            }
                            catch (DirectoryNotFoundException e)
                            {
                                EventLog.WriteEntry(e.ToString(), EventLogEntryType.Error, (int)EventID.EncryptStart);
                                continue;
                            }
                            catch (Exception e)
                            {
                                EventLog.WriteEntry(e.ToString(), EventLogEntryType.Warning, (int)EventID.EncryptStart);
                            }

                            Encrypt(recoveryInfo.DirectoryRoot, recoveryInfo, rsa.ExportParameters(false));
                            EventLog.WriteEntry(string.Format("已成功处理目录。位置：{0}", dir), EventLogEntryType.Information, (int)EventID.EncryptStart);
                            _SaveRecoveryInfo();
                        }
                        catch (Exception e)
                        {
                            string message = string.Format("处理目录时引发了异常。位置：{1}\r\n根目录：{2}\r\n{0}", e, dir, recoveryInfo.DirectoryRoot);
                            EventLog.WriteEntry(message, EventLogEntryType.Error, (int)EventID.EncryptStart);
                            EventLog.WriteEntry(string.Format("未能完全处理目录。位置：{0}", dir), EventLogEntryType.Information, (int)EventID.EncryptStart);
                            _SaveRecoveryInfo();
                            continue;
                        }

                        void _SaveRecoveryInfo()
                        {
                            string recoveryFile = Path.Combine(InnocenceServiceConfigs.RecoveryDirectory, $"{recoveryInfo.Guid:N}.recovery");
                            try
                            {
                                Util.Serialize(recoveryFile, recoveryInfo);
                                EventLog.WriteEntry(string.Format("已成功保存恢复文件。位置：{0}\r\n\r\nGUID:{1}\r\nDirectory Name:{2}\r\nDirectory Root:{3}", recoveryFile, recoveryInfo.Guid, recoveryInfo.DirectoryName, recoveryInfo.DirectoryRoot), EventLogEntryType.Information, (int)EventID.EncryptStart);
                            }
                            catch (Exception e)
                            {
                                string message = string.Format("保存恢复文件时引发了异常。位置：{1}\r\n根目录：{2}\r\n{0}", e, recoveryFile, recoveryInfo.DirectoryRoot);
                                EventLog.WriteEntry(message, EventLogEntryType.Error, (int)EventID.EncryptStart);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(e.ToString(), EventLogEntryType.Error, (int)EventID.EncryptStart);
            }
            finally
            {
                EventLog.WriteEntry(string.Format("已全部处理完成。"), EventLogEntryType.Information, (int)EventID.EncryptStart);
            }
        }

        private void Encrypt(string dirRoot, RecoveryInfo recoveryInfo, RSAParameters keyInfo)
        {
            dirRoot = Path.GetFullPath(dirRoot);
            recoveryInfo.DirectoryRoot = Path.GetFullPath(recoveryInfo.DirectoryRoot);

            string[] files = Directory.GetFiles(dirRoot);
            foreach (string file in files)
            {
                try
                {
                    Guid guid = Guid.NewGuid();
                    string newFile = Path.Combine(dirRoot, guid.ToString("N"));

                    recoveryInfo.Datas.Add(_SubtractDirRoot(newFile, recoveryInfo.DirectoryRoot), _EncryptString(_SubtractDirRoot(file, recoveryInfo.DirectoryRoot)));
                    File.Move(file, newFile);
                }
                catch (Exception e)
                {
                    string message = string.Format("处理文件时引发了异常。位置：{1}\r\n根目录：{2}\r\n{0}", e, file, recoveryInfo.DirectoryRoot);
                    EventLog.WriteEntry(message, EventLogEntryType.Warning, (int)EventID.Encrypt);
                    continue;
                }
            }

            string[] dirs = Directory.GetDirectories(dirRoot);
            foreach (string dir in dirs)
            {
                try
                {
                    string _dir = dir;

                    try
                    {
                        string newDirRoot = Path.Combine(Path.GetDirectoryName(dir), Guid.NewGuid().ToString("N"));
                        recoveryInfo.Datas.Add(_SubtractDirRoot(newDirRoot, recoveryInfo.DirectoryRoot), _EncryptString(_SubtractDirRoot(dir, recoveryInfo.DirectoryRoot)));
                        Directory.Move(dir, newDirRoot);
                        _dir = newDirRoot;
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        EventLog.WriteEntry(e.ToString(), EventLogEntryType.Error, (int)EventID.Encrypt);
                        continue;
                    }
                    catch (Exception e)
                    {
                        EventLog.WriteEntry(e.ToString(), EventLogEntryType.Warning, (int)EventID.Encrypt);
                    }

                    Encrypt(_dir, recoveryInfo, keyInfo);
                }
                catch (Exception e)
                {
                    string message = string.Format("处理目录时引发了异常。位置：{1}\r\n根目录：{2}\r\n{0}", e, dir, recoveryInfo.DirectoryRoot);
                    EventLog.WriteEntry(message, EventLogEntryType.Warning, (int)EventID.Encrypt);
                }
            }

            string _SubtractDirRoot(string _path, string _dirRoot)
            {
                return _path.Substring(_dirRoot.Length);
            }

            string _EncryptString(string _data)
            {
                byte[] dataToEncrypt = Encoding.UTF8.GetBytes(_data);
                return Convert.ToBase64String(Security.RSAEncrypt(dataToEncrypt, keyInfo, false));
            }
        }
        #endregion
    }
}

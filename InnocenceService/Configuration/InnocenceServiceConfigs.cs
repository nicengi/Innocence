using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace InnocenceService.Configuration
{
    [Serializable]
    [XmlRoot("innocenceServiceConfigs")]
    public class InnocenceServiceConfigs
    {
        #region Fields
        /// <summary>
        /// 获取一个值，指示默认的配置文件的路径。
        /// </summary>
        public readonly static string DefaultConfigsFile;

        /// <summary>
        /// 获取一个值，指示默认的数据目录。
        /// </summary>
        public readonly static string DataDirectory;

        /// <summary>
        /// 获取一个值，指示默认的恢复文件目录。
        /// </summary>
        public readonly static string RecoveryDirectory;
        #endregion

        #region Constructor
        public InnocenceServiceConfigs()
        {

        }

        static InnocenceServiceConfigs()
        {

            DataDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "data");

            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            RecoveryDirectory = Path.Combine(DataDirectory, "recovery");

            if (!Directory.Exists(RecoveryDirectory))
            {
                Directory.CreateDirectory(RecoveryDirectory);
            }

            DefaultConfigsFile = Path.Combine(DataDirectory, "Default.configs");

            if (!File.Exists(DefaultConfigsFile))
            {
                Serialize(DefaultConfigsFile, new InnocenceServiceConfigs());
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// 获取或设置一个 <see cref="DateTime"/>，指示系统最后一次关闭的时间。
        /// </summary>
        [XmlElement("lastShutdown")]
        public DateTime LastShutdown { get; set; }

        /// <summary>
        /// 获取或设置一个 <see cref="double"/> 或 <see cref="DateTime"/> 类型的字符串，指示开始加密文件所经过的时间。
        /// </summary>
        [XmlElement("range")]
        public string RangeString { get; set; }

        /// <summary>
        /// 获取 <see cref="RSAPublicKeyXmlString"/> 使用 Base64 编码后的值。通过 <see cref="RSAPublicKeyXmlString"/> 设置此属性的值。
        /// </summary>
        [XmlElement("RSAPublicKey")]
        public string RSAPublicKeyBase64 { get; set; }

        /// <summary>
        /// 获取或设置 Xml 字符串格式的公钥。
        /// </summary>
        [XmlIgnore]
        public string RSAPublicKeyXmlString
        {
            get
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(RSAPublicKeyBase64));
            }

            set
            {
                RSAPublicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
            }
        }

        [XmlArray("directories")]
        [XmlArrayItem("directory")]
        public List<string> Directories { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// 保存 <see cref="InnocenceServiceConfigs"/> 到默认的配置文件。
        /// </summary>
        public static void Save(InnocenceServiceConfigs configs)
        {
            Serialize(DefaultConfigsFile, configs);
        }

        /// <summary>
        /// 保存 <see cref="InnocenceServiceConfigs"/> 到配置文件。
        /// </summary>
        public static void Save(InnocenceServiceConfigs configs, string path)
        {
            Serialize(path, configs);
        }

        /// <summary>
        /// 创建 <see cref="InnocenceServiceConfigs"/>，具有默认参数。
        /// </summary>
        /// <returns></returns>
        public static InnocenceServiceConfigs Create()
        {
            InnocenceServiceConfigs configs = new InnocenceServiceConfigs()
            {

                LastShutdown = DateTime.Now.Subtract(TimeSpan.FromMilliseconds(Environment.TickCount)),
                RangeString = "2032-02-10T20:00:00",
                RSAPublicKeyBase64 = string.Empty,
                Directories = new List<string>(),
            };

            return configs;
        }

        /// <summary>
        /// 创建 <see cref="InnocenceServiceConfigs"/>，从配置文件。
        /// </summary>
        /// <param name="path">文件名。</param>
        /// <returns></returns>
        public static InnocenceServiceConfigs Create(string path)
        {
            return Deserialize(path);
        }

        private static InnocenceServiceConfigs Deserialize(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(InnocenceServiceConfigs));
            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                InnocenceServiceConfigs item = (InnocenceServiceConfigs)serializer.Deserialize(stream);
                return item;
            }
        }

        private static void Serialize(string path, InnocenceServiceConfigs item)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(InnocenceServiceConfigs));
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            using (StreamWriter stream = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                serializer.Serialize(stream, item, namespaces);
            }
        }
        #endregion
    }
}

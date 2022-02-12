using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace InnocenceService
{
    public class Util
    {
        #region Methods
        public static void Serialize(string path, object obj)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                formatter.Serialize(stream, obj);
            }
        }

        public static T Deserialize<T>(string path)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                return (T)formatter.Deserialize(stream);
            }
        }

        public static void XmlSerialize<T>(string path, T obj)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            using (StreamWriter stream = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                serializer.Serialize(stream, obj, namespaces);
            }
        }

        public static T XmlDeserialize<T>(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return (T)serializer.Deserialize(stream);
            }
        }
        #endregion
    }
}

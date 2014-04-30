using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ModernMusic.Library.Helpers
{
    public static class JsonSerialization
    {
        public static string Serialize<T>(T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var _Serializer = new DataContractJsonSerializer(typeof(T));
                _Serializer.WriteObject(ms, obj);
                ms.Position = 0;
                using(StreamReader reader = new StreamReader(ms))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static T Deserialize<T>(string obj)
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(obj ?? "")))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(stream);
            }
        }
    }
}

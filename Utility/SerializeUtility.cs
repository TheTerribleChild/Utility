using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Utility
{
    public static class SerializeUtility
    {
        private static JsonSerializerSettings jsonSetting;

        static SerializeUtility()
        {
            jsonSetting = new JsonSerializerSettings();
            jsonSetting.Converters.Add(new IPEndPointConverter());
            jsonSetting.Converters.Add(new IPAddressConverter());
        }

        public static bool SerializeToFileBinary(object serialObject, string fileLocation)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream("MyFile.bin",FileMode.Create,FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, serialObject);
                stream.Close();
                return true;
            }catch(Exception e)
            {
                Console.Error.WriteLine(e);
                return false;
            }
        }

        public static object DeserializeFileBinary(string fileLocation)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream("MyFile.bin", FileMode.Open, FileAccess.Read, FileShare.Read);
                object deserializedObject = formatter.Deserialize(stream);
                stream.Close();
                return deserializedObject;
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(e);
                return null;
            }
        }

        public static byte[] SerializeToByteBinary(object serialObject)
        {
            try
            {
                byte[] serializedObject;
                IFormatter formatter = new BinaryFormatter();
                MemoryStream stream = new MemoryStream();
                formatter.Serialize(stream, serialObject);
                serializedObject = stream.ToArray();
                stream.Close();
                return serializedObject;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return null;
            }
        }

        public static object DeserializeByteBinary(byte[] serializedByte)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                MemoryStream stream = new MemoryStream(serializedByte);
                object deserializedObject = formatter.Deserialize(stream);
                stream.Close();
                return deserializedObject;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return null;
            }
        }


        public static bool SerializeToFileSoap(object serialObject, string fileLocation)
        {
            try
            {
                IFormatter formatter = new SoapFormatter();
                FileStream stream = new FileStream(fileLocation, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, serialObject);
                stream.Close();
                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return false;
            }
        }

        public static object DeserializeFileSoap(string fileLocation)
        {
            try
            {
                IFormatter formatter = new SoapFormatter();
                FileStream stream = new FileStream(fileLocation, FileMode.Open, FileAccess.Read, FileShare.Read);
                object deserializedObject = formatter.Deserialize(stream);
                stream.Close();
                return deserializedObject;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return null;
            }
        }

        public static string SerializeToString(object serialObject)
        {
            try
            {
                string serializedObject;
                IFormatter formatter = new SoapFormatter();
                MemoryStream stream = new MemoryStream();
                formatter.Serialize(stream, serialObject);
                stream.Position = 0;
                serializedObject = new StreamReader(stream).ReadToEnd();
                stream.Close();
                return serializedObject;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return null;
            }
        }

        public static object DeserializeString(string serializedString)
        {
            try
            {
                IFormatter formatter = new SoapFormatter();
                Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(serializedString));
                object deserializedObject = formatter.Deserialize(stream);
                stream.Close();
                return deserializedObject;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return null;
            }
        }

        public static string SerializeToJsonString(object serialObject)
        {
            try
            {
                return JsonConvert.SerializeObject(serialObject, jsonSetting);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return null;
            }
        }

        public static T DeserializeJsonString<T>(string jsonString)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(jsonString, jsonSetting);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return default(T);
            }
        }
    }

    internal class IPEndPointConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(IPEndPoint));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IPEndPoint ep = (IPEndPoint)value;
            writer.WriteStartObject();
            writer.WritePropertyName("Address");
            serializer.Serialize(writer, ep.Address);
            writer.WritePropertyName("Port");
            writer.WriteValue(ep.Port);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            IPAddress address = jo["Address"].ToObject<IPAddress>(serializer);
            int port = jo["Port"].Value<int>();
            return new IPEndPoint(address, port);
        }
    }

    internal class IPAddressConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(IPAddress));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IPAddress ip = (IPAddress)value;
            writer.WriteValue(ip.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            return IPAddress.Parse(token.Value<string>());
        }
    }
}

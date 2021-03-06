﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
using System.Threading.Tasks;


namespace Utility
{
    public static class SerializeUtility
    {

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
                FileStream stream = new FileStream("MyFile.bin", FileMode.Create, FileAccess.Write, FileShare.None);
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
                FileStream stream = new FileStream("MyFile.bin", FileMode.Open, FileAccess.Read, FileShare.Read);
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
                serializedObject = new StreamReader(stream, Encoding.UTF8).ReadToEnd();
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
    }
}

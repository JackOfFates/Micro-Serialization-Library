using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;




namespace MicroSerializationLibrary.Serialization
{

	/// <summary>
	/// This protocol allow methods that deserialize and serialize object to use the same language. 
	/// </summary>
	/// <remarks></remarks>
	public interface ISerializationProtocol
	{

		byte[] Serialize(object Obj);
		object Deserialize(byte[] Data);
		object GetPropertyValue(string PropertyName, int PropertyIndex, ref object ObjectData, PropertyReference pr);

	}

	/// <summary>
	/// The Default MS BinaryFormatter Serializer Engine. It's Slow and Picky.
	/// </summary>
	public class BinaryFormatterSerializer : ISerializationProtocol
	{

		public object Deserialize(byte[] Data)
		{
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bfTemp = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			using (System.IO.MemoryStream sStream = new System.IO.MemoryStream(Data)) {
				object Obj = bfTemp.Deserialize(sStream);
				return Obj;
			}
		}

        public object GetPropertyValue(string PropertyName, int PropertyIndex, ref object ObjectData, PropertyReference pr) {
            System.Collections.Generic.Dictionary<string, object> d = new System.Collections.Generic.Dictionary<string, object>();
            object outVal;
            d.TryGetValue(PropertyName, out outVal);
            return outVal;
        }

        public byte[] Serialize(object Obj)
		{
			System.IO.MemoryStream sStream = new System.IO.MemoryStream();
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bfTemp = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			bfTemp.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
			bfTemp.Serialize(sStream, Obj);
			return sStream.ToArray();
		}
	}

}

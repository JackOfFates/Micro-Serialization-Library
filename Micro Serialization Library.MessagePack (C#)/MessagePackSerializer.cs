using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using MicroSerializationLibrary.Networking;
using MsgPack.Serialization;
namespace MicroSerializationLibrary.Serialization
{

	/// <summary>
	/// The Message Pack Serializer is faster and slimmer than JSON, but not without limitations. 
	/// https://github.com/msgpack/msgpack/blob/master/spec.md#limitation
	/// </summary>
	public class MessagePackSerializer : ISerializationProtocol
	{


		IMessagePackSingleObjectSerializer serializer = SerializationContext.Default.GetSerializer<DeserializationWrapper>();
		private byte[] ISerializationProtocol_Serialize(object Obj)
		{
			DeserializationWrapper WrapperObject = new DeserializationWrapper(Obj, this);
			return serializer.PackSingleObject(WrapperObject);
		}
		byte[] ISerializationProtocol.Serialize(object Obj)
		{
			return ISerializationProtocol_Serialize(Obj);
		}

        public object Deserialize(byte[] Data)
		{
            string msgpackString = DeserializationWrapper.ConvertByteToString(Data); //Encoding.UTF8.GetString(Data);

            //DeserializationWrapper ObjWrapper = (DeserializationWrapper)serializer.UnpackSingleObject(Data);
            if(!msgpackString.Contains("^")) { return null; }
            string trim = msgpackString.Remove(0, msgpackString.IndexOf("^")).Replace("ÙZSystem.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089","");
            return trim;// ObjWrapper.GetInitializedObject(this);

        }

		public object GetPropertyValue(string PropertyName, int PropertyIndex, ref object ObjectData, PropertyReference pr)
		{
			IList<MsgPack.MessagePackObject> MsgPackObj = ((MsgPack.MessagePackObject)ObjectData).AsList();
			return MsgPackObj[MsgPackObj.Count - PropertyIndex].ToObject();
		}

	}
}

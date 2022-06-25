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
			string jsonstr = System.Text.UnicodeEncoding.UTF8.GetString(Data);
			DeserializationWrapper ObjWrapper = (DeserializationWrapper)serializer.UnpackSingleObject(Data);
			return ObjWrapper.GetInitializedObject(this);
		}

		public object GetPropertyValue(string PropertyName, int PropertyIndex, object ObjectData, PropertyReference pr)
		{
			IList<MsgPack.MessagePackObject> MsgPackObj = ((MsgPack.MessagePackObject)ObjectData).AsList();
			return MsgPackObj[MsgPackObj.Count - PropertyIndex].ToObject();
		}

	}
}

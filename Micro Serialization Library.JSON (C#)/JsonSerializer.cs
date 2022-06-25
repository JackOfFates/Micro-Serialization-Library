using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using MicroSerializationLibrary.Networking;
using MicroSerializationLibrary.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace MicroSerializationLibrary.Serialization {
    /// <summary>
    /// The JSON serialization engine is slower than MessagePack, but supports a wider range of objects.
    /// </summary>
    public class JsonSerializer : ISerializationProtocol {

        public byte[] Serialize(object obj) {
            string JsonString = JsonConvert.SerializeObject(new DeserializationWrapper(obj, this));
            return ASCIIEncoding.ASCII.GetBytes(JsonString);
        }

        public object Deserialize(byte[] Data) {
            string JsonString = DeserializationWrapper.ConvertByteToString(Data);
            DeserializationWrapper ObjWrapper = JsonConvert.DeserializeObject<DeserializationWrapper>(JsonString);
            return ObjWrapper.GetInitializedObject(this);
        }

        public object GetPropertyValue(string PropertyName, int PropertyIndex, ref object ObjectData, PropertyReference pr) {
            object v = null;
            Type tt = null;
            object[] indexArray = { 0 };
            Object objD = ObjectData;

            foreach (PropertyReference p in DeserializationWrapper.GetPropertyReferences(pr.Instance)) {
                if (p.Info.Name == pr.Info.Name) {
                    tt = p.Info.GetValue(p.Instance, indexArray).GetType();
                    v = new JObject(objD).ToObject(tt);
                }
            }
            return v;
        }
    }
}
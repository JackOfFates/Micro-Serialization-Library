using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace MicroSerializationLibrary.Serialization
{
    [Serializable()]
    public class DeserializationWrapper
    {
        public static BindingFlags ReflectionFlags { get; set; }
        public DeserializationWrapper()
        {
        }

        public DeserializationWrapper(object Obj, ISerializationProtocol Protocol)
        {
            this.Type = Obj.GetType().AssemblyQualifiedName;
            this.Data = Obj;
            dftMethods.AddRange(DefaultMethods);
        }

        public string Type { get; set; }
        public object Data { get; set; }

       public static string ConvertByteToString(byte[] data) {
            return new string(data.Select(b => (char)b).ToArray());
        }

        public static byte[] ConvertStringToByte(string s) {
            return s.ToCharArray().Select(c => (byte)c).ToArray();
        }

        public object GetInitializedObject(ISerializationProtocol sender)
        {
            try
            {
                Type theType = System.Type.GetType(Type);
                if (theType == typeof(System.String)) {
                    return Data.ToString();
                } else {

                    object instance = Activator.CreateInstance(theType);
                    GetPropertyReferences(instance).ForEach(r => SetProperty(instance, r, sender));
                    return instance;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        public void SetProperty(object Instance, PropertyReference Reference, ISerializationProtocol Protocol)
        {
            var _with1 = Reference;
            if (_with1.Info.CanWrite)
            {
                var tempData = Data;
                var v = Protocol.GetPropertyValue(_with1.Info.Name, _with1.Index, ref tempData, Reference);
                _with1.Info.SetValue(Instance, v, null);
            }
        }

        #region "GetEnumReferences"

        #endregion

        #region "GetPropertyReferences"

        public static List<PropertyReference> GetPropertyReferences(object target)
        {
            return GetPropertyReferences(target.GetType(), ReflectionFlags);
        }

        public static List<PropertyReference> GetPropertyReferences(object target, BindingFlags ReflectionFlags)
        {
            return GetPropertyReferences(target.GetType(), ReflectionFlags);
        }

        public static List<PropertyReference> GetPropertyReferences(Type Type)
        {
            return GetPropertyReferences(Type, ReflectionFlags);
        }

        public static List<PropertyReference> GetPropertyReferences(Type Type, BindingFlags ReflectionFlags)
        {
            PropertyInfo[] objProperties = Type.GetProperties(ReflectionFlags);
            object instance = Activator.CreateInstance(Type);
            int index = 0;
            List<PropertyReference> References = new List<PropertyReference>();
            foreach (PropertyInfo p in objProperties)
            {
                References.Add(new PropertyReference(instance, p, index));
                index += 1;
            }
            return References;
        }

        public static List<FieldReference> GetFieldReferences(object target)
        {
            return GetFieldReferences(target.GetType(), ReflectionFlags);
        }

        public static List<FieldReference> GetFieldReferences(object target, BindingFlags ReflectionFlags)
        {
            return GetFieldReferences(target.GetType(), ReflectionFlags);
        }

        public static List<FieldReference> GetFieldReferences(Type Type)
        {
            return GetFieldReferences(Type, ReflectionFlags);
        }

        public static List<FieldReference> GetFieldReferences(Type Type, BindingFlags ReflectionFlags)
        {
            FieldInfo[] objProperties = Type.GetFields(ReflectionFlags);
            object instance = Activator.CreateInstance(Type);
            int index = 0;
            List<FieldReference> References = new List<FieldReference>();
            foreach (FieldInfo p in objProperties)
            {
                References.Add(new FieldReference(instance, p, index));
                index += 1;
            }
            return References;
        }

        public static List<EnumReference> GetEnumReferences(Type EnumType, BindingFlags ReflectionFlags)
        {
            List<string> valuesAslist = new List<string>();
            valuesAslist.AddRange((string[])Enum.GetValues(typeof(Type)));
            List<EnumReference> pRefs = new List<EnumReference>();
            int i = 0;
            valuesAslist.ForEach(s =>
            {
                pRefs.Add(new EnumReference(EnumType, s, i));
                i += 1;
            });
            return pRefs;
        }

        public static List<EnumReference> GetEnumReferences(Type EnumType)
        {
            return GetEnumReferences(EnumType, ReflectionFlags);
        }

        #endregion

        #region "GetMethodReferences"
        private static List<string> dftMethods = new List<string>();
        private static string[] DefaultMethods = {
            "GETTYPE",
            "TOSTRING",
            "GETHASHCODE",
            "EQUALS"
        };
        public static List<MethodReference> GetMethodReferences(object target)
        {
            return GetMethodReferences(target.GetType(), ReflectionFlags);
        }

        public static List<MethodReference> GetMethodReferences(object target, bool includeFunctions)
        {
            return GetMethodReferences(target.GetType(), ReflectionFlags, includeFunctions);
        }

        public static List<MethodReference> GetMethodReferences(object target, BindingFlags ReflectionFlags)
        {
            return GetMethodReferences(target.GetType(), ReflectionFlags);
        }

        public static List<MethodReference> GetMethodReferences(object target, BindingFlags ReflectionFlags, bool includeFunctions)
        {
            return GetMethodReferences(target.GetType(), ReflectionFlags, includeFunctions);
        }

        public static List<MethodReference> GetMethodReferences(Type Type)
        {
            return GetMethodReferences(Type, ReflectionFlags);
        }

        public static List<MethodReference> GetMethodReferences(Type Type, bool includeFunctions)
        {
            return GetMethodReferences(Type, ReflectionFlags, includeFunctions);
        }

        public static List<MethodReference> GetMethodReferences(Type Type, BindingFlags ReflectionFlags, bool includeFunctions, bool CatchDefaultMethods = false)
        {
            MethodInfo[] objMethods = Type.GetMethods(ReflectionFlags | (BindingFlags.SetProperty) | (BindingFlags.GetProperty));
            object instance = Activator.CreateInstance(Type);
            int index = 0;
            List<MethodReference> References = new List<MethodReference>();
            if (!includeFunctions)
            {
                foreach (MethodInfo m in objMethods)
                {
#if NET20
					if (m.ReturnType == null) {
						string all = Strings.Join(DefaultMethods, "").ToUpper();
						if (!CatchDefaultMethods && (all.IndexOf(m.Name.ToUpper()) != -1))
							continue;
#else
                    if (((!CatchDefaultMethods) && (dftMethods.Contains(m.Name.ToUpper()))))
                        continue;
#endif

                    References.Add(new MethodReference(instance, m, index));
                    index += 1;
                }
            }
            else
            {
                foreach (MethodInfo m in objMethods)
                {
#if NET20
				
					string all = Strings.Join(DefaultMethods, "").ToUpper();
					if (!CatchDefaultMethods && (all.IndexOf(m.Name.ToUpper()) != -1))
						continue;
#else
                    //if (((!CatchDefaultMethods) && (DefaultMethods.Contains(m.Name.ToUpper()))))

#endif

                    References.Add(new MethodReference(instance, m, index));
                    index += 1;
                }
            }
            return References;
        }


    }

    #endregion
}

public abstract class BaseReference<T>
{

    public BaseReference(object Instance, T Info, int Index)
    {
        this.Instance = Instance;
        this.Info = Info;
        this.Index = Index;
    }

    public object Instance { get; set; }
    public T Info { get; set; }
    public int Index { get; set; }

}

public class PropertyReference : BaseReference<PropertyInfo>
{

    public PropertyReference(object Instance, PropertyInfo PropertyInfo, int Index) : base(Instance, PropertyInfo, Index)
    {
    }
}

public class MethodReference : BaseReference<MethodInfo>
{

    public MethodReference(object Instance, MethodInfo MethodInfo, int Index) : base(Instance, MethodInfo, Index)
    {
    }
}

public class FieldReference : BaseReference<FieldInfo>
{

    public FieldReference(object Instance, FieldInfo FieldInfo, int Index) : base(Instance, FieldInfo, Index)
    {
    }
}

public class EnumReference : BaseReference<string>
{

    public EnumReference(object Instance, string EnumName, int Index) : base(Instance, EnumName, Index)
    {
    }
}

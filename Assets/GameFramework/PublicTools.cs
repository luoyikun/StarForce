using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Serialization;

namespace GameFramework
{
    public class PublicTools
    {
        static JsonSerializerSettings m_jsonsettings = new JsonSerializerSettings()
        {

            ContractResolver = new ForceJSONSerializePrivatesResolver()
        };





        public static void DebugObj<T>(T data, string add = "")
        {
            string sRes = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore //循环引用问题
            });
            Debug.Log(add + "->" + sRes);
        }

        public static string GetObj2Json<T>(T data)
        {
            string sRes = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            return sRes;
        }

        public static void SaveString(string path, string JsonString)    //保存Json格式字符串
        {
            
            if (File.Exists(path) == true)
            {
                File.Delete(path);
            }
            FileInfo file = new FileInfo(path);   //保存文件的路径
            StreamWriter writer = file.CreateText();   //用文本写入的方式
            writer.Write(JsonString);   //写入数据
            writer.Close();   //关闭写指针
            writer.Dispose();    //销毁写指针
        }

        public static void DebugObj2<T>(T data, string pre = "",string path = "")
        {
            string sRes = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.None, m_jsonsettings);
            Debug.Log(pre + "->" + sRes);

            if (path != "")
            {
                SaveString(path, sRes);
            }
        }
    }

    public class ForceJSONSerializePrivatesResolver :

Newtonsoft.Json.Serialization.DefaultContractResolver

    {

        protected override IList<JsonProperty> CreateProperties

        (System.Type type, MemberSerialization memberSerialization)

        {

            var props = type.GetProperties(System.Reflection.BindingFlags.Public

            | System.Reflection.BindingFlags.NonPublic

            | System.Reflection.BindingFlags.Instance);

            System.Collections.Generic.List<JsonProperty> jsonProps =

            new System.Collections.Generic.List<JsonProperty>();

            foreach (var prop in props)

            {

                jsonProps.Add(base.CreateProperty(prop, memberSerialization));

            }

            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public

            | System.Reflection.BindingFlags.NonPublic

            | System.Reflection.BindingFlags.Instance))

            {

                jsonProps.Add(base.CreateProperty(field, memberSerialization));

            }

            jsonProps.ForEach(p => { p.Writable = true; p.Readable = true; });

            return jsonProps;

        }

    }

}

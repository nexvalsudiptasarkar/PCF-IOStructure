using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FileSystemLib.Common
{
    internal sealed class JsonParserUtil
    {
        #region Members Variables
        private readonly JsonSerializerSettings _serializerSettings = null;
        #endregion

        #region Constructor
        public JsonParserUtil(JsonSerializerSettings serializerSettings)
        {
            _serializerSettings = serializerSettings;
        }
        #endregion

        #region Public Methods
        public JToken GetJsonRootObject(string dataToParse)
        {
            try
            {
                var jo = JObject.Parse("{\"root\": " + dataToParse + "}");
                var root = jo["root"];

                return root;
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to parse data [Length in Characters:{0}]! Exception:{1}", dataToParse.Length, e.Message);
                Trace.TraceError(e.StackTrace);
                //Debug.Assert(false, string.Format("Failed to parse data [Length in Characters:{0}]! Exception:{1}", dataToParse.Length, e.Message));
                return null;
            }
        }

        public T[] DeserializeToList<T>(string jsonString, ref bool isParsingSuccessful)
        {
            isParsingSuccessful = false;
            try
            {
                List<T> l = Newtonsoft.Json.JsonConvert.DeserializeObject<List<T>>(jsonString, _serializerSettings);
                isParsingSuccessful = true;
                return l.Count <= 0 ? null : l.ToArray();
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to deserialize result to List of:{0}! Exception:{1}", typeof(T).ToString(), e.Message);
                Trace.TraceError(e.StackTrace);
            }
            return null;
        }

        #endregion

        #region Private Methods
        #endregion
    }
}
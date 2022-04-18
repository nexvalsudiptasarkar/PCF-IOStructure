using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace FileSystemLib.Common
{
    internal sealed class HttpUtil
    {
        private string _uriWithQueryStrings;

        #region Constructor
        public HttpUtil()
        {
            _uriWithQueryStrings = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Makes HTTP Request with POST method
        /// </summary>
        /// <param name="uri">The base URI</param>
        /// <param name="queryParameters">The query parameters</param>
        /// <param name="dataInRequestBody">The data to be passed in request body</param>
        /// <returns>Result as string if successful; else null</returns>
        public string ExecuteEx(string uri, IDictionary<string, string> queryParameters, string dataInRequestBody)
        {
            _uriWithQueryStrings = addEncodedParameters(uri, queryParameters);

            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(dataInRequestBody);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_uriWithQueryStrings);
            request.ContentType = "application/json; encoding='utf-8'";
            request.ContentLength = bytes.Length;
            request.Method = "POST";

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Close();
            }
            Debug.WriteLine(string.Format("Executing HTTP Request:{0}, Request Body:{1}...", _uriWithQueryStrings, dataInRequestBody));
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response != null)
                {
                    return extractTextFromResponseBody(response);
                }
                Trace.TraceError("Execution failed for HTTP Request:{0}, Request Body:{1}!", _uriWithQueryStrings, dataInRequestBody);
                return null;
            }
        }

        public string Execute(string restUrl, IDictionary<string, string> queryParameters)
        {
            _uriWithQueryStrings = addEncodedParameters(restUrl, queryParameters);

            Debug.WriteLine(string.Format("Executing HTTP Request:{0}...", _uriWithQueryStrings));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_uriWithQueryStrings);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response != null)
            {
                return extractTextFromResponseBody(response);
            }
            Trace.TraceError("Execution failed for HTTP Request:{0}, returned null as 'HttpWebResponse' object!", _uriWithQueryStrings);
            return null;
        }

        public string UriWithQueryStrings
        {
            get
            {
                return _uriWithQueryStrings;
            }
        }
        #endregion

        #region Private Methods
        private static string extractTextFromResponseBody(HttpWebResponse response)
        {
            var body = string.Empty;

            if (response.Headers.AllKeys.Contains("Content-Encoding"))
            {
                if (response.Headers["Content-Encoding"].Equals("gzip"))
                {
                    using (var stream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            body = reader.ReadToEnd();
                            return body.Trim();
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException(response.Headers["Content-Encoding"] + " encoding is not implemented!");
                }
            }
            //
            using (var stream = response.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    body = reader.ReadToEnd();
                }
            }
            return body.Trim();
        }

        private static string addEncodedParameters(string restAPI, IDictionary<string, string> parameters)
        {
            if (parameters == null)
                return restAPI;

            StringBuilder buffer = new StringBuilder(restAPI);
            bool firstParameter = true;
            bool hasQuestionMarkInUri = restAPI.Contains('?');
            foreach (var key in parameters.Keys)
            {
                if (firstParameter && !hasQuestionMarkInUri)
                {
                    firstParameter = false;
                    buffer.Append("?");
                }
                else
                {
                    buffer.Append("&");
                }
                string item = string.Format("{0}={1}", key, HttpUtility.UrlEncode(parameters[key]));
                buffer.Append(item);
            }
            return buffer.ToString();
        }
        #endregion
    }
}
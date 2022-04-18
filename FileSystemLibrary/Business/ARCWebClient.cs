using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace FileSystemLib
{
    internal sealed class ARCWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            request.Timeout = 600000;
            return request;
        }
    }
}


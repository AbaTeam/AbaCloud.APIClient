using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Abacloud.ApiClient
{
    class AbaCloudApiClientException : Exception
    {
        public AbaCloudApiClientException(string a_message) : base(a_message)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Abacloud.ApiClient
{
    public class Session
    {
        private readonly string stringValue;

        public Session(string a_string)
        {
            stringValue = a_string;
        }

        public override string ToString()
        {
            return stringValue;
        }
    }
}

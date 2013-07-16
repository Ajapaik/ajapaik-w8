using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ajapaik.Models
{
    public class FbData
    {
        private string _id;
        private string _token;
        private string _name;

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }
        public string Token
        {
            get { return _token; }
            set { _token = value; }
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}

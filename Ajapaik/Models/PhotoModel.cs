using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ajapaik.Models
{
    public class PhotoModel
    {
        private string _pictureKey;

        public string PictureKey
        {
            get { return _pictureKey; }
            set { _pictureKey = value; }
        }

        public PhotoModel(string key)
        {
            this._pictureKey = key;
        }
    }
}

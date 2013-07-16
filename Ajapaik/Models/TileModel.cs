using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Ajapaik.Models
{
    class TileModel
    {
        private int _id;
        private string _description;
        private string _imgurl;
        private string _rephotourl;
        private string _imgthumb;
        private string _photolink; 
        //private Connect helper = new Connect();
        
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string Imgurl
        {
            get { return _imgurl; }
            set { _imgurl = value; }
        }
        public string Rephotourl
        {
            get { return _rephotourl; }
            set { _rephotourl = value; }
        }
        public string Imgthumb
        {
            get { return _imgthumb; }
            set { _imgthumb = value; }
        }
        public string Photolink
        {
            get { return _photolink; }
            set { _photolink = value; }
        }


        public TileModel(JsonObject jsvalue)
        { 
            this._id = Convert.ToInt32(jsvalue["id"].GetNumber());
            this._description = jsvalue["description"].GetString();
            this._rephotourl = jsvalue["image_rephoto_url"].GetString();
            this._imgurl = jsvalue["image_source_url"].GetString();
            this._imgthumb = jsvalue["image_thumb"].GetString();
            this._photolink = jsvalue["photo_link"].GetString();   
        }
    }
}

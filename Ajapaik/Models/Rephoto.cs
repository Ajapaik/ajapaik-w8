using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Ajapaik.Models
{
    class Rephoto
    {
        // TODO!!!
        private int _id;
        private string _description;
        private string _fbuserid;
        private string _fbusername = "Anonüümne";
        private string _fbuserlink;
        private int _rephotoofid;
        private string _largeimgurl;
        private string _imgurl;
        private string _imgthumb;
        private string _photolink;
        private string _fbcommentsapi;
        private string _fbuserthumb;

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
        public string FbUserId
        {
            get { return _fbuserid; }
            set { _fbuserid = value; }
        }
        public string FbUserName
        {
            get { return _fbusername; }
            set { _fbusername = value; }
        }
        public string FbUserLink
        {
            get { return _fbuserlink; }
            set { _fbuserlink = value; }
        }
        public int RephotoOfId
        {
            get { return _rephotoofid; }
            set { _rephotoofid = value; }
        }
        public string LargeImgUrl
        {
            get { return _largeimgurl; }
            set { _largeimgurl = value; }
        }
        public string ImgUrl
        {
            get { return _imgurl; }
            set { _imgurl = value; }
        }
        public string ImgThumb
        {
            get { return _imgthumb; }
            set { _imgthumb = value; }
        }
        public string PhotoLink
        {
            get { return _photolink; }
            set { _photolink = value; }
        }
        public string FbCommentsApi
        {
            get { return _fbcommentsapi; }
            set { _fbcommentsapi = value; }
        }
        public string FbUserThumb
        {
            get { return _fbuserthumb; }
            set { _fbuserthumb = value; }
        }

        public Rephoto(JsonObject jsvalue)
        {
            this._id = Convert.ToInt32(jsvalue["id"].GetNumber());
            
            if (jsvalue["description"].ValueType != JsonValueType.Null)
            {
                this._description = jsvalue["description"].GetString();
            }
            if (jsvalue["fb_user_id"].ValueType != JsonValueType.Null)
            {
                this._fbuserid = jsvalue["fb_user_id"].GetString();
            }
            if (jsvalue["fb_user_name"].ValueType != JsonValueType.Null)
            {
                this._fbusername = jsvalue["fb_user_name"].GetString();
            }
            if (jsvalue["fb_user_link"].ValueType != JsonValueType.Null)
            {
                this._fbuserlink = jsvalue["fb_user_link"].GetString();
            }
            
            this._rephotoofid = Convert.ToInt32(jsvalue["rephoto_of_id"].GetNumber());
            this._largeimgurl = jsvalue["image_large"].GetString();
            this._imgurl = jsvalue["image_url"].GetString();
            this._imgthumb = jsvalue["image_thumb"].GetString();
            this._photolink = jsvalue["photo_link"].GetString();

            if (jsvalue["fb_comments_api"].ValueType != JsonValueType.Null)
            {
                this._fbcommentsapi = jsvalue["fb_comments_api"].GetString();
            }
            if (jsvalue["fb_user_thumb"].ValueType != JsonValueType.Null)
            {
                this._fbuserthumb = jsvalue["fb_user_thumb"].GetString();
            }
        }
    }
}

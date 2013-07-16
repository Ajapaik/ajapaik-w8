using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bing.Maps;
using Windows.Data.Json;
using Windows.UI;
using Windows.UI.Xaml.Media;
using System.Net.Http;
using System.ComponentModel;
using Ajapaik.Helpers;
using System.Globalization;

namespace Ajapaik.Models
{
    public class Pin : INotifyPropertyChanged
    {
        private int _id;
        private string _description;
        private Location _coordinates;
        private int _cityid;
        private string _imgurl;
        private string _largeImageurl;
        private string _imgthumb;
        private string _photolink;
        private string _photoapi;
        private int _rephotocount;
        private double _distance;
        private PhotoModel _photo;
        private Connect helper = new Connect();
        
        public event PropertyChangedEventHandler PropertyChanged;
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
        public Location Coordinates
        {
            get { return _coordinates; }
            set { _coordinates = value; }
        }
        public int Cityid
        {
            get { return _cityid; }
            set { _cityid = value; }
        }
        public string Imgurl
        {
            get { return _imgurl; }
            set { _imgurl = value; }
        }
        public string LargeImgUrl
        {
            get { return _largeImageurl; }
            set { _largeImageurl = value; }
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
        public string Photoapi
        {
            get { return _photoapi; }
            set { _photoapi = value; }
        }
        public int Rephotocount
        {
            get { return _rephotocount; }
            set { _rephotocount = value; }
        }
        public double Distance
        {
            get { return _distance; }
            set { _distance = value; }
        }
        public String DistanceString
        {
            get
            {
                if (this._distance < 1000)
                {
                    int dist = Convert.ToInt32(this._distance);
                    return string.Format("{0} m", dist);
                }
                else
                {
                    double dist = this._distance;
                    return string.Format("{0:0.00} km", dist / 1000);
                }
            }
        }
        public SolidColorBrush PinColor
        {
            get
            {
                if (this.Rephotocount > 0)
                {
                    return new SolidColorBrush(Colors.CornflowerBlue);
                }
                else
                {
                    return new SolidColorBrush(Colors.Black);
                }
            }
        }
        public PhotoModel Photo
        {
            get {
                if(_photo==null){
                    loadPhotoData();
                }
                return _photo; 
            }


            set { _photo = value; NotifyPropertyChanged("Photo"); }
        }

        public Pin(JsonObject jsvalue)
        {
            
            this._id = Convert.ToInt32(jsvalue["id"].GetNumber());
            this._description = jsvalue["description"].GetString();
            this._coordinates = new Location(Convert.ToDouble(jsvalue["lat"].Stringify(), new CultureInfo("en-US")), Convert.ToDouble(jsvalue["lon"].Stringify(), new CultureInfo("en-US")));
            this._cityid = Convert.ToInt32(jsvalue["city_id"].GetNumber());
            this._largeImageurl = jsvalue["image_large"].GetString();
            this._imgurl = jsvalue["image_url"].GetString();
            this._imgthumb = jsvalue["image_thumb"].GetString();
            this._photolink = jsvalue["photo_link"].GetString();
            this._photoapi = jsvalue["photo_api"].GetString();
            this._rephotocount = Convert.ToInt32(jsvalue["rephoto_count"].GetNumber());
            this._distance = jsvalue["distance_m"].GetNumber();           
           //loadPhotoData();
        }

        #region load photo data
        public async void loadPhotoData()
        {
            var json = await helper.getHttpResponseAsync(this.Photoapi);
            this.parseJson(json);
        }

        public void parseJson(string response)
        {
            JsonObject root = JsonObject.Parse(response);
            string pickey = "";

            IJsonValue json;
            if (root.TryGetValue("result", out json))
            {
                JsonObject obj = json.GetObject();

                IJsonValue svalue;
                if (obj.TryGetValue("source_data", out svalue))
                {
                    var jsonob = svalue.GetObject();
                    IJsonValue name;
                    IJsonValue desc;
                    if (jsonob.TryGetValue("description", out desc) &&
                        jsonob.TryGetValue("name", out name))
                    {
                        pickey += desc.GetString() + " ";
                        pickey += name.GetString() + " ";
                    }
                }

                IJsonValue skey;
                if (obj.TryGetValue("source_key", out skey))
                {
                    pickey += skey.GetString();
                    this.Photo = new PhotoModel(pickey);
                }
            }
        }
        #endregion

        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

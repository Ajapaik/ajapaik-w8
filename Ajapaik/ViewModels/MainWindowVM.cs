using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Bing.Maps;
using Windows.Data.Json;
using Windows.UI.Popups;
using Windows.Devices.Geolocation;
using Ajapaik.Models;
using System.Threading;
using System.ComponentModel;
using Ajapaik.Common;
using Ajapaik.Helpers;
using System.Globalization;

namespace Ajapaik.ViewModels
{
    class MainWindowVM:BindableBase
    {
        private ObservableCollection<Pin> _pushpins;
        private ObservableCollection<Pin> _pushpinsFull;

        internal ObservableCollection<Pin> PushpinsFull
        {
            get { return _pushpinsFull; }
            set { _pushpinsFull = value; }
        }
        private Location _mylocation;
        private Connect helper = new Connect();

        public ObservableCollection<Pin> Pushpins
        {
            get { return _pushpins; }
            set { _pushpins = value;
            OnPropertyChanged("Pushpins");
            }
        }

        public Location MyLocation
        {
            get { return _mylocation; }
            set { _mylocation = value;
            OnPropertyChanged("MyLocation");
            }
        }

        public MainWindowVM()
        {
            this._pushpins = new ObservableCollection<Pin>();
            this._pushpinsFull = new ObservableCollection<Pin>();
            this._mylocation = new Location();
        }

        public async Task loadPins(string search = "NoSearching")
        {
            var json = await helper.getHttpResponseAsync(this.urlBuilder(this._mylocation, search));
            this.parseJson(json);   
        }

        public void parseJson(string response)
        {
            JsonObject root = JsonObject.Parse(response);

            IJsonValue json;
            var tempPins = new ObservableCollection<Pin>();
            if (root.TryGetValue("result", out json))
            {
                JsonArray array = json.GetArray();
                
                foreach (var item in array)
                {
                    var pin = new Pin(item.GetObject());
                    //this.PushpinsFull.Add(pin);
                    tempPins.Add(pin);
                }
                this.Pushpins = tempPins;
            }
        }

        private string urlBuilder(Location loc, string search = "NoSearching")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(helper.getFromResourceFile("NearbyPhotosUrl01_part01"));
            if (loc.Latitude == 0.0 && loc.Longitude == 0.0)
            {
                sb.Append(helper.getFromResourceFile("NearbyPhotosUrl01_part02_alt"));
                sb.Append(helper.getFromResourceFile("NearbyPhotosUrl01_part03_alt"));
            }
            else
            {
                sb.Append(helper.getFromResourceFile("NearbyPhotosUrl01_part02"));
                sb.AppendFormat(new CultureInfo("en-US"), "{0}", loc.Latitude);
                sb.Append(helper.getFromResourceFile("NearbyPhotosUrl01_part03"));
                sb.AppendFormat(new CultureInfo("en-US"), "{0}", loc.Longitude);
            }
            if (search != "NoSearching")
            {
                sb.Append(helper.getFromResourceFile("NearbyPhotosUrl01_part04"));
                sb.AppendFormat(new CultureInfo("en-US"), "{0}", search);
            }
            return Uri.EscapeUriString(sb.ToString());
        }

        public async Task setGeoLocation(Location myCurrentLocation)
        {
            this.MyLocation = await helper.getMyLocation();
            if (MyLocation == null)
            {
                this.MyLocation = myCurrentLocation;
            }
        }

        public Bing.Maps.LocationRect GetLocationsRect(ObservableCollection<Pin> locations, Location locationMy = null)
        {
            var boundingRect = new Bing.Maps.LocationRect();

            if (locations.Count == 0)
            {
                if (locationMy != null)
                {
                    boundingRect.Center = locationMy;
                }
            }
            else
            {
                double minLatitude = locations[0].Coordinates.Latitude;
                double minLongitude = locations[0].Coordinates.Longitude;
                double maxLatitude = locations[0].Coordinates.Latitude;
                double maxLongitude = locations[0].Coordinates.Longitude;
                if (locationMy != null)
                {
                    minLatitude = locationMy.Latitude;
                    minLongitude = locationMy.Longitude;
                    maxLatitude = locationMy.Latitude;
                    maxLongitude = locationMy.Longitude;
                }

                foreach (Pin loc in locations)
                {
                    if (loc.Coordinates.Latitude < minLatitude)
                    {
                        minLatitude = loc.Coordinates.Latitude;
                    }
                    else if (loc.Coordinates.Latitude > maxLatitude)
                    {
                        maxLatitude = loc.Coordinates.Latitude;
                    }

                    if (loc.Coordinates.Longitude < minLongitude)
                    {
                        minLongitude = loc.Coordinates.Longitude;
                    }
                    else if (loc.Coordinates.Longitude > maxLongitude)
                    {
                        maxLongitude = loc.Coordinates.Longitude;
                    }
                }

                double width = maxLongitude - minLongitude;
                double height = maxLatitude - minLatitude;
                var center = new Bing.Maps.Location(0.5 * (maxLatitude + minLatitude), 0.5 * (maxLongitude + minLongitude));
                if (locationMy != null)
                {
                    center = locationMy;
                }
                boundingRect.Center = center;
                boundingRect.Width = width + 0.005;
                boundingRect.Height = height + 0.005;
            }
            return boundingRect;
        }
    }
}

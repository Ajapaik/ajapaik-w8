using Ajapaik.Helpers;
using Ajapaik.Models;
using Bing.Maps;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Popups;

namespace Ajapaik.ViewModels
{
    class ItemDetailPageVM
    {
        private Pin _item;
        private ObservableCollection<Rephoto> _rePhotos;
        private Connect _helper = new Connect();

        public ItemDetailPageVM(Pin item)
        {
            this._rePhotos = new ObservableCollection<Rephoto>();
            this._item = item;  
        }

        public Pin Item
        {
            get { return _item; }
        }

        public ObservableCollection<Rephoto> RePhotos
        {
            get { return _rePhotos; }
        }

        public async Task loadRephotos()
        {
           if (_helper.hasInternet())
            {
                var json = await _helper.getHttpResponseAsync(this.urlBuilder(Item.Id));
                this.parseJson(json);
            } 
        }

        public void parseJson(string response)
        {
            JsonObject root = JsonObject.Parse(response);

            IJsonValue json;
            IJsonValue json2;
            if (root.TryGetValue("result", out json))
            {

                var oldPhoto = json.GetObject();
                if (oldPhoto.TryGetValue("rephotos", out json2))
                {
                    JsonArray array = json2.GetArray();
                    foreach (var item in array)
                    {
                        var a = new Rephoto(item.GetObject());
                        this.RePhotos.Add(a);
                    }
                }
            }
        }

        private string urlBuilder(int oldPicId)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_helper.getFromResourceFile("RephotosUrl01_part01"));
            if (oldPicId == 0)
            {
                sb.Append(_helper.getFromResourceFile("RephotosUrl01_part02_alt"));
            }
            else
            {
                sb.Append(_helper.getFromResourceFile("RephotosUrl01_part02"));
                sb.Append(oldPicId);
            }
            return sb.ToString();
        }
    }
}

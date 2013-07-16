using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Popups;
using Bing.Maps;
using Windows.Devices.Geolocation;
using System.Threading;
using System.Net.Http;
using Windows.Devices.Sensors;
using Windows.Storage;
using System.Globalization;
using System.IO;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.Data.Json;
using System.Collections.ObjectModel;
using Ajapaik.Models;

namespace Ajapaik.Helpers
{
    class Connect
    {
        public bool hasInternet(bool showDialog = true)
        {
            ConnectionProfile connection = NetworkInformation.GetInternetConnectionProfile();
            if (connection != null && connection.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
            {
                connection = null;
                return true;
            }
            if (showDialog)
            {
                showBasicDialog("Interneti ühedus puudub.");
            }
            return false;

        }

        public bool getShareContent(DataRequest request, String share_id)
        {
            // The URI used in this sample is provided by the user so we need to ensure it's a wellformatted absolute URI
            // before we try to share it.
            //rootPage.NotifyUser("", NotifyType.StatusMessage);
            Uri dataPackageUri = new Uri(getFromResourceFile("ShareUrl01_part01") + share_id + getFromResourceFile("ShareUrl01_part02"), UriKind.Absolute);
            if (dataPackageUri != null)
            {
                DataPackage requestData = request.Data;
                requestData.Properties.Title = "Ajapaik";
                requestData.Properties.Description = "Kus ajalugu asub?"; // The description is optional.
                requestData.SetUri(dataPackageUri);
                return true;
            }
            else
            {
                request.FailWithDisplayText("Sisesta link ja proovi uuesti.");
                return false;
            }
        }

        public async Task<Location> getMyLocation(bool showDialog = true)
        {

            Location myloc = new Location();
            Geolocator locator = new Geolocator();

            try
            {
                // Get cancellation token
                var _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;

                // Carry out the operation
                Geoposition pos = await locator.GetGeopositionAsync().AsTask(token);

                myloc.Latitude = pos.Coordinate.Latitude;
                myloc.Longitude = pos.Coordinate.Longitude;
            }
            catch (UnauthorizedAccessException)
            {
                if (showDialog)
                {
                    showBasicDialog("Rakendus vaja teie luba asukoha tuvastamiseks.");
                }
                myloc = null;
            }
            catch (TaskCanceledException)
            {
                if (showDialog)
                {
                    showBasicDialog("Ülesanne katkestati.");
                }
                myloc = null;
            }
            catch (Exception)
            {
                if (showDialog)
                {
                    showBasicDialog("Asukoha tuvastamisega tekkis probleeme.");
                }
                myloc = null;
            }

            // pos.Coordinate.Accuracy.ToString();
            //MessageDialog result = new MessageDialog(myloc.Latitude.ToString());
            //result.ShowAsync();
            return myloc;
        }

        public async Task<string> getHttpResponseAsync(string url)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage stream = await client.GetAsync(new Uri(url));

            return await stream.Content.ReadAsStringAsync();
        }

        public string getFromResourceFile(string name, string fileName = "UrlStrings") 
        { 
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader(fileName);
            return loader.GetString(name);
        }

        public async Task<StorageFile> imagePicker()
        {

            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            //Converting needs to be added before
            //openPicker.FileTypeFilter.Add(".png");
            //openPicker.FileTypeFilter.Add(".bmp");
            //openPicker.FileTypeFilter.Add(".tif");
            //openPicker.FileTypeFilter.Add(".tiff");
            return await openPicker.PickSingleFileAsync();
        }

        public bool EnsureUnsnapped()
        {
            bool unsnapped = ((ApplicationView.Value != ApplicationViewState.Snapped) || ApplicationView.TryUnsnap());
            return unsnapped;
        }

        public async Task createTiles() 
        {
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            
            var thumbsUrls = await loadThumbsUrls();
            var k = 0;
            for (int j = 1; j <= 5; j++)
            {
                XmlDocument tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWideImageCollection);
                XmlDocument squareTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquareImage);
                
                //// Set the branding on the notification as specified in the input
                //// The logo and display name are declared in the manifest
                //// Branding defaults to logo if omitted
                XmlElement bindingElement = (XmlElement)tileXml.GetElementsByTagName("binding").Item(0);
                bindingElement.SetAttribute("branding", "none");
                XmlElement bindingSquareElement = (XmlElement)squareTileXml.GetElementsByTagName("binding").Item(0);
                bindingSquareElement.SetAttribute("branding", "none");

                #region input handling
                XmlNodeList imageElements = tileXml.GetElementsByTagName("image");
                XmlNodeList squareImageElements = squareTileXml.GetElementsByTagName("image");
                for (int i = 0; i < imageElements.Length; i++)
                {
                    XmlElement imageElement = (XmlElement)imageElements.Item((uint)(i));
                    string imageSource = String.Empty;
                    
                    //Minimum 10 uuendust on vaja
                    if (thumbsUrls.Count >= 9)
                    {
                        if (i==0)
                        {
                            switch (j)
                            {
                                case 1:
                                    imageSource = "ms-appx:///Assets/Logo.png";
                                    break;                                                              
                                case 2:
                                    imageSource = thumbsUrls[0].Imgurl;
                                    break;
                                case 3:
                                    if (thumbsUrls[0].Rephotourl != null)
                                    {
                                        imageSource = thumbsUrls[0].Rephotourl;
                                    }
                                    break;
                                case 4:
                                    imageSource = thumbsUrls[1].Imgurl;
                                    break;
                                case 5:
                                    if (thumbsUrls[0].Rephotourl != null)
                                    {
                                        imageSource = thumbsUrls[1].Rephotourl;
                                    }
                                    break;   
                                default:
                                    //Yle 5 erineva tile ei n2ita nagunii
                                    break;
                            } 
                            
                        } 
                        else if (i % 2 == 1)
	                    { 
                            if (j % 2 == 0)
                            {
                                imageSource = thumbsUrls[k].Imgurl;
                            }
                            else if (j % 2 == 1)
                            {
                                if (thumbsUrls[0].Rephotourl != null)
                                {
                                    imageSource = thumbsUrls[k].Rephotourl;
                                }
                            }
                        }
                        else if (i % 2 == 0)
                        {
                            if (j % 2 == 1)
                            {
                                imageSource = thumbsUrls[k].Imgurl;
                            }
                            else if (j % 2 == 0)
                            {
                                if (thumbsUrls[0].Rephotourl != null)
                                {
                                    imageSource = thumbsUrls[k].Rephotourl;
                                }
                            }
                            k++;
	                    } 

                        if (imageSource.Equals(String.Empty))
                        {
                            imageSource = "ms-appx:///Assets/Logo.png";
                        }
                    }
                    if (i == 0)
                    {
                        XmlElement squareImageElement = (XmlElement)squareImageElements.Item((uint)i);
                        squareImageElement.SetAttribute("src", imageSource);
                    }
                    imageElement.SetAttribute("src", imageSource);
                }
                #endregion

                TileNotification squareTile = new TileNotification(squareTileXml);
                TileUpdateManager.CreateTileUpdaterForApplication().Update(squareTile);
                TileNotification tile = new TileNotification(tileXml);
                TileUpdateManager.CreateTileUpdaterForApplication().Update(tile);
            }
        

            

            //// optionally, set the language of the notification
            //string lang = Lang.Text; // this needs to be a BCP47 tag
            //if (!String.IsNullOrEmpty(lang))
            //{
            //    // specify the language of the text in the notification 
            //    // this ensure the correct font is used to render the text
            //    XmlElement visualElement = (XmlElement)tileXml.GetElementsByTagName("visual").Item(0);
            //    visualElement.SetAttribute("lang", lang);
            //}
        }

        private async Task<ObservableCollection<TileModel>> loadThumbsUrls()
        {
            var json = await getHttpResponseAsync(getFromResourceFile("LiveTile"));
            return this.parseJson(json);
        }

        private ObservableCollection<TileModel> parseJson(string response)
        {
            JsonObject root = JsonObject.Parse(response);

            IJsonValue json;
            var tempThumbs = new ObservableCollection<TileModel>();
            if (root.TryGetValue("result", out json))
            {
                JsonArray array = json.GetArray();

                foreach (var item in array)
                {
                    var pin = new TileModel(item.GetObject());
                    //this.PushpinsFull.Add(pin);
                    tempThumbs.Add(pin);
                }
                return tempThumbs;
            }
            return tempThumbs;
        }
        private async void showBasicDialog(string message)
        {
            MessageDialog error = new MessageDialog(message);
            await error.ShowAsync();
        }
    }
}

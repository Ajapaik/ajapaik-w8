using Ajapaik.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Json;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Ajapaik.Helpers;
using Windows.Devices.Sensors;
using Bing.Maps;
using Facebook;
using System.Dynamic;
using Windows.Security.Authentication.Web;
using System.Globalization;
using Windows.UI.ViewManagement;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Ajapaik.Views
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class FullscreenCamPage : Ajapaik.Common.LayoutAwarePage
    {
        private HttpClient httpClient;
        MediaCapture manager;
        private DeviceInformationCollection devInfoCollection;
        private List<string> deviceList;
        private string currentDev = "";
        private bool previewing = false;
        private bool new_upload = false;
        private bool tappedVideo = false;
        private bool tappedResult = false;
        private bool isCamPhotoOpened = false;
        private Windows.Storage.StorageFile photoFile;
        
        private DataTransferManager dataTransferManager;
        private double newPicId;
        private InclinometerReading sensorRead;
        private Connect helper = new Connect();
        private Location loc;

        private string _facebookAppId; // You must set your own AppId here
        private string _permissions; // Set your permissions here (coma seperated)
        private FacebookClient _fb = new FacebookClient();
        private dynamic fbParameters;
        
        public FullscreenCamPage()
        {
            this.InitializeComponent();
            this._facebookAppId = helper.getFromResourceFile("FbAppId", "DataStrings");
            this._permissions = helper.getFromResourceFile("FbPermissions", "DataStrings");
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            Windows.Graphics.Display.DisplayProperties.AutoRotationPreferences = Windows.Graphics.Display.DisplayOrientations.Landscape;
            CamPhoto.Source = new BitmapImage(new Uri(App.ZoomLoc.LastSelectedOldPic.LargeImgUrl));
            
            if ((e.Parameter as StorageFile) != null)
            {
                this.photoFile = e.Parameter as StorageFile;
                var photoProp = await photoFile.Properties.GetImagePropertiesAsync();
                if (photoProp.Latitude.HasValue & photoProp.Longitude.HasValue)
                {
                    loc = new Location(photoProp.Latitude.Value, photoProp.Longitude.Value);
                }
                new_upload = true;
                makeReadyForUpload();
            }
            else
            {
                //...Sarting in CamMode
                await getListofWebcamsAsync();
                await loadCam("default", true);
            }

            createHttpClient(ref httpClient);
            camPhoto_MaxSized();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Unregister the current page as a share source.
            if (this.dataTransferManager != null)
            {
                this.dataTransferManager.DataRequested -= new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.onDataRequested);
                this.dataTransferManager = null;
            }
            Windows.Graphics.Display.DisplayProperties.AutoRotationPreferences = Windows.Graphics.Display.DisplayOrientations.None;
            dispose();

            base.OnNavigatedFrom(e);
        }
        
        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        private async void FilePickerBtnClick(object sender, RoutedEventArgs e)
        {
            if (helper.EnsureUnsnapped())
            {
                StorageFile file = await helper.imagePicker();
                if (file != null)
                {
                    // Application now has read/write access to the picked file 
                    //MessageDialog dialog = new MessageDialog("Valiti välja foto: " + file.Name);
                    //dialog.ShowAsync();
                    photoFile = file;
                    var photoProp = await photoFile.Properties.GetImagePropertiesAsync();
                    if (photoProp.Latitude.HasValue & photoProp.Longitude.HasValue)
                    {
                        loc = new Location(photoProp.Latitude.Value, photoProp.Longitude.Value);
                    }
                    new_upload = true;
                    makeReadyForUpload();
                }
                else
                {
                    if (ApplicationView.Value != ApplicationViewState.Snapped)
                    {
                        //showBasicDialog("Tegevus tühistati.");
                    }
                    else
                    {
                        showBasicDialog("Pildi valikuks pole piisavalt ruumi.");
                    }
                }
            }
        }

        private async Task loadCam(string deviceId = "default", bool goBack = false)
        {
            if (previewing)
            {
                await manager.StopPreviewAsync();
                CamVideo.Source = null;
            }
            manager = new MediaCapture();

            if (deviceList.Count > 0)
            {
                if (deviceList.Count == 1)
                {
                    deviceId = deviceList.FirstOrDefault();
                }
                else
                {
                    if (deviceId == "default")
                    {
                        deviceId = deviceList.LastOrDefault();
                        foreach (var devInfo in devInfoCollection)
                        {
                            //Finds camera on back side of screen if exsist
                            if (devInfo.EnclosureLocation != null && devInfo.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back)
                            {
                                deviceId = devInfo.Id;
                            }
                        }
                    }
                }
                currentDev = deviceId;
                manager = new MediaCapture();
                
                var settings = new MediaCaptureInitializationSettings();
                var chosenDevInfo = devInfoCollection[deviceList.IndexOf(deviceId)];
                settings.VideoDeviceId = chosenDevInfo.Id;
                settings.AudioDeviceId = "";
                settings.StreamingCaptureMode = StreamingCaptureMode.Video;

                try
                {
                    await manager.InitializeAsync(settings);

                    // Find the highest resolution available
                    VideoEncodingProperties resolutionMax = null;
                    int max = 0;
                    var resolutions = manager.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo);
                    for (var i = 0; i < resolutions.Count; i++)
                    {
                        var res = resolutions[i] as VideoEncodingProperties;
                        if (res != null)
                        {
                            if (res.Width * res.Height > max)
                            {
                                max = (int)(res.Width * res.Height);
                                resolutionMax = res;
                            }
                        }
                    }
                    //await manager.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, resolutionMax);
                    await manager.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, resolutionMax);
                    

                    CamVideo.Source = manager;
                    await manager.StartPreviewAsync();
                    previewing = true;
                }
                catch (UnauthorizedAccessException)
                {
                    showBasicDialog("Rakendus vajab teie luba kaamera kasutamiseks.", goBack);
                    return;
                }
                catch (Exception)
                {
                    showBasicDialog("Kaamera laadimine ebaõnnestus!", goBack);
                    return;
                }
            }
            else
            {
                //No devices found
                showBasicDialog("Ei leitud ühtegi kaamerat!", goBack);
                return;
            }

            slider1.Value = 50.0;
        }

        private async Task getListofWebcamsAsync()
        {

            //MessageDialog("Enumerating Webcams...");
            devInfoCollection = null;

            if (deviceList == null) deviceList = new List<string>();
            else deviceList.Clear();

            devInfoCollection = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            if (devInfoCollection.Count > 0)
            {
                for (int i = 0; i < devInfoCollection.Count; i++)
                {
                    var devInfo = devInfoCollection[i];
                    deviceList.Add(devInfo.Id);
                }
            }

        }

        private async void camVideoTap(object sender, TappedRoutedEventArgs e)
        {
            if (!tappedVideo)
            {
                tappedVideo = true;
                await getListofWebcamsAsync();
                if (deviceList.Count > 0)
                {
                    try
                    {
                    
                        photoFile = await Windows.Storage.KnownFolders.PicturesLibrary.CreateFileAsync("Photo.jpg", CreationCollisionOption.GenerateUniqueName); // name for picture library

                        if (!deviceList.Exists(currentDev.Equals))
                        {
                            //Crashes when working cam is removed even if put in try and catch.
                            //No devices found
                            showBasicDialog("Töötav kaamera eemaldati!", true);
                            return;
                        }
                        else
                        {
                            await manager.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), photoFile);
                            activateSensorData();
                            loc = await helper.getMyLocation(false);
                            if (loc != null)
                            {
                                await SaveGeoTag(photoFile, loc);
                            }
                            new_upload = true;
                        }
                        if (new_upload)
                        {
                            makeReadyForUpload();
                        }
                    }
                    catch (Exception)
                    {
                        tappedVideo = false;
                        return;
                    }
                }
                
                tappedVideo = false;
            }
        }

        public async Task<BitmapImage> GetBitmapAsync(StorageFile storageFile)
        {
            BitmapImage bitmap = new BitmapImage();
            IAsyncOperation<IRandomAccessStream> read = storageFile.OpenAsync(FileAccessMode.Read);
            IRandomAccessStream stream = await read;
            bitmap.SetSource(stream);

            return bitmap;
        }

        private async void makeReadyForUpload() 
        {
            if (photoFile != null)
            {
                if (previewing)
                {
                    await manager.StopPreviewAsync();
                    CamVideo.Source = null;
                    previewing = false;
                }
                var bitmap = new BitmapImage();
                bitmap = await GetBitmapAsync(photoFile);
                CamResult.Source = bitmap;
                slider1.Value = 75.0;
                CamResult.Visibility = Visibility.Visible;
                CamVideo.Visibility = Visibility.Collapsed;
                if (helper.hasInternet(false))
                {
                    var messageDialog = new MessageDialog("Kas soovid pilti üles laadida?");
                    // Add commands and set their callbacks
                    messageDialog.Commands.Add(new UICommand("Jah", (command) =>
                    {
                        if (helper.hasInternet(false))
                        {
                            uploadPicBtn(null, null);
                        }
                    }));
                    messageDialog.Commands.Add(new UICommand("Ei"));
                    // Set the command that will be invoked by default
                    messageDialog.DefaultCommandIndex = 1;
                    // Show the message dialog
                    await messageDialog.ShowAsync();
                }
            } 
        }

        private async Task SaveGeoTag(StorageFile photoFile, Location loc)
        {
            var propertyText = new Dictionary<string, object>(); 

            // Latitude and longitude are returned as double precision numbers, 
            // but we want to convert to degrees/minutes/seconds format. 
            propertyText.Add("System.GPS.LatitudeRef", (loc.Latitude >= 0) ? "N" : "S");
            propertyText.Add("System.GPS.LongitudeRef", (loc.Longitude >= 0) ? "E" : "W");
            
            double latDeg = Math.Floor(Math.Abs(loc.Latitude));
            double latMin = Math.Floor((Math.Abs(loc.Latitude) - latDeg) * 60);
            double latSec = (Math.Abs(loc.Latitude) - latDeg - latMin / 60) * 3600;
            uint[] latitude = { (uint)latDeg, (uint)latMin, (uint)(latSec*10000) };
            propertyText.Add("System.GPS.LatitudeNumerator", latitude);

            double longDeg = Math.Floor(Math.Abs(loc.Longitude));
            double longMin = Math.Floor((Math.Abs(loc.Longitude) - longDeg) * 60);
            double longSec = (Math.Abs(loc.Longitude) - longDeg - longMin / 60) * 3600;
            uint[] longitude = { (uint)longDeg, (uint)longMin, (uint)(longSec*10000) };
            propertyText.Add("System.GPS.LongitudeNumerator", longitude);

            uint[] denominator =  { 1, 1, 10000 };
            propertyText.Add("System.GPS.LatitudeDenominator", denominator);
            propertyText.Add("System.GPS.LongitudeDenominator", denominator);
            await photoFile.Properties.SavePropertiesAsync(propertyText);
            return;
        }

        private void slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            CamPhoto.Opacity = 1 - slider1.Value / 100;
            CamResult.Opacity = slider1.Value / 100;
        }
        
        private void sliderHeight_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ImageTransform.ScaleX = 0.5 + sliderHeight.Value/100* 3.5;
            ImageTransform.ScaleY = 0.5 + sliderHeight.Value / 100 * 3.5;
        }
        
        private async void camResultTap(object sender, TappedRoutedEventArgs e)
        {
            if (!tappedResult)
            {
                tappedResult = true;
                await getListofWebcamsAsync();

                if (deviceList.Count == 0)
                {
                    CamVideo.Visibility = Visibility.Collapsed;

                    //FilePickerBtnClick(sender,e);
                    //BAppBar.IsOpen = true;
                    this.Frame.GoBack();
                    return;
                }
                else
                {
                    await loadCam();
                    CamVideo.Visibility = Visibility.Visible;
                }
                CamResult.Visibility = Visibility.Collapsed;
                camPhoto_MaxSized();
                tappedResult = false;
            } 
        }

        private async void changeCamera(object sender, RoutedEventArgs e)
        {
            var place = deviceList.IndexOf(currentDev);
            var next = (place + 1) % deviceList.Count;
            if (!deviceList.Exists(currentDev.Equals))
            {

                //Crashes when working cam is removed even if put in try and catch.
                //No devices found
                showBasicDialog("Töötav kaamera eemaldati!", true);
                return;
            }
            else
            {
                await loadCam(deviceList[next]);
            }
        }

        private async void bottomAppBarOpened(object sender, object e)
        {
            await getListofWebcamsAsync();

            if (deviceList.Count > 1)
            {
                ChangeCamButton.IsEnabled = true;
            }
            else
            {
                ChangeCamButton.IsEnabled = false;
            }

            if (this.dataTransferManager != null)
            {
                ShareButton.IsEnabled = true;
            }
            else
            {
                ShareButton.IsEnabled = false;
            }

            if (new_upload)
            {
                UploadPicButton.IsEnabled = true;
                ShareButton.IsEnabled = false;
            }
            else
            {
                UploadPicButton.IsEnabled = false;
            }
        }

        private async void uploadPicBtn(object sender, RoutedEventArgs e)
        {
            if (App.FbParameters == null)
            {
                try
                {
                    await facebookLogin();
                }
                catch (Exception er)
                {
                    showBasicDialog(er.Message);
                    return;
                }

                if (App.FbParameters != null)
                {
                    if (helper.hasInternet())
                    {
                        
                        //uploadingPic();
                    }
                }
            }
            else
            {
                if (helper.hasInternet())
                {
                    uploadAndGetResponse();
                }
            }
        }

        // When share is invoked (by the user or programatically) the event handler we registered will be called to populate the datapackage with the
        // data to be shared.
        private void onDataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            // Call the scenario specific function to populate the datapackage with the data to be shared.
            if (helper.getShareContent(e.Request, newPicId.ToString()))
            {
                // Out of the datapackage properties, the title is required. If the scenario completed successfully, we need
                // to make sure the title is valid since the sample scenario gets the title from the user.
                if (String.IsNullOrEmpty(e.Request.Data.Properties.Title))
                {
                    //e.Request.FailWithDisplayText(MainPage.MissingTitleError);
                }
            }
        }

        private void shareClick(object sender, RoutedEventArgs e)
        {
            // If the user clicks the share button, invoke the share flow programatically.
            DataTransferManager.ShowShareUI();
        }

        private void activateSensorData()
        {
            Inclinometer inclinometer = Inclinometer.GetDefault();

            if (inclinometer != null)
            {
                sensorRead = inclinometer.GetCurrentReading();
            }
        }

        private void CamPhoto_ImageOpened(object sender, RoutedEventArgs e)
        {
            isCamPhotoOpened = true;
            camPhoto_MaxSized();
        }
        
        private void camPhoto_MaxSized()
        {

            if (isCamPhotoOpened)
            {
                //if (CamPhoto.ActualHeight > CamPhoto.ActualWidth)
                //{
                //    ImageTransform.Rotation = -90.0;
                //    var a = (Window.Current.Bounds.Height - CamPhoto.ActualHeight) / CamPhoto.ActualHeight;
                //    ImageTransform.ScaleX = 1 + a;
                //    ImageTransform.ScaleY = 1 + a;
                //}
                //else
                //{
                double scale;
                if (ImageHolder.ActualHeight >= CamPhoto.ActualHeight)
                {
                    scale = (ImageHolder.ActualHeight - CamPhoto.ActualHeight) / CamPhoto.ActualHeight;
                }
                else
                {
                    //BUG needs fix
                    scale = 0.6;
                }
                sliderHeight.Value = (0.5 + scale) / 3.5 * 100;
                //}
            }
        }

        private async Task facebookLogin()
        {
            var redirectUrl = helper.getFromResourceFile("FbLoginSuccessUrl");
            try
            {
                //fb.AppId = facebookAppId;
                var loginUrl = _fb.GetLoginUrl(new
                {
                    client_id = _facebookAppId,
                    redirect_uri = redirectUrl,
                    scope = _permissions,
                    display = "popup",
                    response_type = "token"
                });

                var endUri = new Uri(redirectUrl);

                WebAuthenticationResult WebAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(
                                                        WebAuthenticationOptions.None,
                                                        loginUrl,
                                                        endUri);
                if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.Success)
                {
                    var callbackUri = new Uri(WebAuthenticationResult.ResponseData.ToString());
                    var facebookOAuthResult = _fb.ParseOAuthCallbackUrl(callbackUri);
                    var accessToken = facebookOAuthResult.AccessToken;
                    if (String.IsNullOrEmpty(accessToken))
                    {
                        // User is not logged in, they may have canceled the login
                    }
                    else
                    {
                        // User is logged in and token was returned
                        loginSucceded(accessToken);
                    }

                }
                else if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.ErrorHttp)
                {
                    throw new InvalidOperationException("HTTP Error returned by AuthenticateAsync() : " + WebAuthenticationResult.ResponseErrorDetail.ToString());
                }
                else
                {
                    // The user canceled the authentication
                }
            }
            catch (Exception ex)
            {
                // Bad Parameter, SSL/TLS Errors and Network Unavailable errors are to be handled here.
                throw ex;
            }
        }

        private async void loginSucceded(string accessToken)
        {
            fbParameters = new ExpandoObject();
            fbParameters.access_token = accessToken;
            fbParameters.name = "";
            fbParameters.fields = "id,name";

            dynamic result = await _fb.GetTaskAsync("me", fbParameters);
            fbParameters = new ExpandoObject();
            fbParameters.id = result.id;
            fbParameters.name = result.name;
            fbParameters.access_token = accessToken;
            App.FbParameters = new FbData();
            App.FbParameters.Id = result.id;
            App.FbParameters.Name = result.name;
            App.FbParameters.Token = accessToken;

            var messageDialog = new MessageDialog("Oled ühendatud Facebook kasutajaga " + fbParameters.name + ". Kas soovid pilti üles laadida?");
            // Add commands and set their callbacks
            messageDialog.Commands.Add(new UICommand("Jah", (command) =>
            {
                if (App.FbParameters != null)
                {
                    uploadAndGetResponse();
                }
            }));
            messageDialog.Commands.Add(new UICommand("Ei"));
            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 1;
            // Show the message dialog
            await messageDialog.ShowAsync();
        }
        
        private async void uploadAndGetResponse()
        {
            double scale = CamPhoto.ActualHeight * ImageTransform.ScaleY / Window.Current.Bounds.Height;
            var response = await uploadingPic(scale);
            await displayTextResult(response);
        }

        private async Task<HttpResponseMessage> uploadingPic(double scale)
        {

            try
            {
                MultipartFormDataContent form = new MultipartFormDataContent();
                form.Add(new StreamContent(await photoFile.OpenStreamForReadAsync()), "user_file[]", "W8-" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss") + ".jpg"); // name for upload
                if (App.FbParameters != null)
                {
                    form.Add(new StringContent(App.FbParameters.Id), "fb_application_id");
                    form.Add(new StringContent(App.FbParameters.Token), "fb_access_token");
                }
                form.Add(new StringContent(String.Format(new CultureInfo("en-US"), "{0}", scale)), "scale_factor");
                if (loc != null)
                {
                    form.Add(new StringContent(String.Format(new CultureInfo("en-US"), "{0}", loc.Latitude)), "lat");
                    form.Add(new StringContent(String.Format(new CultureInfo("en-US"), "{0}", loc.Longitude)), "lon");
                }
                if (sensorRead != null)
                {
                    form.Add(new StringContent(String.Format(new CultureInfo("en-US"), "{0,5:0.00}", sensorRead.YawDegrees)), "yaw");
                    form.Add(new StringContent(String.Format(new CultureInfo("en-US"), "{0,5:0.00}", sensorRead.PitchDegrees)), "pitch");
                    form.Add(new StringContent(String.Format(new CultureInfo("en-US"), "{0,5:0.00}", sensorRead.RollDegrees)), "roll");
                }

                string resourceAddress = helper.getFromResourceFile("UploadPhotoUrl01_part01") + App.ZoomLoc.LastSelectedOldPic.Id.ToString() + helper.getFromResourceFile("UploadPhotoUrl01_part02");
                HttpResponseMessage response = await httpClient.PostAsync(resourceAddress, form);
                
                return response;
            }
            catch (HttpRequestException hre)
            {
                showBasicDialog(hre.Message);
                return null;
            }
            catch (TaskCanceledException)
            {
                //MessageDialog dialog = new MessageDialog("Katkestati.");
                //dialog.ShowAsync();
                return null;
            }
            finally
            {
                //Helpers.ScenarioCompleted(StartButton, CancelButton);
            }
        }

        internal static void createHttpClient(ref HttpClient httpClient)
        {
            if (httpClient != null)
            {
                httpClient.Dispose();
            }

            // HttpClient functionality can be extended by plugging multiple handlers together and providing
            // HttpClient with the configured handler pipeline.
            HttpMessageHandler handler = new HttpClientHandler();
            //handler = new PlugInHandler(handler); // Adds a custom header to every request and response message.            
            httpClient = new HttpClient(handler);

            // The following line sets a "User-Agent" request header as a default header on the HttpClient instance.
            // Default headers will be sent with every request sent from this HttpClient instance.
            //httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Ajapaik.App.Current.DebugSettings., ));
        }

        private void dispose()
        {
            if (httpClient != null)
            {
                httpClient.Dispose();
                httpClient = null;
            }
        }

        internal async Task displayTextResult(HttpResponseMessage response)
        {
            string text = "";
            string responseBodyAsText;

            // We cast the StatusCode to an int so we display the numeric value (e.g., "200") rather than the
            // name of the enum (e.g., "OK") which would often be redundant with the ReasonPhrase.
            text += ((int)response.StatusCode) + " " + response.ReasonPhrase + Environment.NewLine;

            if ((int)response.StatusCode == 200)
            {
                responseBodyAsText = await response.Content.ReadAsStringAsync();
                parseJson(responseBodyAsText);
                text += responseBodyAsText;
                new_upload = false;
            }
            //MessageDialog dialog = new MessageDialog(text);
            MessageDialog messageDialog = new MessageDialog("Pilt on üles laetud. Kas soovid jagada?");
            // Add commands and set their callbacks
            messageDialog.Commands.Add(new UICommand("Jah", (command) =>
            {
                DataTransferManager.ShowShareUI();
            }));
            messageDialog.Commands.Add(new UICommand("Ei"));
            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;
            // Show the message dialog
            await messageDialog.ShowAsync();

            if (this.dataTransferManager == null)
            {
                // Register the current page as a share source.
                this.dataTransferManager = DataTransferManager.GetForCurrentView();
                this.dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.onDataRequested);
            }
        }

        public void parseJson(string response)
        {
            Windows.Data.Json.JsonObject root = Windows.Data.Json.JsonObject.Parse(response);
            IJsonValue json;
            if (root.TryGetValue("new_id", out json))
            {
                //JsonArray array = json.GetArray();
                newPicId = json.GetNumber();
            }
        }

        private async void showBasicDialog(string message, bool goBack = false)
        {
            MessageDialog error = new MessageDialog(message);
            await error.ShowAsync();

            if (goBack)
            {
                this.Frame.GoBack();
                return;
            }
        }
    }
}

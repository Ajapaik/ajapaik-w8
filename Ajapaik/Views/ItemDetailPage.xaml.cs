using Ajapaik.Models;
using Ajapaik.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Ajapaik.Helpers;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Popups;
using System.Collections.ObjectModel;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.Storage;

// The Item Detail Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234232

namespace Ajapaik.Views
{
    /// <summary>
    /// A page that displays details for a single item within a group while allowing gestures to
    /// flip through other items belonging to the same group.
    /// </summary>
    public sealed partial class ItemDetailPage : Ajapaik.Common.LayoutAwarePage
    {
        private ItemDetailPageVM idvm;
        ObservableCollection<Rephoto> rePhotos;

        Connect helper = new Connect();
        
        public ItemDetailPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            //this.DataContext = e.Parameter;
            idvm = new ItemDetailPageVM(App.ZoomLoc.LastSelectedOldPic);
            this.DataContext = idvm.Item;
            await idvm.loadRephotos();

            rePhotos = new ObservableCollection<Rephoto>();
            progressBar.Maximum = idvm.RePhotos.Count;
            progressBar.Value = 0;

            itemGridView.ItemsSource = rePhotos;
            itemListView.ItemsSource = rePhotos;
            RePhotoUpdate();
            
            if (Window.Current.Bounds.Height <= 1080.0)
            {
                itemGridView.MaxHeight = Window.Current.Bounds.Height - 140.0;
            }
            else
            {
                itemGridView.MaxHeight = 940.0;
            }

            base.OnNavigatedTo(e);
        }

        private void RePhotoUpdate()
        {
            if (idvm.RePhotos.Count > progressBar.Value)
            {
                progressBar.Visibility = Visibility.Visible;
                rePhotos.Add(idvm.RePhotos.ElementAt((int)progressBar.Value));
            }
            else
            {
                progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void ImageOpened(object sender, RoutedEventArgs e)
        {
            progressBar.Value += 1;
            RePhotoUpdate();
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

        private void goOverToPhotoshot(object sender, RoutedEventArgs e)
        {
            if (!helper.EnsureUnsnapped())
            {
                showBasicDialog("Ekraanil pole piisavalt ruumi.");
            }
            else
            {
                this.Frame.Navigate(typeof(FullscreenCamPage), idvm.Item);
            }
        }

        private async void bottomAppBarOpened(object sender, object e)
        {
            var devInfoCollection = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            if (devInfoCollection.Count == 0){
                GoToCamBtn.IsEnabled = false;
            }else
            {
                GoToCamBtn.IsEnabled = true;
	        }
        }

        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            
            if (!helper.EnsureUnsnapped())
            {
                showBasicDialog("Ekraanil pole piisavalt ruumi.");
            }
            else
            {
                this.Frame.Navigate(typeof(PictureFullPage), idvm.Item);
            }
        }

        private void RePhotoClick(object sender, ItemClickEventArgs e)
        {
            if (!helper.EnsureUnsnapped())
            {
                showBasicDialog("Ekraanil pole piisavalt ruumi.");
            }
            else
            {
                this.Frame.Navigate(typeof(PictureFullPage), e.ClickedItem);
            }
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
                    this.Frame.Navigate(typeof(FullscreenCamPage), file);
                }
                else
                {
                    if(ApplicationView.Value != ApplicationViewState.Snapped){
                        //MessageDialog dialog = new MessageDialog("Tegevus tühistati.");
                        //dialog.ShowAsync();
                    }
                    else
                    {
                        showBasicDialog("Pildi valikuks pole piisavalt ruumi.");
                    }
                }
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

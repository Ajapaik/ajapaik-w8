using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Ajapaik.ViewModels;
using Ajapaik.Models;
using Windows.UI.Popups;
using Ajapaik.Helpers;
using System.Threading.Tasks;
using Bing.Maps;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace Ajapaik.Views
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class ItemsPage : Ajapaik.Common.LayoutAwarePage
    {
        ItemsPageVM ivm;
        ObservableCollection<Pin> pins;
        Connect helper = new Connect();

        public ItemsPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }
       
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Assign a bindable collection of items to this.DefaultViewModel["Items"]
            if ((e.Parameter as ObservableCollection<Pin>) != null)
            {
                pageTitle.Text = "Loend";
                ivm = new ItemsPageVM(e.Parameter as ObservableCollection<Pin>);
            }
            else if ((e.Parameter as string) != null)
            {
                pageTitle.Text = "Otsingu päring: " + (e.Parameter as string);
                var vm = new MainWindowVM();
                
                if (helper.hasInternet(false))
                {
                    
                    if (App.ZoomLoc == null)
                    {
                        App.ZoomLoc = new ZoomedLocation();
                        App.ZoomLoc.Loc = vm.MyLocation;
                        App.ZoomLoc.MapLookType = MapType.Road;
                        await vm.setGeoLocation(new Location(58.644817399944429, 25.066338372170776));
                    }
                    else
                    {
                        await vm.setGeoLocation(App.ZoomLoc.Loc);
                    }
                    await vm.loadPins(e.Parameter as string);
                    if (vm.Pushpins.Count == 0)
                    {
                        pageTitle.Text = "Ei leitud vastuseid: " + (e.Parameter as string);
                    }
                    else
                    {
                        pageTitle.Text = "Otsingu päring (" + vm.Pushpins.Count + " objekti): " + (e.Parameter as string);
                    }
                    ivm = new ItemsPageVM(vm.Pushpins);
                    progressBar.Maximum = ivm.Pinlist.Count;
                }
                else
                {
                    MessageDialog dialog = new MessageDialog("Otsing vajab ligipääsu internetti.");
                    await dialog.ShowAsync();
                    if (!App.askedInternetatStart)
                    { 
                        App.Current.Exit();
                        return;
                    }
                }
                App.askedInternetatStart = true;
            }

            pins = new ObservableCollection<Pin>();
            
            progressBar.Value = 0;
            itemGridView.ItemsSource = pins;
            itemListView.ItemsSource = pins;
            
            PinUpdate();
            base.OnNavigatedTo(e);
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

        private void PinUpdate()
        {
            if (ivm.Pinlist.Count > progressBar.Value)
            {
                progressBar.Visibility = Visibility.Visible;
                pins.Add(ivm.Pinlist.ElementAt((int)progressBar.Value));
            }
            else
            {
                progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void ItemClick(object sender, ItemClickEventArgs e)
        {
            if (helper.hasInternet())
            {
                App.ZoomLoc.LastSelectedOldPic = e.ClickedItem as Pin;
                
                this.Frame.Navigate(typeof(ItemDetailPage));
                //this.Frame.Navigate(typeof(MainPage));
                //MessageDialog dialogbox = new MessageDialog(item.Description);
                //dialogbox.ShowAsync();
            }
        }

        private void ImageOpened(object sender, RoutedEventArgs e)
        {
            progressBar.Value += 1;
            PinUpdate();
        }

    }
}

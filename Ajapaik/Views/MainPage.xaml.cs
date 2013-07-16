using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Ajapaik.ViewModels;
using Windows.Networking.Connectivity;
using Windows.UI.Popups;
using Ajapaik.Models;
using Bing.Maps;
using System.Threading.Tasks;
using Ajapaik.Helpers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Ajapaik.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Ajapaik.Common.LayoutAwarePage
    {
        MainWindowVM vm;
        Connect helper = new Connect();
        bool pointerIsPressed = false;
        bool pointerIsDragging = false;
        private SearchPane searchPane;
        
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded; 
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {  
            vm = new MainWindowVM();
            Container.DataContext = vm;
            
            if (helper.hasInternet(false))
            {
                await vm.setGeoLocation(MainMap.Center);
            } else
            {
                if (!App.askedInternetatStart)
                {
                    MessageDialog dialog = new MessageDialog("Rakendus vajab internetti.");
                    await dialog.ShowAsync();
                    App.Current.Exit();
                    return;
                }
            }

            if (App.ZoomLoc == null)
            {
                App.ZoomLoc = new ZoomedLocation();
            } else
            {
                MainMap.MapType = App.ZoomLoc.MapLookType;
                MainMap.SetView(MainMap.Center, MainMap.ZoomLevel);
                if (helper.hasInternet(false))
                {
                    vm.MyLocation = App.ZoomLoc.Loc;
                }
            }
            if (helper.hasInternet())
            {
                await vm.loadPins();
                Bing.Maps.LocationRect boundingRect = vm.GetLocationsRect(vm.Pushpins, vm.MyLocation);
                await Task.Delay(700);
                MainMap.SetView(boundingRect);
            }

            MainMap.ViewChangeEnded += _map_ViewChangeEnded;
            MainMap.ViewChangeStarted += _map_ViewChangeStarted;
            MainMap.PointerPressedOverride += _map_PointerPressedOverride;
            MainMap.PointerMovedOverride += _map_PointerMovedOverride;
            MainMap.PointerReleasedOverride += _map_PointerReleasedOverride;
            
            App.askedInternetatStart = true;

        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (helper.hasInternet(false))
            {
                //CreateTiles() j2tta synkroonselt jooksma
                helper.createTiles();
            }
            
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (App.ZoomLoc != null)
            {
                App.ZoomLoc.MapLookType = MainMap.MapType;
                App.ZoomLoc.Loc = MainMap.Center;
                App.ZoomLoc.Zoom = MainMap.ZoomLevel;
            }
            base.OnNavigatedFrom(e);
        }

        void _map_PointerPressedOverride(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            pointerIsPressed = true;
        }

        void _map_PointerMovedOverride(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (pointerIsPressed)
                pointerIsDragging = true;
        }

        async void _map_PointerReleasedOverride(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            pointerIsPressed = false;
            if (pointerIsDragging)
            {
                // guard against inertia
                await Task.Delay(200);
                pointerIsDragging = false;
            }
        }

        async void _map_ViewChangeEnded(object sender, ViewChangeEndedEventArgs e)
        {
            bool WasUserInitiated = pointerIsDragging;

            if (WasUserInitiated)
            {
                MainMap.SetView(MainMap.Center);
                pointerIsDragging = false;
                await Task.Delay(2000);
                if (helper.hasInternet(false))
                {
                    vm.MyLocation = MainMap.Center;
                    MainMap.SetView(vm.MyLocation);
                    //Tryout
                    //Bing.Maps.LocationRect boundingRect = vm.GetLocationsRect(vm.Pushpins);
                    //MainMap.SetView(boundingRect);
                    await vm.loadPins();
                }
            }
            //MainMap.UpdateLayout();
        }

        void _map_ViewChangeStarted(object sender, ViewChangeStartedEventArgs e)
        {
            bool WasUserInitiated = pointerIsDragging;

            if (WasUserInitiated)
            {
                ScreenCenter.Text = "*";
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            searchPane = SearchPane.GetForCurrentView();
            searchPane.Show();
        }

        private async void List_Click(object sender, RoutedEventArgs e)
        {
            vm.MyLocation = MainMap.Center;
            if (helper.hasInternet())
            {
                await vm.loadPins();
                this.Frame.Navigate(typeof(ItemsPage), vm.Pushpins);
            }
        }

        private async void Location_Click(object sender, RoutedEventArgs e)
        {
            MyLocBtn.IsEnabled = false;
            if (helper.hasInternet())
            {
                await vm.setGeoLocation(MainMap.Center);
                await vm.loadPins();
                ScreenCenter.Text = "X";
               
                if (vm.Pushpins.Count > 0)
                {
                    Bing.Maps.LocationRect boundingRect = vm.GetLocationsRect(vm.Pushpins,vm.MyLocation);
                    MainMap.SetView(boundingRect);
                }
                else
                {
                    try
                    {
                        MainMap.SetView(vm.MyLocation, 15);
                    }
                    catch (Exception)
                    {
                        MyLocBtn.IsEnabled = true;
                        return;
                    }
                }
            }
            MyLocBtn.IsEnabled = true;
        } 

        private void Pin_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (helper.hasInternet())
            {
                TextBlock tb = e.OriginalSource as TextBlock;

                try
                {
                    if (tb.Text.Equals(null)) return;

                    var item = (from x in vm.Pushpins
                                where x.Id.ToString() == tb.Text
                                select x).Single();

                    if (item == null) return;
                    App.ZoomLoc.LastSelectedOldPic = item;
                    this.Frame.Navigate(typeof(ItemDetailPage));
                }
                catch (Exception)
                {
                    return;
                }
            }
        }
    }
}

using Ajapaik.Common;
using Ajapaik.Models;
using Ajapaik.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Search;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;


// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace Ajapaik
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {

        public static FbData FbParameters;
        public static ZoomedLocation ZoomLoc;
        public static bool askedInternetatStart = false;
        
        public bool isEventRegistered = false;
        // This is the container that will hold our custom content.
        private Popup settingsPopup;
        // Used to determine the correct height to ensure our custom UI fills the screen.
        private Rect windowBounds;
        // Desired width for the settings UI. UI guidelines specify this should be 346 or 646 depending on your needs.
        private double settingsWidth = 346;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            //App.Current.DebugSettings.EnableFrameRateCounter = true;

            // Enable the notification queue - this only needs to be called once in the lifetime of your app
            TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);

            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
            await EnsureMainPageActivatedAsync(args);
        }
        /// Invoked when the window size is updated.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        void OnWindowSizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            windowBounds = Window.Current.Bounds;
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
        protected override void OnWindowCreated(WindowCreatedEventArgs args) 
        {
            base.OnWindowCreated(args);
            windowBounds = Window.Current.Bounds;
            // Added to listen for events when the window size is updated.
            Window.Current.SizeChanged += OnWindowSizeChanged;
            if (!isEventRegistered)
            {
                // Listening for this event lets the app initialize the settings commands and pause its UI until the user closes the pane.
                // To ensure your settings are available at all times in your app, place your CommandsRequested handler in the overridden
                // OnWindowCreated of App.xaml.cs
                SettingsPane.GetForCurrentView().CommandsRequested += onCommandsRequested;
                isEventRegistered = true;
            }

            // Register QuerySubmitted handler for the window at window creation time and only registered once
            // so that the app can receive user queries at any time.
            SearchPane.GetForCurrentView().QuerySubmitted += new TypedEventHandler<SearchPane, SearchPaneQuerySubmittedEventArgs>(OnQuerySubmitted);
        
        }
        /// <summary>
        /// This event is generated when the user opens the settings pane. During this event, append your
        /// SettingsCommand objects to the available ApplicationCommands vector to make them available to the
        /// SettingsPange UI.
        /// </summary>
        /// <param name="settingsPane">Instance that triggered the event.</param>
        /// <param name="eventArgs">Event data describing the conditions that led to the event.</param>
        void onCommandsRequested(SettingsPane settingsPane, SettingsPaneCommandsRequestedEventArgs eventArgs)
        {
            UICommandInvokedHandler handler = new UICommandInvokedHandler(onSettingsCommand);

            SettingsCommand aboutCommand = new SettingsCommand("AboutAppId", "Rakenduse kirjeldus", handler);
            eventArgs.Request.ApplicationCommands.Add(aboutCommand);
            SettingsCommand privacyCommand = new SettingsCommand("PrivacyPolicyId", "Privaatsuspoliitika", handler);
            eventArgs.Request.ApplicationCommands.Add(privacyCommand);
            SettingsCommand termsofuseCommand = new SettingsCommand("TermsOfUseId", "Kasutajatingimused", handler);
            eventArgs.Request.ApplicationCommands.Add(termsofuseCommand);
        }
        /// <summary>
        /// This the event handler for the "Defaults" button added to the settings charm. This method
        /// is responsible for creating the Popup window will use as the container for our settings Flyout.
        /// The reason we use a Popup is that it gives us the "light dismiss" behavior that when a user clicks away 
        /// from our custom UI it just dismisses.  This is a principle in the Settings experience and you see the
        /// same behavior in other experiences like AppBar. 
        /// </summary>
        /// <param name="command"></param>
        void onSettingsCommand(IUICommand command)
        {
            //rootPage.NotifyUser("Defaults command invoked", NotifyType.StatusMessage);

            // Create a Popup window which will contain our flyout.
            settingsPopup = new Popup();
            settingsPopup.Closed += OnPopupClosed;
            Window.Current.Activated += OnWindowActivated;
            settingsPopup.IsLightDismissEnabled = true;
            settingsPopup.Width = settingsWidth;
            settingsPopup.Height = windowBounds.Height;

            // Add the proper animation for the panel.
            settingsPopup.ChildTransitions = new TransitionCollection();
            settingsPopup.ChildTransitions.Add(new PaneThemeTransition()
            {
                Edge = (SettingsPane.Edge == SettingsEdgeLocation.Right) ?
                       EdgeTransitionLocation.Right :
                       EdgeTransitionLocation.Left
            });
            if (command.Id.ToString() == "AboutAppId")
            {
                // Create a SettingsFlyout the same dimenssions as the Popup.
                SettingsAboutApp mypane = new SettingsAboutApp();
                mypane.Width = settingsWidth;
                mypane.Height = windowBounds.Height;
                // Place the SettingsFlyout inside our Popup window.
                settingsPopup.Child = mypane;
            }
            else if (command.Id.ToString() == "PrivacyPolicyId")
            {
                // Create a SettingsFlyout the same dimenssions as the Popup.
                SettingsPrivacyPolicy mypane = new SettingsPrivacyPolicy();
                mypane.Width = settingsWidth;
                mypane.Height = windowBounds.Height;
                // Place the SettingsFlyout inside our Popup window.
                settingsPopup.Child = mypane;
            }
            else if (command.Id.ToString() == "TermsOfUseId")
            {
                // Create a SettingsFlyout the same dimenssions as the Popup.
                SettingsTermsOfUse mypane = new SettingsTermsOfUse();
                mypane.Width = settingsWidth;
                mypane.Height = windowBounds.Height;
                // Place the SettingsFlyout inside our Popup window.
                settingsPopup.Child = mypane;
            }

            // Let's define the location of our Popup.
            settingsPopup.SetValue(Canvas.LeftProperty, SettingsPane.Edge == SettingsEdgeLocation.Right ? (windowBounds.Width - settingsWidth) : 0);
            settingsPopup.SetValue(Canvas.TopProperty, 0);
            settingsPopup.IsOpen = true;
        }
        
        /// <summary>
        /// We use the window's activated event to force closing the Popup since a user maybe interacted with
        /// something that didn't normally trigger an obvious dismiss.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        private void OnWindowActivated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                settingsPopup.IsOpen = false;
            }
        }

        /// <summary>
        /// When the Popup closes we no longer need to monitor activation changes.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        void OnPopupClosed(object sender, object e)
        {
            Window.Current.Activated -= OnWindowActivated;
        }

        async private Task EnsureMainPageActivatedAsync(IActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

             //Do not repeat app initialization when the Window already has content,
             //just ensure that the window is active
            if (rootFrame == null)
            {
                 //Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                    // Do an asynchronous restore
                    await SuspensionManager.RestoreAsync();
                }

                //Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                 //When the navigation stack isn't restored navigate to the first page,
                 //configuring the new page by passing required information as a navigation
                 //parameter
                if (!rootFrame.Navigate(typeof(Views.MainPage)))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// This is the handler for Search activation.
        /// </summary>
        /// <param name="args">This is the list of arguments for search activation, including QueryText and Language</param>
        async protected override void OnSearchActivated(SearchActivatedEventArgs args)
        {
            await EnsureMainPageActivatedAsync(args);
            if (args.QueryText == "")
            {
                // navigate to landing page.
            }
            else
            {
                // display search results.
                (Window.Current.Content as Frame).Navigate(typeof(ItemsPage), args.QueryText);
            }
        }

        private void OnQuerySubmitted(object sender, SearchPaneQuerySubmittedEventArgs args)
        {
            if (Window.Current != null)
            {
                (Window.Current.Content as Frame).Navigate(typeof(ItemsPage), args.QueryText);
            }
        }
    }
}

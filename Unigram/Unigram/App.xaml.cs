﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.HockeyApp;
using Telegram.Api.Helpers;
using Telegram.Api.Services;
using Telegram.Api.Services.Cache;
using Telegram.Api.Services.Updates;
using Template10.Common;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Unigram.Views;
using Unigram.Core.Notifications;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Media.SpeechRecognition;
using Windows.ApplicationModel.VoiceCommands;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;
using Unigram.Tasks;
using Windows.UI.Notifications;
using Windows.Storage;
using Windows.UI.Popups;
using Unigram.Common;
using Windows.Media;
using System.IO;
using Template10.Services.NavigationService;
using Unigram.Views.SignIn;
using Windows.UI.Core;
using Unigram.Converters;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel.Core;
using System.Collections;
using Telegram.Api.TL;
using System.Collections.Generic;
using Unigram.Core.Services;
using Template10.Controls;
using Windows.Foundation;
using Windows.ApplicationModel.Contacts;
using Telegram.Api.Aggregator;
using Unigram.Controls;
using Unigram.Views.Users;
using System.Linq;
using Telegram.Logs;

namespace Unigram
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : BootStrapper
    {
        public static ShareOperation ShareOperation { get; private set; }
        public static AppServiceConnection Connection { get; private set; }

        public static AppInMemoryState InMemoryState { get; } = new AppInMemoryState();

        public static event TypedEventHandler<CoreDispatcher, AcceleratorKeyEventArgs> AcceleratorKeyActivated;

        public ViewModelLocator Locator
        {
            get
            {
                return Resources["Locator"] as ViewModelLocator;
            }
        }

        private BackgroundTaskDeferral appServiceDeferral = null;

        private MediaExtensionManager m_mediaExtensionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            m_mediaExtensionManager = new MediaExtensionManager();
            m_mediaExtensionManager.RegisterByteStreamHandler("Unigram.Native.OpusByteStreamHandler", ".ogg", "audio/ogg");

            if (SettingsHelper.SwitchGuid != null)
            {
                SettingsHelper.SessionGuid = SettingsHelper.SwitchGuid;
                SettingsHelper.SwitchGuid = null;
            }

            FileUtils.CreateTemporaryFolder();

            UnhandledException += async (s, args) =>
            {
                args.Handled = true;

                try
                {
                    await new TLMessageDialog(args.Exception?.ToString() ?? string.Empty, "Unhandled exception").ShowQueuedAsync();
                }
                catch { }
            };

#if !DEBUG

            HockeyClient.Current.Configure("7d36a4260af54125bbf6db407911ed3b",
                new TelemetryConfiguration()
                {
                    EnableDiagnostics = true,
                    Collectors = WindowsCollectors.Metadata |
                                 WindowsCollectors.Session |
                                 WindowsCollectors.UnhandledException
                });

#endif
        }

        public static bool IsActive { get; private set; }
        public static bool IsVisible { get; private set; }

        private void Window_Activated(object sender, WindowActivatedEventArgs e)
        {
            IsActive = e.WindowActivationState != CoreWindowActivationState.Deactivated;
            HandleActivated(e.WindowActivationState != CoreWindowActivationState.Deactivated);
        }

        private void Window_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            IsVisible = e.Visible;
            //HandleActivated(e.Visible);

            //if (e.Visible)
            //{
            //    Log.Write("Window_VisibilityChanged");

            //    Task.Run(() => Locator.LoadStateAndUpdate());
            //}
            //else
            //{
            //    var cacheService = UnigramContainer.Current.ResolveType<ICacheService>();
            //    if (cacheService != null)
            //    {
            //        cacheService.TryCommit();
            //    }

            //    var updatesService = UnigramContainer.Current.ResolveType<IUpdatesService>();
            //    if (updatesService != null)
            //    {
            //        updatesService.SaveState();
            //        updatesService.CancelUpdating();
            //    }
            //}
        }

        private void HandleActivated(bool active)
        {
            var aggregator = UnigramContainer.Current.ResolveType<ITelegramEventAggregator>();
            aggregator.Publish(active ? "Window_Activated" : "Window_Deactivated");

            //if (active)
            //{
            //    var protoService = UnigramContainer.Current.ResolveType<IMTProtoService>();
            //    protoService.UpdateStatusAsync(false, null);
            //}
            //else
            //{
            //    var protoService = UnigramContainer.Current.ResolveType<IMTProtoService>();
            //    protoService.UpdateStatusAsync(true, null);
            //}
        }

        /////// <summary>
        /////// Initializes the app service on the host process 
        /////// </summary>
        ////protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        ////{
        ////    base.OnBackgroundActivated(args);

        ////    if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails)
        ////    {
        ////        appServiceDeferral = args.TaskInstance.GetDeferral();
        ////        AppServiceTriggerDetails details = args.TaskInstance.TriggerDetails as AppServiceTriggerDetails;
        ////        Connection = details.AppServiceConnection;
        ////    }
        ////    else if (args.TaskInstance.TriggerDetails is RawNotification)
        ////    {
        ////        var task = new NotificationTask();
        ////        task.Run(args.TaskInstance);
        ////    }
        ////    else if (args.TaskInstance.TriggerDetails is ToastNotificationActionTriggerDetail)
        ////    {
        ////        // TODO: upgrade the task to take advanges from in-process execution.
        ////        var task = new InteractiveTask();
        ////        task.Run(args.TaskInstance);
        ////    }
        ////}

        public override UIElement CreateRootElement(IActivatedEventArgs e)
        {
            var navigationFrame = new Frame();
            var navigationService = NavigationServiceFactory(BackButton.Ignore, ExistingContent.Include, navigationFrame) as NavigationService;
            navigationService.SerializationService = TLSerializationService.Current;

            //return new ModalDialog
            //{
            //    DisableBackButtonWhenModal = false,
            //    Content = navigationFrame
            //};

            return navigationFrame;
        }

        public override Task OnInitializeAsync(IActivatedEventArgs args)
        {
            Execute.Initialize();
            Locator.Configure();

            if (Window.Current != null)
            {
                Window.Current.Activated -= Window_Activated;
                Window.Current.Activated += Window_Activated;
                Window.Current.VisibilityChanged -= Window_VisibilityChanged;
                Window.Current.VisibilityChanged += Window_VisibilityChanged;
                Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated -= Dispatcher_AcceleratorKeyActivated;
                Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;

                ShowStatusBar();
                ColourTitleBar();
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(320, 500));
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

                Theme.Current.Update();
                App.RaiseThemeChanged();
            }

            return base.OnInitializeAsync(args);
        }

        public override async Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            if (SettingsHelper.IsAuthorized)
            {
                if (args is ShareTargetActivatedEventArgs share)
                {
                    ShareOperation = share.ShareOperation;
                    NavigationService.Navigate(typeof(ShareTargetPage));
                }
                else if (args is VoiceCommandActivatedEventArgs voice)
                {
                    SpeechRecognitionResult speechResult = voice.Result;
                    string command = speechResult.RulePath[0];

                    if (command == "ShowAllDialogs")
                    {
                        NavigationService.Navigate(typeof(MainPage));
                    }
                    if (command == "ShowSpecificDialog")
                    {
                        //#TODO: Fix that this'll open a specific dialog
                        NavigationService.Navigate(typeof(MainPage));
                    }
                    else
                    {
                        NavigationService.Navigate(typeof(MainPage));
                    }
                }
                else if (args is ContactPanelActivatedEventArgs contact)
                {
                    var backgroundBrush = Application.Current.Resources["TelegramBackgroundTitlebarBrush"] as SolidColorBrush;
                    contact.ContactPanel.HeaderColor = backgroundBrush.Color;

                    var annotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
                    var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
                    if (store != null && annotationStore != null)
                    {
                        var full = await store.GetContactAsync(contact.Contact.Id);
                        if (full == null)
                        {
                            goto Navigate;
                        }

                        var annotations = await annotationStore.FindAnnotationsForContactAsync(full);

                        var first = annotations.FirstOrDefault();
                        if (first == null)
                        {
                            goto Navigate;
                        }

                        var remote = first.RemoteId;
                        if (int.TryParse(remote.Substring(1), out int userId))
                        {
                            NavigationService.Navigate(typeof(DialogPage), new TLPeerUser { UserId = userId });
                        }
                        else
                        {
                            goto Navigate;
                        }
                    }
                    else
                    {
                        NavigationService.Navigate(typeof(MainPage));
                    }

                    Navigate:
                    NavigationService.Navigate(typeof(MainPage));
                }
                else if (args is ProtocolActivatedEventArgs protocol)
                {
                    NavigationService.Navigate(typeof(MainPage), protocol.Uri.ToString());
                }
                else
                {
                    var activate = args as ToastNotificationActivatedEventArgs;
                    var launch = activate?.Argument ?? null;

                    NavigationService.Navigate(typeof(MainPage), launch);
                }
            }
            else
            {
                NavigationService.Navigate(typeof(SignInWelcomePage));
            }

            Window.Current.Activated -= Window_Activated;
            Window.Current.Activated += Window_Activated;
            Window.Current.VisibilityChanged -= Window_VisibilityChanged;
            Window.Current.VisibilityChanged += Window_VisibilityChanged;
            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated -= Dispatcher_AcceleratorKeyActivated;
            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;

            ShowStatusBar();
            ColourTitleBar();
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(320, 500));
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

            Theme.Current.Update();
            App.RaiseThemeChanged();

            Task.Run(() => OnStartSync());
            //return Task.CompletedTask;
        }

        private void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (AcceleratorKeyActivated is MulticastDelegate multicast)
            {
                var list = multicast.GetInvocationList();
                for (int i = list.Length - 1; i >= 0; i--)
                {
                    var result = list[i].DynamicInvoke(sender, args);
                    if (args.Handled)
                    {
                        return;
                    }
                }
            }
        }

        private async void OnStartSync()
        {
            //#if DEBUG
            await VoIPConnection.Current.ConnectAsync();
            //#endif

            await Toast.RegisterBackgroundTasks();

            BadgeUpdateManager.CreateBadgeUpdaterForApplication("App").Clear();
            TileUpdateManager.CreateTileUpdaterForApplication("App").Clear();
            ToastNotificationManager.History.Clear("App");

#if !DEBUG && !PREVIEW
            Execute.BeginOnThreadPool(async () =>
            {
                await new AppUpdateService().CheckForUpdatesAsync();
            });
#endif

            //if (ApiInformation.IsTypePresent("Windows.ApplicationModel.FullTrustProcessLauncher"))
            //{
            //    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            //}

            try
            {
                // Prepare stuff for Cortana
                var localVoiceCommands = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///VoiceCommands/VoiceCommands.xml"));
                await VoiceCommandDefinitionManager.InstallCommandDefinitionsFromStorageFileAsync(localVoiceCommands);
            }
            catch { }
        }

        public override async void OnResuming(object s, object e, AppExecutionState previousExecutionState)
        {
            Log.Write("OnResuming");

            if (SettingsHelper.IsAuthorized)
            {
                var updatesService = UnigramContainer.Current.ResolveType<IUpdatesService>();
                updatesService.LoadStateAndUpdate(() => { });
            }

            //#if DEBUG
            await VoIPConnection.Current.ConnectAsync();
            //#endif

            base.OnResuming(s, e, previousExecutionState);
        }

        public override Task OnSuspendingAsync(object s, SuspendingEventArgs e, bool prelaunchActivated)
        {
            Log.Write("OnSuspendingAsync");

            var cacheService = UnigramContainer.Current.ResolveType<ICacheService>();
            if (cacheService != null)
            {
                cacheService.TryCommit();
            }

            var updatesService = UnigramContainer.Current.ResolveType<IUpdatesService>();
            if (updatesService != null)
            {
                updatesService.SaveState();
                updatesService.CancelUpdating();
            }

            return base.OnSuspendingAsync(s, e, prelaunchActivated);
        }

        // Methods
        private void ShowStatusBar()
        {
            // Show StatusBar on Win10 Mobile, in theme of the pass
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var statusBar = StatusBar.GetForCurrentView();

                var bgcolor = Application.Current.Resources["TelegramBackgroundTitlebarBrush"] as SolidColorBrush;

                // Background
                statusBar.BackgroundColor = bgcolor.Color;
                statusBar.BackgroundOpacity = 1;

                // Branding colour
                //statusBar.BackgroundColor = Color.FromArgb(255, 54, 173, 225);
                //statusBar.ForegroundColor = Colors.White;
                //statusBar.BackgroundOpacity = 1;
            }
        }

        private void ColourTitleBar()
        {
            try
            {
                //Window.Current.Activated -= Window_Activated;
                //Window.Current.Activated += Window_Activated;

                // Changes to the titlebar (colour, and such)
                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;

                var titlebar = ApplicationView.GetForCurrentView().TitleBar;
                var backgroundBrush = Application.Current.Resources["TelegramBackgroundTitlebarBrush"] as SolidColorBrush;
                var foregroundBrush = Application.Current.Resources["SystemControlForegroundBaseHighBrush"] as SolidColorBrush;

                titlebar.BackgroundColor = backgroundBrush.Color;
                titlebar.ForegroundColor = foregroundBrush.Color;
                titlebar.ButtonBackgroundColor = backgroundBrush.Color;
                titlebar.ButtonForegroundColor = foregroundBrush.Color;

                //// Accent Color
                //var accentBrush = Application.Current.Resources["SystemControlHighlightAccentBrush"] as SolidColorBrush;
                //var titleBrush = Application.Current.Resources["TelegramBackgroundTitlebarBrush"] as SolidColorBrush;
                //var subtitleBrush = Application.Current.Resources["TelegramBackgroundSubtitleBarBrush"] as SolidColorBrush;

                //// Foreground
                //titlebar.ButtonForegroundColor = Colors.White;
                //titlebar.ButtonHoverForegroundColor = Colors.White;
                //titlebar.ButtonInactiveForegroundColor = Colors.LightGray;
                //titlebar.ButtonPressedForegroundColor = Colors.White;
                //titlebar.ForegroundColor = Colors.White;
                //titlebar.InactiveForegroundColor = Colors.LightGray;

                //// Background
                //titlebar.BackgroundColor = titleBrush.Color;
                //titlebar.ButtonBackgroundColor = titleBrush.Color;

                //titlebar.InactiveBackgroundColor = subtitleBrush.Color;
                //titlebar.ButtonInactiveBackgroundColor = subtitleBrush.Color;

                //titlebar.ButtonHoverBackgroundColor = Helpers.ColorsHelper.ChangeShade(titleBrush.Color, -0.06f);
                //titlebar.ButtonPressedBackgroundColor = Helpers.ColorsHelper.ChangeShade(titleBrush.Color, -0.09f);

                //// Branding colours
                ////titlebar.BackgroundColor = Color.FromArgb(255, 54, 173, 225);
                ////titlebar.ButtonBackgroundColor = Color.FromArgb(255, 54, 173, 225);
                ////titlebar.ButtonHoverBackgroundColor = Color.FromArgb(255, 69, 179, 227);
                ////titlebar.ButtonPressedBackgroundColor = Color.FromArgb(255, 84, 185, 229);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Device does not have a Titlebar");
            }
        }

        //private void Window_Activated(object sender, WindowActivatedEventArgs e)
        //{
        //    ((SolidColorBrush)Resources["TelegramBackgroundTitlebarBrush"]).Color = e.WindowActivationState != CoreWindowActivationState.Deactivated ? ((SolidColorBrush)Resources["TelegramBackgroundTitlebarBrushBase"]).Color : ((SolidColorBrush)Resources["TelegramBackgroundTitlebarBrushDeactivated"]).Color;
        //}

        public static void RaiseThemeChanged()
        {
            var frame = Window.Current.Content as Frame;
            if (frame != null)
            {
                var dark = (bool)App.Current.Resources["IsDarkTheme"];

                frame.RequestedTheme = dark ? ElementTheme.Light : ElementTheme.Dark;
                frame.RequestedTheme = ElementTheme.Default;
            }
        }
    }

    public class AppInMemoryState
    {
        public IEnumerable<TLMessage> ForwardMessages { get; set; }

        public TLKeyboardButtonSwitchInline SwitchInline { get; set; }
        public TLUser SwitchInlineBot { get; set; }

        public string SendMessage { get; set; }
        public bool SendMessageUrl { get; set; }

        public int? NavigateToMessage { get; set; }
        public string NavigateToAccessToken { get; set; }
    }
}

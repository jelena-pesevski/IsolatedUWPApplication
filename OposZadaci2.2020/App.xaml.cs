using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace OposZadaci2._2020
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        internal OposZadaci2._2020.JobManager JobManager { get; private set; }
      
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            JobManager = await OposZadaci2._2020.JobManager.Load();
            UnregisterPreviousBackgroundTasks();

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                // Register a handler for BackRequested events and set the
                // visibility of the Back button
                SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
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
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            if (JobManager != null)
                await JobManager.Save();
            var taskRegistered = false;
            var taskName = "My Background Trigger";

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    taskRegistered = true;
                    break;
                }
            }
            if (taskRegistered == false)
            {
                var builder = new BackgroundTaskBuilder();
                builder.Name =taskName;
                builder.SetTrigger(new TimeTrigger(15, true));
                BackgroundTaskRegistration task = builder.Register();

            }  
            deferral.Complete();
        }

        private void UnregisterPreviousBackgroundTasks()
        {
            var tasks = BackgroundTaskRegistration.AllTasks;
            foreach (var task in tasks)
            {
                task.Value.Unregister(true);
            }
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame.CanGoBack)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        protected async override void OnFileActivated(FileActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                JobManager = await OposZadaci2._2020.JobManager.Load();
                UnregisterPreviousBackgroundTasks();
               
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

             
                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

            }
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), args.Files);
                
            }
            // Ensure the current window is active


            Window.Current.Activate();

        }

        protected async override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
          
            base.OnBackgroundActivated(args);
            IBackgroundTaskInstance taskInstance = args.TaskInstance;
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            await RunBackgroundWork();
            deferral.Complete();
        }

        private async Task RunBackgroundWork()
        {

            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("jobs.xml");
            XElement xml;
            using (Stream stream = await file.OpenStreamForReadAsync())
                xml = XElement.Load(stream);

            List<Job> jobs = xml.Elements().Select(x => new Job(int.Parse(x.Attribute("numOfCores").Value), x.Attribute("filename").Value)).ToList();
            if (jobs.Count != 0)
            {
                await NotificationManager.NotifyUserFromBackground("There are some unfinished jobs.");
            }
        }
    }
}

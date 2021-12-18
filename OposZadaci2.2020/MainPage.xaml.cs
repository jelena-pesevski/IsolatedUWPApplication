using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace OposZadaci2._2020
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            jobManager = (Application.Current as App).JobManager;
        }

        readonly JobManager jobManager;

        public delegate void RunJobsClickedDelegate();

        public event RunJobsClickedDelegate RunJobsClicked;

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
       
            if (e.Parameter is IReadOnlyList<IStorageItem>)
            {
             
                IStorageFile file = (IStorageFile)(((IReadOnlyList<IStorageItem>)e.Parameter)[0]);
             //    await Task.Run(() => jobManager.ReadJobFromFile(file));
            //     jobManager.ReadJobFromFile(file);
                
                XElement xml;
                using (Stream stream = await file.OpenStreamForReadAsync())
                    xml = XElement.Load(stream);

                await Task.Run(() => jobManager.LoadJobFromFile(xml));         

            }
            await InitializeStackPanel(jobManager.Jobs);
        }

        private async Task InitializeStackPanel(IReadOnlyList<Job> jobs)
        {
            foreach (Job job in jobs)
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    JobProgressControl jobProgressControl = new JobProgressControl(job);         
                    jobProgressControl.JobPaused += JobProgressControl_JobPaused;
                    jobProgressControl.JobResumed += JobProgressControl_JobResumed;
                    jobProgressControl.JobCancelled += JobProgressControl_JobCancelled;     
                    jobProgressControl.JobRemoved += JobProgressControl_JobRemoved;
                    RunJobsClicked += jobProgressControl.RunJobsClicked_AssignNumberOfThreads;
                    JobsStackPanel.Children.Add(jobProgressControl);
                });
        }

        private async void JobProgressControl_JobRemoved(Job job, object sender)
        {
            await RemoveJob(job, sender as JobProgressControl);
        }

      
        private async void JobProgressControl_JobPaused(Job job, object sender) => await job.Pause();

        private async void JobProgressControl_JobCancelled(Job job, object sender)
        {
            await job.Cancel();
            await RemoveJob(job, sender as JobProgressControl);
            await NotificationManager.NotifyUser("Job is cancelled.", job);
        }
 

        private async void JobProgressControl_JobResumed(Job job, object sender) => await job.Resume();

        private async Task RemoveJob(Job job, JobProgressControl jobProgressControl)
        {
            if (!job.IsFinished)
                await job.Cancel(true);
            jobManager.RemoveJob(job);
            if (jobProgressControl != null)
                JobsStackPanel.Children.Remove(jobProgressControl);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".bmp");

            var files = await picker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {               
                Job[] jobs = await Task.Run(()=>jobManager.AddJobs(files));  //treba da odabrane fajlove pretvorim u jobove
                await InitializeStackPanel(jobs);
            }
        }

        private void ParallelJobsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            return;
        }
   
        private async void RunJobs_Click(object sender, RoutedEventArgs e)
        {
            RunJobsClicked?.Invoke();
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                jobManager.DestinationFolder = folder;

                await Task.Run(()=>jobManager.SetDestFolders());
                await jobManager.RunJobs();
            }
           
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            int newValue;
            if (int.TryParse(ParallelJobsTextBox.Text, out newValue))
            {
                if (jobManager.MaxConcurrentJobs != newValue)
                    jobManager.MaxConcurrentJobs = newValue;
            }
        }

        private async void SaveFiles_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");
            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                jobManager.FolderForMYTFiles = folder;
               await jobManager.SaveMYTFiles();
            }

        }
    }
}

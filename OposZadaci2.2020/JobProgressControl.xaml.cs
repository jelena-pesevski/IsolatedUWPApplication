using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OposZadaci2._2020
{
    public sealed partial class JobProgressControl : UserControl
    {
        public delegate void JobActionCompletedDelegate(Job job, object sender);

        public Job Job { get; private set; }

        public event JobActionCompletedDelegate JobCancelled;
        public event JobActionCompletedDelegate JobPaused;

        public event JobActionCompletedDelegate JobCompleted;
        public event JobActionCompletedDelegate JobRemoved;
        public event JobActionCompletedDelegate JobResumed;

        public JobProgressControl(Job job)
        {
            this.InitializeComponent();

            Job = job;

            UpdateControlVisibility();

            JobTitleTextBlock.Text = job.Filename;

            job.ProgressChanged += Job_ProgressChanged;
        }

        //DA LI MENI UOPSTE TREBA START BUTTON KOD SVAKOG JOBA-ne
        private void UpdateControlVisibility()
        {
            //ovo se ne vidi dok je pending
            JobProgressBar.Visibility = CancelButton.Visibility = PauseButton.Visibility =ResumeButton.Visibility= (!(Job.IsFinished || Job.IsPending)).ToVisibility();
          //  StartButton.Visibility = (Job.IsFinished || Job.IsPending).ToVisibility();
            NumberOfThreads.Visibility = Job.IsPending.ToVisibility();

            CancelButton.IsEnabled = (Job.CurrentState != Job.JobState.Cancelling && Job.CurrentState != Job.JobState.Cancelled);
          //  PauseButton.IsEnabled = (Job.CurrentState != Job.JobState.Pausing && Job.CurrentState != Job.JobState.Paused);
          //  ResumeButton.IsEnabled = Job.CurrentState == Job.JobState.Paused;
        }

        private async void Job_ProgressChanged(double progress, Job.JobState jobState)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (!(progress == 100.0)) 
                    JobProgressBar.Value = JobProgressBar.Value + progress;
                else if(progress<0)
                    JobProgressBar.Value = 0.0;
                else
                    JobProgressBar.Value = 100.0;

                UpdateControlVisibility();
                
                if (jobState == Job.JobState.Done)
                    JobCompleted?.Invoke(Job, this);
            });
        }

        //handler za RunJobs
        internal void RunJobsClicked_AssignNumberOfThreads()
        {
            if (Job.IsPending && !string.IsNullOrWhiteSpace(NumberOfThreads.Text)) Job.NumberOfCores = int.Parse(NumberOfThreads.Text);
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e) => JobRemoved?.Invoke(Job, this);

        private void CancelButton_Click(object sender, RoutedEventArgs e) => JobCancelled?.Invoke(Job, this);

        private void PauseButton_Click(object sender, RoutedEventArgs e) => JobPaused?.Invoke(Job, this);

        private void ResumeButton_Click(object sender, RoutedEventArgs e) => JobResumed?.Invoke(Job, this);

        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e) => FlyoutBase.ShowAttachedFlyout(sender as Grid);


        //prazan handler
        private void NumberOfThreads_TextChanged(object sender, TextChangedEventArgs e)
        { }
    }
}

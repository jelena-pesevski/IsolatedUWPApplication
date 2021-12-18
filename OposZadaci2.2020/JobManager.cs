using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace OposZadaci2._2020
{
    public class JobManager
    {
   
        public StorageFolder DestinationFolder { get; internal set; }
        public StorageFolder FolderForMYTFiles { get; internal set; }

        public int MaxConcurrentJobs { get; set; } = 4;

        private readonly List<Job> jobs;

        public IReadOnlyList<Job> Jobs => jobs;

        private JobManager(List<Job> jobs)
        {
            this.jobs = jobs;
            foreach(Job job in jobs)
                job.ProgressChanged += Job_ProgressChanged;
        }

        private JobManager() {
            this.jobs = new List<Job>();
        }
 

        public void RemoveJob(Job job) => jobs.Remove(job);

        private async void Job_ProgressChanged(double progress, Job.JobState jobState)
        {
            if (jobState == Job.JobState.Cancelled || jobState == Job.JobState.Error || jobState == Job.JobState.Done)
                await RunJobs();
        }

        public Job[] AddJobs(IReadOnlyList<Windows.Storage.StorageFile> files)
        {
            List<Job> jobs = new List<Job>();
            foreach (var file in files)
            {
                //cuvam destination file i naziv putanje
                Job job = new Job(file, file.Path);

                job.ProgressChanged += Job_ProgressChanged;

                this.jobs.Add(job);
                jobs.Add(job);
            }
            return jobs.ToArray();
        }

       
        public async Task RunJobs()
        {
            int currentActiveJobs = jobs.Count(x => x.CurrentState == Job.JobState.Processing);
            if (currentActiveJobs < MaxConcurrentJobs)
            {
                List<Job> pendingJobs = jobs.Where(x => x.CurrentState == Job.JobState.Pending).Take(MaxConcurrentJobs - currentActiveJobs).ToList();
                foreach (Job job in pendingJobs)
                    await job.Start(true);
            }
        }

        internal async void LoadJobFromFile(XElement xml)
        {
            var fal = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            Job jobFromFile = new Job(int.Parse(xml.Attribute("numOfCores").Value), xml.Attribute("filename").Value);
                      
            foreach (Windows.Storage.AccessCache.AccessListEntry entry in fal.Entries)
            {
                string token = entry.Token;
                string pathName = entry.Metadata;
                if (pathName.Equals(jobFromFile.Filename))
                {
                    jobFromFile.sourceFile = await fal.GetFileAsync(token);
                    break;
                }
            }
            jobFromFile.ProgressChanged += Job_ProgressChanged;
            jobs.Add(jobFromFile);
        }


        private static async Task<StorageFile> GetSerializationFileToSave() => await ApplicationData.Current.LocalFolder.CreateFileAsync("jobs.xml", CreationCollisionOption.ReplaceExisting);
        private static async Task<StorageFile> GetSerializationFile() => await ApplicationData.Current.LocalFolder.CreateFileAsync("jobs.xml", CreationCollisionOption.OpenIfExists);

        public async Task Save()
        {

            XElement xml = new XElement(nameof(JobManager), jobs.Where(x => !x.IsFinished).ToList().Select(x => x.GetParameters()).Select(x => new XElement(nameof(Job), new XAttribute("numOfCores", (int)x.numOfCores),new XAttribute("filename", x.filename))));

            //sacuvaj pravo pristupa za neobradjene jobove
            var fal = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            jobs.Where(x=>!x.IsFinished).ToList().ForEach(x => fal.Add(x.sourceFile, x.Filename));
     
            //uvijek cuvaj u novi
            StorageFile file = await GetSerializationFileToSave();

            using (Stream stream = await file.OpenStreamForWriteAsync())
                xml.Save(stream);
        }

        public static async Task<JobManager> Load()
        {
            try
            {  
                var fal = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
                StorageFile file = await GetSerializationFile();

                XElement xml;
                using (Stream stream = await file.OpenStreamForReadAsync())
                    xml = XElement.Load(stream);

                List<Job> jobs = xml.Elements().Select(x => new Job(int.Parse(x.Attribute("numOfCores").Value), x.Attribute("filename").Value)).ToList();

                foreach (Windows.Storage.AccessCache.AccessListEntry entry in fal.Entries)
                {
                    string token = entry.Token;
                    string pathName = entry.Metadata;

                    jobs.Where(x => x.Filename.Equals(pathName)).ToList().ForEach(async (x) => x.sourceFile = await fal.GetFileAsync(token));
                }
                

                return new JobManager(jobs);
            }
            catch
            {
                return new JobManager();
            }
        }

        public void SetDestFolders()
        {
            jobs.ForEach(x => x.destinationFolder = DestinationFolder);
        }

        internal async Task ReadJobFromFile(IStorageFile file)
        {
            XElement xml;
            using (Stream stream = await file.OpenStreamForReadAsync())
                xml = XElement.Load(stream);
            var fal = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            Job jobFromFile = new Job(int.Parse(xml.Attribute("numOfCores").Value), xml.Attribute("filename").Value);
            foreach (Windows.Storage.AccessCache.AccessListEntry entry in fal.Entries)
            {
                string token = entry.Token;
                string pathName = entry.Metadata;
                if (pathName.Equals(jobFromFile.Filename))
                {
                    jobFromFile.sourceFile = await fal.GetFileAsync(token);
                    break;
                }
            }
            jobs.Add(jobFromFile);
        }

        internal async Task SaveMYTFiles()
        {
            var fal = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            foreach (Job job in jobs)
            {
                StorageFile destination = await FolderForMYTFiles.CreateFileAsync(job.sourceFile.Name+".myt", CreationCollisionOption.ReplaceExisting);
                XElement xml = new XElement(nameof(Job), new XAttribute("numOfCores", (int)job.NumberOfCores), new XAttribute("filename", job.Filename));
 
                fal.Add(job.sourceFile, job.Filename);

                using (Stream stream2 = await destination.OpenStreamForWriteAsync())
                    xml.Save(stream2);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace OposZadaci2._2020
{
    public class Job
    {
        public enum JobState { Pending, Processing, Pausing, Paused, Resuming, Cancelling, Cancelled, Error, Done };

        public delegate void ProgressReportedDelegate(double progress, JobState jobState);

        public event ProgressReportedDelegate ProgressChanged;

        private Task processingTask;
        private CancellationTokenSource cancellationTokenSource;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim pauseSemaphore = new SemaphoreSlim(1);
      
        internal StorageFile sourceFile;
        internal StorageFolder destinationFolder;

        public int NumberOfCores { get;  set; } = 2; //ako se nista ne dodijeli je 2
        public JobState CurrentState { get; private set; } = JobState.Pending;
        public bool IsFinished => CurrentState == JobState.Done || CurrentState == JobState.Error || CurrentState == JobState.Cancelled || CurrentState==JobState.Cancelling;
        public bool IsPending => CurrentState == JobState.Pending;
        public string Filename { get; private set; }

        public Job(StorageFile destinationFile, string filename) => (this.sourceFile, Filename) = (destinationFile, filename);
        public Job(int numOfCores, StorageFile destinationFile, string filename) => (NumberOfCores, this.sourceFile, Filename) = (numOfCores, destinationFile, filename);
        public Job(int numOfCores, string filename) => (NumberOfCores, Filename) = (numOfCores, filename);

        private async Task ProcessImage(CancellationToken cancellationToken)
        {
            try { 
                CurrentState = JobState.Processing;
                ProgressChanged?.Invoke(0, CurrentState);

                byte[] data;
                int fileWidth;
                int fileHeight;

                var handle = sourceFile.CreateSafeFileHandle(options: FileOptions.RandomAccess);

                using (var filein = new BinaryReader(new FileStream(handle, FileAccess.Read)))
                {
                    var Signature1 = filein.ReadByte(); // now at 0x1
                    var Signature2 = filein.ReadByte(); // now at 0x2
                    if (Signature1 != 66 || Signature2 != 77) // Must be BM
                    {
                        await NotificationManager.NotifyUser("Chosen image is not in .bmp format.", this);
                        return;
                    }

                    filein.ReadDouble(); // skip next 8 bytes now at position     
                    var Offset = filein.ReadInt32(); // offset in file                   
                    filein.ReadInt32(); // now at 0x12a
                    fileWidth = filein.ReadInt32(); // now at 0x16
                    fileHeight = filein.ReadInt32(); // now at 0x1a

                    filein.ReadBytes(Offset - 0x1a); //sada je na offsetu
                    data = filein.ReadBytes((int)filein.BaseStream.Length - Offset);
                }

                byte[] destPixels = new byte[4 * fileWidth * fileHeight];
                int interval = fileHeight / 5;  //uvecavanje za svakih 20%
               
                SemaphoreSlim cancelSemaphore = new SemaphoreSlim(1);
                SemaphoreSlim semaphoreForStopping = new SemaphoreSlim(1);
                SemaphoreSlim semaphoreForResuming = new SemaphoreSlim(1);

                Parallel.For(0, fileHeight, new ParallelOptions { MaxDegreeOfParallelism = NumberOfCores }, async (y, state) =>
                {
                     if (y % 100 == 0)  Task.Delay(5).Wait();
                    for (int x = 0; x < fileWidth; x++)
                    {
                     
                   
                        int b = data[(x + y * fileWidth) * 4];
                        int g = data[(x + y * fileWidth) * 4 + 1];
                        int r = data[(x + y * fileWidth) * 4 + 2];
                        int a = data[(x + y * fileWidth) * 4 + 3];

                        b = b * a / 255;
                        g = g * a / 255;
                        r = r * a / 255;

                        if ((b + g + r) / 3 < 200)
                        {
                            destPixels[(x + y * fileWidth) * 4] = (byte)(((b + g + r) / 3) / 4);     // B
                            destPixels[(x + y * fileWidth) * 4 + 1] = (byte)(((b + g + r) / 3) / 4); // G
                            destPixels[(x + y * fileWidth) * 4 + 2] = (byte)(((b + g + r) / 3) / 4); // R
                            destPixels[(x + y * fileWidth) * 4 + 3] = 255; // A
                        }
                        else
                        {
                            destPixels[(x + y * fileWidth) * 4] = 255;     // B
                            destPixels[(x + y * fileWidth) * 4 + 1] = 255; // G
                            destPixels[(x + y * fileWidth) * 4 + 2] = 255; // R
                            destPixels[(x + y * fileWidth) * 4 + 3] = 255; // A
                        }
                    }
                            
                    if (CurrentState == JobState.Pausing)
                    {
                        CurrentState = JobState.Paused;
                        ProgressChanged?.Invoke(0, CurrentState);

                        semaphoreForStopping.Wait();
                        await NotificationManager.NotifyUser("Paused", this);
                        await pauseSemaphore.WaitAsync();
                        pauseSemaphore.Release();
                        await NotificationManager.NotifyUser("Resumed", this);
                        CurrentState = JobState.Processing;
                        semaphoreForStopping.Release();
                    }

                    if (CurrentState == JobState.Paused)
                    {                       
                        semaphoreForStopping.Wait();
                        semaphoreForStopping.Release();           
                    }

                    if (state.ShouldExitCurrentIteration)
                    {
                        if (state.LowestBreakIteration < y)
                            return;
                    }

                    cancelSemaphore.Wait();
                    if (CurrentState == JobState.Cancelling)
                    {
                        CurrentState = JobState.Cancelled;
                        state.Break();
                        cancelSemaphore.Release();
                        return;
                    }
                    cancelSemaphore.Release();

                    if (y % interval == 0)
                    {
                        ProgressChanged?.Invoke(20.0, CurrentState);
                    }
                });

                if (CurrentState != JobState.Cancelling && CurrentState != JobState.Cancelled)
                {
                    await WriteBmp32(fileWidth, fileHeight, destPixels);
                    CurrentState = JobState.Done;
                    ProgressChanged?.Invoke(100.0, CurrentState);
                }
                else
                {
                    CurrentState = JobState.Cancelled;
                    ProgressChanged?.Invoke(-100.0, CurrentState);            
                }

            }
            catch
            {
                CurrentState = JobState.Error;
                ProgressChanged?.Invoke(-100.0, CurrentState);
            }
        }

        public async Task WriteBmp32(int width, int height, byte[] pixels)
        {
            StorageFile destination = await destinationFolder.CreateFileAsync(sourceFile.Name + "New.bmp",CreationCollisionOption.ReplaceExisting);
            var handle = destination.CreateSafeFileHandle(options: FileOptions.RandomAccess);

            int size = pixels.Length, pom2 = 0;
            short pom = 0x4d42;
            if (height % 4 == 0) size *= 3;
            else size *= 4;

            using (BinaryWriter writer = new BinaryWriter(new FileStream(handle, FileAccess.ReadWrite)))
            {
                writer.Write((Int16)pom);
                size += 122;
                writer.Write((Int32)size);
                writer.Write((Int32)pom2);
                pom2 = 122;
                writer.Write((Int32)pom2); // offset_piksela, broj bajtova ukupno u oba zaglavlja
                pom2 = 108;
                writer.Write((Int32)pom2); // velicina DIB zaglavlja
                writer.Write((Int32)width);// sirina
                writer.Write((Int32)height); // visina
                pom = 1;
                writer.Write((Int16)pom); // karatna boja, nije bitno
                pom = 32;
                writer.Write((Int16)pom); // broj bita po pikselu
                pom2 = 3;
                writer.Write((Int32)pom2); // kompresija
                size -= 122;
                writer.Write((Int32)pom2); // velicina piksela
                pom2 = 2835;
                writer.Write((Int32)pom2);
                writer.Write((Int32)pom2);
                pom2 = 0;
                writer.Write((Int32)pom2);
                writer.Write((Int32)pom2);
                pom2 = 0x00FF0000;
                writer.Write((Int32)pom2); // crveni maska
                pom2 = 0x0000FF00;
                writer.Write((Int32)pom2);// zeleni maska
                pom2 = 0x000000FF;
                writer.Write((Int32)pom2); // alfa maska
                pom2 = unchecked((int)0xFF000000);
                writer.Write((Int32)pom2); // alfa maska
                pom2 = 0x57696e20;
                writer.Write((Int32)pom2); //nesto ya win
                pom2 = 0;
                for (int i = 0; i < 12; i++)
                {
                    writer.Write((Int32)pom2); // nesto za win
                }
                foreach (var b in pixels)
                {
                    writer.Write(b);
                }
            }
        }

        public async Task Start(bool silent = false)
        {
            await semaphore.WaitAsync();
            try
            {
                if (CurrentState == JobState.Pending)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    processingTask = Task.Factory.StartNew(async () => await ProcessImage(cancellationTokenSource.Token), cancellationTokenSource.Token);
                }
                else if (!silent)
                    await NotificationManager.NotifyUser("The job is already started.", this);          
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task Cancel(bool silent = false)
        {
            await semaphore.WaitAsync();
            try
            {
                if (CurrentState == JobState.Pending)
                    CurrentState = JobState.Cancelled;
                else if (CurrentState == JobState.Processing || CurrentState == JobState.Pausing || CurrentState == JobState.Paused || CurrentState == JobState.Resuming)
                {
                    CurrentState = JobState.Cancelling;
                    cancellationTokenSource.Cancel();
                }
                else if (!silent)
                    await NotificationManager.NotifyUser("The job cannot be cancelled.", this);
            }
            finally
            {
                semaphore.Release();
            }

        }

        public async Task Pause()
        {
            await semaphore.WaitAsync();
            try
            {
                if (CurrentState == JobState.Processing)
                {
                    CurrentState = JobState.Pausing;
                    await pauseSemaphore.WaitAsync();
                }
                else
                    await NotificationManager.NotifyUser("Only a processing job can be paused.", this);
            }
            finally
            {
                semaphore.Release();
            }

        }

        public async Task Resume()
        {
            await semaphore.WaitAsync();
            try
            {
                if (CurrentState == JobState.Paused || CurrentState == JobState.Pausing)
                {
                    CurrentState = JobState.Resuming;
                    pauseSemaphore.Release();
                }
                else
                    await NotificationManager.NotifyUser("Only a paused job can be resumed.", this);
             
            }
            finally
            {
                semaphore.Release();
            }

        }

        public (int numOfCores, JobState state, string filename) GetParameters() => (NumberOfCores, CurrentState switch
        {
            JobState.Cancelling or JobState.Cancelled => JobState.Cancelled,
            JobState.Error or JobState.Done => CurrentState,
            _ => JobState.Pending
        }, Filename);
    }
}

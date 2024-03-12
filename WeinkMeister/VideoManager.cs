using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using System.Security.Cryptography;
using MediaInfoCore = MediaInfo;
using SearchOption = System.IO.SearchOption;
using MediaInfo;
using Konsole;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using RandomVideo.Classes;
using System.Windows;
//using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace RandomVideo
{
    public class VideoManager
    {
        public string vlcPath;

        public ConfigurationManager configManager;
        public string configFile = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
        public bool isFullScreen = false;
        public List<VideoInfo> videoList = new List<VideoInfo>();
        public List<string> ignoreList;
        public string randomVideoName = "";
        const byte VK_Q = (byte)'Q';
        const byte VK_A = (byte)'A';
        const byte VK_W = (byte)'W';
        const byte VK_Z = (byte)'Z';
        private static IntPtr display;
        private static IntPtr rootWindow;
        const int VK_CTRL = 0x11;
        Process CurrentProcess;

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern short GetAsyncKeyState(int virtualKeyCode);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetKeyboardState(byte[] keystate);

        public VideoManager()
        {
            vlcPath = FindVLCPath();
            LoadConfig();
        }

        public async Task LaunchMenu()
        {
            string welcomeMessage = "";
            ignoreList = JsonManager.ReadList(configManager.config.IgnoreFile);
            videoList = JsonManager.ReadVideoInfo(configManager.config.VideoFile);
            welcomeMessage =
                "Basic usage while the video is open:\n" +
                "CTRL + A: Add to Favourites list\n" +
                "CTRL + Z: Add to Best list\n" +
                "CTRL + W: Add to Ignore list. These videos \n" +
                "CTRL + Q: Close the video and return to menu\n" +
                "Q: Close the video and open a new random one\n" +
                "Please select what you want to be played"
                ;
            if (videoList.Count == 0)
            {
                welcomeMessage = "It looks like the list of videos is empty. Please select \"Remake list / Initial config\" in the menu below to make it";
            }



            string[] menuOptions = { "Launch a random video", "From favourites", "From best", "Update list of videos", "Remake list / Initial config", "Exit" };
            ConsoleMenu consoleMenu = new ConsoleMenu(welcomeMessage, menuOptions);

            int selectedOptionIndex = consoleMenu.ShowMenu();
            string selectedOption = menuOptions[selectedOptionIndex];


            switch (selectedOptionIndex)
            {
                case 0:
                    launch_video_loop();
                    break;
                case 1:
                    var favList = JsonManager.ReadList(configManager.config.FavFile);
                    //favList = favList.Select(x => x.Replace(Path.DirectorySeparatorChar, '/')).ToList();
                    //JsonManager.Write(configManager.config.FavFile, favList);
                    var favVideos = MakeInfoListFromString(favList);
                    launch_video_loop(favVideos);
                    break;
                case 2:
                    var bestList = JsonManager.ReadList(configManager.config.BestFile);
                    //bestList = bestList.Select(x => x.Replace(Path.DirectorySeparatorChar, '/')).ToList();
                    //JsonManager.Write(configManager.config.BestFile, bestList);
                    var bestVideos = MakeInfoListFromString(bestList);
                    launch_video_loop(bestVideos);
                    break;
                case 3:
                    Console.WriteLine("Updating list. This may take a minute or two...");
                    UpdateList();
                    LaunchMenu();
                    break;
                case 4:
                    Console.WriteLine("This process may take a long time. Are you sure? (Y / Yes) (N / No): ");
                    var ans = Console.ReadLine();
                    if (ans != null && (string.Equals(ans.ToLower(), "yes") || string.Equals(ans.ToLower(), "y")))
                    {
                        RemakeList();
                    }
                    LaunchMenu();
                    break;

                default:
                    Console.WriteLine("No option selected.");
                    Environment.Exit(0);
                    break;
            }
        }

        public List<VideoInfo> MakeInfoListFromString(List<string> list)
        {
            List<VideoInfo> newList = new List<VideoInfo>();
            int sum = 0;
            foreach (var item in list)
            {
                var normalizedItemPath = Path.GetFullPath(item);
                string regexPattern = Regex.Escape(item).Replace(@"\?", ".");
                var vid = videoList.Find(x => Regex.IsMatch(x.VideoPath, regexPattern, RegexOptions.IgnoreCase));

                //var video = videoList.Find(x => Path.GetFullPath(x.VideoPath).Equals(normalizedItemPath, StringComparison.OrdinalIgnoreCase));
                if (vid != null)
                {
                    VideoInfo video = new VideoInfo();
                    video.VideoPath = vid.VideoPath;
                    video.Duration = vid.Duration;
                    sum += video.Duration;
                    video.DurationSum = sum;
                    newList.Add(video);
                }
                else
                {
                    Console.WriteLine($"Could not find video {item}");
                }
            }
            return newList;
        }
        public void LoadConfig()
        {
            configManager = new ConfigurationManager(configFile);
            configManager.LoadConfig();
            
        }
        public void SaveConfig()
        {
            configManager.SaveConfig();
        }

        private async Task GetVideoDurationAndAdd(string filePath)
        {
            try
            {
                var ts = TimeSpan.Zero;
                var _mediaInfo = new MediaInfo.MediaInfo();
                _mediaInfo.Open(filePath);
                string durationString = _mediaInfo.Get(StreamKind.General, 0, "Duration");
                _mediaInfo.Close();
                double durationMilliseconds;

                if (!string.IsNullOrEmpty(durationString))
                {
                    if (double.TryParse(durationString, out durationMilliseconds))
                    {
                        ts = TimeSpan.FromMilliseconds(durationMilliseconds);
                    }
                }

                int duration = (int)Math.Round((double)ts.TotalMinutes);

                if (duration >= 0)
                {
                    if (duration == 0)
                    {
                        duration = 1;
                    }
                    //int duration = (int)Math.Round((double)dur?.TotalMinutes);

                    Console.WriteLine($"Video: {Path.GetFileName(filePath)}, Duration: {duration}");


                    videoList.Add(new VideoInfo
                    {
                        VideoPath = filePath,
                        Duration = duration,
                    });
                }
                else
                {
                    Console.WriteLine(filePath);
                    Console.WriteLine($"  Cant get duration of video");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(filePath);
                Console.WriteLine($"  An error occurred: {ex.Message}");
                throw;
            }
        }

        public async Task MakeVideoListFromList(List<string> videoFiles)
        {
            if (configManager.config.RType == RandomTypeEnum.Duration)
            {
                int completedTasks = 0;
                int totalTasks = videoFiles.Count;
                var bars = new ConcurrentBag<ProgressBar>();
                var pb = new ProgressBar(PbStyle.DoubleLine, totalTasks);
                bars.Add(pb);
                await Task.WhenAll(videoFiles.Select(async filePath =>
                {
                    pb.Refresh(completedTasks, $"Video: {Path.GetFileName(filePath)}");
                    // Execute task asynchronously
                    await GetVideoDurationAndAdd(filePath);

                    // Increment completed tasks and report progress
                    completedTasks++;
                }));
            }
            else //configManager.config.RType == RandomTypeEnum.Random
            {
                foreach (var path in videoFiles)
                {
                    videoList.Add(new VideoInfo
                    {
                        VideoPath = path,
                        Duration = 1,
                    });
                }
            }

            int durationSum = 0;

            foreach (var videoInfo in videoList)
            {
                durationSum += videoInfo.Duration;
                videoInfo.DurationSum = durationSum;
            }
        }

        public static List<string> GetVideoList(List<string> folders)
        {
            string[] videoExtensions = { ".mp4", ".avi", ".mkv" };
            List<string> list = new List<string>();
            foreach (var folder in folders)
            {

                string[] videoFiles = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                                       .Where(file => videoExtensions.Contains(Path.GetExtension(file).ToLower()))
                                       .Select(path => path.Replace(Path.DirectorySeparatorChar, '/'))
                                       .ToArray();
                list.AddRange(videoFiles);
            }
            return list;
        }

        public async Task RemakeList()
        {

            List<string> videoFiles = GetVideoList(configManager.config.Folders);

            await MakeVideoListFromList(videoFiles);

            CSVManager.WriteCSV(configManager.config.VideoFile, videoList);
        }

        public async Task UpdateList()
        {
            List<string> videoFiles = GetVideoList(configManager.config.Folders);
            if (videoFiles.Count <= 0)
            {
                Console.WriteLine("The video file must be loaded and not empty");
                return;
            }
            else
            {
                foreach (var videoFile in videoFiles)
                {
                    if (!videoList.Any(x => Path.Equals(x.VideoPath, videoFile)))
                    {
                        Console.WriteLine($"We need to add {videoFile}");
                    }
                }

                foreach (var videoFile in videoList)
                {
                    if (!videoFiles.Any(x => Path.Equals(x, videoFile.VideoPath)))
                    {
                        Console.WriteLine($"We need to remove {videoFile}");
                    }
                }
            }
        }

        public string FindVLCPath()
        {
            string registryPath = @"SOFTWARE\VideoLAN\VLC";
            string vlcKey = "InstallDir";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
            {
                if (key != null)
                {
                    string installDir = key.GetValue(vlcKey)?.ToString();
                    if (!string.IsNullOrEmpty(installDir) && File.Exists(Path.Combine(installDir, "vlc.exe")))
                    {
                        return Path.Combine(installDir, "vlc.exe");
                    }
                }
            }

            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            string[] directories = pathEnv.Split(';');

            foreach (string directory in directories)
            {
                string vlcPath = Path.Combine(directory, "vlc.exe");
                if (File.Exists(vlcPath))
                {
                    return vlcPath;
                }
            }

            string[] possiblePaths = {
                @"C:\Program Files\VideoLAN\VLC\vlc.exe",
                @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe"
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            Console.WriteLine("VLC path not found.");
            return null;
        }
        public async void KillCurrentProcess(object? state)
        {
            if (CurrentProcess != null)
            {
                CurrentProcess.Kill();
            }
        }
        public void launch_video_loop(List<VideoInfo> currentList)
        {
            CurrentProcess = null;
            int p_pid = 0;
            Timer quitTimer = null;

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                while (true)
                {
                    bool qPressed = (GetAsyncKeyState(VK_Q) & 0x8000) != 0;
                    bool aPressed = (GetAsyncKeyState(VK_A) & 0x8000) != 0;
                    bool zPressed = (GetAsyncKeyState(VK_Z) & 0x8000) != 0;
                    bool wPressed = (GetAsyncKeyState(VK_W) & 0x8000) != 0;
                    bool ctrlPressed = (GetAsyncKeyState(VK_CTRL) & 0x8000) != 0;

                    if (qPressed)
                    {
                        if (quitTimer != null)
                        {
                            quitTimer = new Timer(new TimerCallback(KillCurrentProcess), null, 0, 300);
                        }
                    }
                    else
                    {
                        if (quitTimer != null)
                        {
                            quitTimer.Dispose();
                            quitTimer = null;
                        }
                    }
                    if (qPressed && ctrlPressed)
                    {
                        // Kill process and end script
                        CurrentProcess?.Kill();
                        LaunchMenu();
                        break;
                    }

                    if (aPressed && ctrlPressed)
                    {
                        AddToFavourites();
                    }

                    if (zPressed && ctrlPressed)
                    {
                        AddToBest();
                    }

                    if (wPressed && ctrlPressed)
                    {

                        AddToIgnore();
                        CurrentProcess?.Kill();
                    }

                    if (p_pid == 0 || (CurrentProcess != null && CurrentProcess.HasExited))
                    {
                        Thread.Sleep(500);
                        CurrentProcess = LaunchRandomVideo(currentList);
                        p_pid = CurrentProcess?.Id ?? 0;
                        Thread.Sleep(40);
                    }
                }
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                byte[] keys = new byte[32]; // Bit array for keyboard state
                NativeMethods.XQueryKeymap(display, keys);

                while (true)
                {
                    bool qPressed = IsKeyPressed(VK_Q, keys);
                    bool aPressed = IsKeyPressed(VK_A, keys);
                    bool zPressed = IsKeyPressed(VK_Z, keys);
                    bool wPressed = IsKeyPressed(VK_W, keys);
                    bool ctrlPressed = IsKeyPressed(VK_CTRL, keys);

                    if (qPressed)
                    {
                        if (quitTimer != null)
                        {
                            quitTimer = new Timer(new TimerCallback(KillCurrentProcess), null, 0, 300);
                        }
                    }
                    else
                    {
                        if (quitTimer != null)
                        {
                            quitTimer.Dispose();
                            quitTimer = null;
                        }
                    }

                    if (qPressed && ctrlPressed)
                    {
                        CurrentProcess?.Kill();
                        LaunchMenu();
                        break;
                    }

                    if (aPressed && ctrlPressed)
                    {
                        AddToFavourites();
                    }

                    if (zPressed && ctrlPressed)
                    {
                        AddToBest();
                    }

                    if (wPressed && ctrlPressed)
                    {

                        AddToIgnore();
                        CurrentProcess?.Kill();
                    }

                    if (p_pid == 0 || (CurrentProcess != null && CurrentProcess.HasExited))
                    {
                        Thread.Sleep(500);
                        CurrentProcess = LaunchRandomVideo();
                        p_pid = CurrentProcess?.Id ?? 0;
                        Thread.Sleep(40);
                    }
                }
            }
        }

        public void launch_video_loop()
        {
            launch_video_loop(videoList);
        }

        public void AddToBest()
        {
            AddToFile(configManager.config.BestFile);
            Console.WriteLine("Added to best: " + randomVideoName.ToString());
        }

        public void AddToFavourites()
        {
            AddToFile(configManager.config.FavFile);
            Console.WriteLine("Added to fav: " + randomVideoName.ToString());
        }

        public void AddToFile(string path)
        {
            //TODO: Manage empty files
            string fullVideoPath = randomVideoName.ToString();
            List<string> videos = JsonManager.ReadList(path);


            if (!videos.Any(line => line.Contains(fullVideoPath)))
            {
                videos.Add(fullVideoPath);
                JsonManager.Write(path, videos);
            }
            else
            {
                Console.WriteLine("Already Added!");
            }

            Thread.Sleep(500);
        }

        public void AddToIgnore()
        {
            string fullVideoPath = randomVideoName.ToString();

            if (ignoreList != null && !ignoreList.Any(line => line.Contains(fullVideoPath)))
            {
                ignoreList.Add(fullVideoPath);
                JsonManager.Write(configManager.config.IgnoreFile, ignoreList);
                Console.WriteLine("Added to ignore: " + fullVideoPath);
            }
            else
            {
                Console.WriteLine("Already Added!");
            }

            Thread.Sleep(500);
        }

        public VideoInfo SelectRandomVideo(List<VideoInfo> currentList)
        {

            if (currentList.Count == 0)
            {
                Console.WriteLine("No videos found.");
                throw new Exception();
            }

            // Get the total duration sum of all videos
            int totalDurationSum = currentList.Last().DurationSum;

            // Generate a random number between 0 and totalDurationSum - 1 using RNGCryptoServiceProvider
            byte[] randomNumberBytes = new byte[4]; // Using 4 bytes for simplicity
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumberBytes);
            }
            int randomNumber = Math.Abs(BitConverter.ToInt32(randomNumberBytes, 0)) % totalDurationSum;


            // Binary search to find the video corresponding to the random number
            int left = 0;
            int right = currentList.Count - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;

                if (currentList[mid].DurationSum <= randomNumber)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            // At this point, 'right' is the index of the selected video
            return currentList[right];
        }

        public Process LaunchRandomVideo(List<VideoInfo> currentList)
        {
            var selectedVideo = SelectRandomVideo(currentList);

            Console.WriteLine($"Launching random video: {selectedVideo.VideoPath}");


            if (vlcPath == null)
            {
                throw new Exception("VLC path is invalid or VLC is not installed.");
            }

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = vlcPath;
            psi.Arguments = "file:///" + Uri.EscapeDataString(selectedVideo.VideoPath); // Enclose path in quotes to handle spaces
            randomVideoName = selectedVideo.VideoPath;
            if ((bool)configManager.config.Fullscreen)
            {
                psi.Arguments += " --fullscreen"; // Add fullscreen argument
            }

            try
            {
                return Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while launching VLC: {ex.Message}");
                return null;
            }
        }
        private static bool IsKeyPressed(int keyCode, byte[] keys)
        {
            int byteIndex = keyCode / 8;
            int bitIndex = keyCode % 8;

            return (keys[byteIndex] & (1 << bitIndex)) != 0;
        }

        private static Process LaunchRandomVideo()
        {
            // Implement logic to launch random video
            return null; // Placeholder
        }
    }

    internal static class NativeMethods
    {
        [DllImport("libX11.so")]
        public static extern IntPtr XOpenDisplay(IntPtr display_name);

        [DllImport("libX11.so")]
        public static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport("libX11.so")]
        public static extern int XQueryKeymap(IntPtr display, byte[] keys);

        [DllImport("libX11.so")]
        public static extern int XCloseDisplay(IntPtr display);
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomVideo.Classes
{
    public class VideoInfo
    {
        public string VideoPath { get; set; }
        public int Duration { get; set; }
        public int DurationSum { get; set; }

        public VideoInfo() { }

        public VideoInfo(string videoPath, int duration)
        {
            VideoPath = videoPath;
            Duration = duration;
        }
        public VideoInfo(string videoPath, int duration, int durationSum)
        {
            VideoPath = videoPath;
            Duration = duration;
            DurationSum = durationSum;
        }
    }
}

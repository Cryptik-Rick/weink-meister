using System;
using System.Diagnostics;
using System.IO;

namespace RandomVideo
{
    class Program
    {

        static async Task Main(string[] args)
        {
            VideoManager videoManager = new VideoManager();
            videoManager.LaunchMenu();

        }
    }
}

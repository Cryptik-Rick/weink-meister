using RandomVideo.Classes;
using System;
using System.Collections.Generic;
using System.IO;

class CSVManager
{
    public static List<VideoInfo> ReadCSV(string filePath)
    {
        List<VideoInfo> videoList = new List<VideoInfo>();

        try
        {
            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(',');
                        string sum = parts[parts.Length - 1];
                        string sur = parts[parts.Length - 2];
                        
                        string path = string.Join(",", parts, 0, parts.Length - 2);

                        videoList.Add(new VideoInfo
                        {
                            VideoPath = path,
                            Duration = int.Parse(sur),
                            DurationSum = int.Parse(sum)
                        });
                    }
                }
            }
            else
            {
                Console.WriteLine($"CSV file not found: {filePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while reading CSV file: {ex.Message}");
        }

        return videoList;
    }

    public static List<string> ReadStringCSV(string filePath)
    {
        List<string> list = new List<string>();

        try
        {
            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        list.Add(line);
                    }
                }
            }
            else
            {
                Console.WriteLine($"CSV file not found: {filePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while reading CSV file: {ex.Message}");
        }

        return list;
    }

    public static void WriteCSV(string filePath, List<VideoInfo> videoList)
    {
        try
        {
            // Create a temporary file
            string tempFilePath = Path.Combine(Path.GetDirectoryName(filePath), Guid.NewGuid().ToString() + ".tmp");

            // Write to the temporary file
            using (StreamWriter writer = new StreamWriter(tempFilePath))
            {
                foreach (var video in videoList)
                {
                    writer.WriteLine($"{video.VideoPath},{video.Duration},{video.DurationSum}");
                }
            }
            if (File.Exists(filePath))
            {
                // Replace the original file with the temporary file
                File.Replace(tempFilePath, filePath, null);
            }
            else
            {
                File.Move(tempFilePath, filePath);
            }

            Console.WriteLine("CSV file saved successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while saving CSV file: {ex.Message}");
        }
    }

    public static void WriteCSV(string filePath, List<string> videoList)
    {
        try
        {
            // Create a temporary file
            string tempFilePath = Path.Combine(Path.GetDirectoryName(filePath), Guid.NewGuid().ToString() + ".tmp");

            // Write to the temporary file
            using (StreamWriter writer = new StreamWriter(tempFilePath))
            {
                foreach (var video in videoList)
                {
                    writer.WriteLine($"{video}");
                }
            }

            // Replace the original file with the temporary file
            File.Replace(tempFilePath, filePath, null);

            Console.WriteLine("CSV file saved successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while saving CSV file: {ex.Message}");
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using RandomVideo.Classes;

public static class JsonManager
{
    public static List<VideoInfo> ReadVideoInfo(string filePath)
    {
        List<VideoInfo> videoList = new List<VideoInfo>();

        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                videoList = JsonConvert.DeserializeObject<List<VideoInfo>>(json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while reading JSON file: {ex.Message}");
        }

        return videoList;
    }

    public static void Write(string filePath, List<VideoInfo> videoList)
    {
        try
        {
            string json = JsonConvert.SerializeObject(videoList, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while saving JSON file: {ex.Message}");
        }
    }

    public static void Write(string filePath, List<string> videoList)
    {
        try
        {
            string json = JsonConvert.SerializeObject(videoList, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while saving JSON file: {ex.Message}");
        }
    }

    public static List<string> ReadList(string filePath)
    {
        List<string> resultList = new List<string>();

        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                resultList = JsonConvert.DeserializeObject<List<string>>(json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while reading JSON file: {ex.Message}");
        }

        return resultList;
    }
}
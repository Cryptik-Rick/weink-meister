using System;
using System.Configuration;
using System.IO;
using Newtonsoft.Json;

public class ConfigurationManager
{
    private readonly string configFilePath;
    public Config config { get; set; }

    public ConfigurationManager(string configFilePath)
    {
        this.configFilePath = configFilePath;
    }

    public void LoadConfig()
    {
        if (File.Exists(configFilePath))
        {
            try
            {
                string json = File.ReadAllText(configFilePath);
                config = JsonConvert.DeserializeObject<Config>(json);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config file: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Config file not found.");
            throw new FileNotFoundException();
        }


        if(null == config.Folders || config.Folders.Count == 0)
        {
            Console.WriteLine("You must specify at least one folder.");
            throw new InvalidDataException();
        }

        if (null == config.WorkingFolder)
        {
            config.WorkingFolder = Directory.GetCurrentDirectory();
        }
        if (null == config.Fullscreen)
        {
            config.Fullscreen = true;
        }
    }

    public void SaveConfig()
    {
        try
        {
            string json = JsonConvert.SerializeObject(this.config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(configFilePath, json);
            Console.WriteLine("Config file saved successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config file: {ex.Message}");
        }
    }
}

public enum RandomTypeEnum
{
    Video = 0,
    Duration = 1
};

public class Config
{
    public List<string> Folders { get; set; }
    public string FavFile { get; set; }
    public string BestFile { get; set; }
    public string IgnoreFile { get; set; }
    public bool? Fullscreen { get; set; }
    public string VideoFile { get; set; }

    public RandomTypeEnum RType;
    public string RandomType
    {
        get
        {
            return RType.ToString();
        }
        set
        {
            string v = value.ToLower();
            if (string.Equals(v, "video"))
            {
                RType = RandomTypeEnum.Video;
            }
            else if (string.Equals(v, "duration"))
            {
                RType = RandomTypeEnum.Duration;
            }
        }
    }
    private string _workingFolder;
    public string WorkingFolder
    {
        get { return _workingFolder; }
        set
        {
            _workingFolder = value;
            FavFile = Path.Combine(_workingFolder, "Favourites.json");
            BestFile = Path.Combine(_workingFolder, "Best.json");
            IgnoreFile = Path.Combine(_workingFolder, "Ignore.json");
            VideoFile = Path.Combine(_workingFolder, "Videos.json");
        }
    }

    // Add other configuration properties as needed
}
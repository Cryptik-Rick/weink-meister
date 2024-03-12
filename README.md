# WeinkMeister

Simple .NET program to launch random videos in a loop. Once the video is closed, another one opens. Currently only supported on Windows 10.

Using the random by duration will give more weight to longer videos during the selection process.

Two lists of videos will be managed: Favourites and Best. Once a video is playing, you can add it to either lists through keyboard shortcuts.

## Dependencies
Requires an installation of VLC: It will look for your VLC installation in the path, the registry and in the following folders: "C:\Program Files\VideoLAN\VLC\vlc.exe" and "C:\Program Files (x86)\VideoLAN\VLC\vlc.exe".

If you would like another player supported, let me know.

## Usage

Simply rename the config.json.example to config.json and add your video folder. It will look recursively for all videos in that folder. Here's a detailed list of options:

```WorkingFolder```: List of folders to look through when creating the list of videos

```Fullscreen```: Whether VLC launches in fullscreen mode or not

```RandomType```: Either "Video" or "Duration". Default is Video:

    If set to "Video", will not give more weight depending on the duration. Every video will have an equal chance of being selected.
    
    If set to "Duration", will give more weight to longer videos, but the initial making of the list will take longer (could take close to an hour for big collections). A progress bar will be shown to show progress.

Once the config is done, you can launch weinkmeister.exe:

![image](https://github.com/Cryptik-Rick/weink-meister/assets/105178852/ad3c3564-58e5-4412-ac67-89512f87ce21)

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader
{
    class OldDownloadElement
    {
        public String link { get; set; } 
        public Grid grid { get; set; }
        private TextBlock label { get; set; }
        private ProgressBar progressbar { get; set; }

        public OldDownloadElement(String link)
        {
            this.link = link;

            {
                this.grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Auto)
                });

                this.label = new TextBlock
                {
                    Text = this.link,
                    TextAlignment = TextAlignment.Left,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#fff"),
                    Margin = new Thickness(2)
                };
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                var rightPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(2)
                };

                this.progressbar = new ProgressBar
                {
                    Width = 40,
                    Margin = new Thickness(2),
                    Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#179c22"),
                    
                    Minimum = 1,
                    Maximum = 100,
                    Value = 0
                };
                rightPanel.Children.Add(progressbar);


                var button = new Button
                {
                    Content = new Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("icons/close.png", UriKind.Relative)),
                        Height = 16,
                        Width = 16
                    },
                    Background = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#00000000"),
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(2)
                };
                button.Click += (sender, e) =>
                {
                    // TODO
                };
                rightPanel.Children.Add(button);

                Grid.SetColumn(rightPanel, 1);
                grid.Children.Add(rightPanel);

                grid.Visibility = Visibility.Collapsed;
            }
        }



        public async Task StartDownloadAsync(String path)
        {
            var youtube = new YoutubeClient();
            try
            {
                var video = await youtube.Videos.GetAsync(link);

                // Wait for the information to be downloaded before displaying the grid
                label.Text = video.Title;
                grid.Visibility = Visibility.Visible;

                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(link);
                var streamInfo = streamManifest.GetAudioOnly().WithHighestBitrate();

                if (streamInfo != null)
                {
                    var progress = new Progress<double>(percent =>
                    {
                        progressbar.Value = Math.Round(percent * 100);
                    });

                    await youtube.Videos.Streams.DownloadAsync(streamInfo, System.IO.Path.Combine(path, RemoveInvalidChars(video.Title) + ".mp3"), progress);
                }
            } catch (ArgumentException)
            {
                grid.Visibility = Visibility.Visible;
                progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#b8200f");
                progressbar.Value = 100;
            }
        }

        private string RemoveInvalidChars(string filename)
        {
            return string.Concat(filename.Split(System.IO.Path.GetInvalidFileNameChars()));
        }
    }
}
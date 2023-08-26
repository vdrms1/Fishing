﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FishingFun
{
    public partial class FlyingFishAnimation : UserControl
    {
        private readonly List<Fish> fish = new List<Fish>();
        private readonly Random random = new Random();
        private readonly Timer t = new Timer { Interval = 30 };

        public FlyingFishAnimation()
        {
            InitializeComponent();

            t.Elapsed += MoveFishes;

            for (var i = 0; i < 30; i++)
            {
                fish.Add(CreateFish(random, i));
                FishGrid.Children.Add(fish.Last().image);
            }
        }

        public int AnimationWidth { get; set; }
        public int AnimationHeight { get; set; }

        public void Start()
        {
            fish.ForEach(f =>
            {
                f.X = random.Next(AnimationWidth);
                f.Y = random.Next(AnimationHeight);
                f.image.Visibility = f.index * 33 < AnimationHeight ? Visibility.Visible : Visibility.Collapsed;
            });

            t.Start();
        }

        public void Stop()
        {
            t.Stop();
            fish.ForEach(f => { f.image.Visibility = Visibility.Collapsed; });
        }

        private Fish CreateFish(Random r, int i)
        {
            var fish = new Fish(
                new Image
                {
                    Source = FishImage.Source,
                    Height = 100,
                    Width = 100,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Visibility = Visibility.Collapsed
                })
            {
                rotationAngle = r.Next(359),
                rotationStep = 30 - r.Next(60),
                speedY = r.Next(30) + 30,
                speedX = 30 - r.Next(60),
                index = i
            };

            fish.image.Margin = new Thickness(fish.X, fish.Y, 0, 0);
            //fish.image.RenderTransform = new RotateTransform(fish.rotationAngle);
            return fish;
        }

        private void MoveFishes(object sender, ElapsedEventArgs e)
        {
            Dispatch(() =>
            {
                fish.ForEach(f =>
                {
                    f.X = KeepInBounds(f.X + f.speedX, AnimationWidth);
                    f.Y = KeepInBounds(f.Y + f.speedY, AnimationHeight);
                    //f.rotationAngle = KeepInBounds(f.rotationAngle + f.rotationStep, 360);
                    f.image.Margin = new Thickness(f.X, f.Y, 0, 0);
                    //f.image.RenderTransform = new RotateTransform(f.rotationAngle);
                });
            });
        }

        public int KeepInBounds(int value, int max)
        {
            return value < 0 ? max + value : value > max ? value - max : value;
        }

        private void Dispatch(Action action)
        {
            Application.Current?.Dispatcher.BeginInvoke((Action)(() => action()));
            Application.Current?.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }

        private class Fish
        {
            public readonly Image image;
            public int index;
            public int rotationAngle;
            public int rotationStep;
            public int speedX;
            public int speedY;
            public int X;
            public int Y;

            public Fish(Image image)
            {
                this.image = image;
            }
        }
    }
}
﻿//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PeerConnectionClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExtendedSplashScreen : Page
    {
        private SplashScreen splash; // Variable to hold the splash screen object.
        private DispatcherTimer showWindowTimer;

        public ExtendedSplashScreen()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.splash = (SplashScreen)e.Parameter;
            if (splash != null)
            {
                PositionImage();
            }
        }

        void PositionImage()
        {
            Rect splashImageRect = splash.ImageLocation;
            extendedSplashImage.SetValue(Viewbox.HeightProperty, splashImageRect.Height);
            extendedSplashImage.SetValue(Viewbox.WidthProperty, splashImageRect.Width);
        }

        void ExtendedSplash_OnResize(Object sender, WindowSizeChangedEventArgs e)
        {
            // Safely update the extended splash screen image coordinates. This function will be fired in response to snapping, unsnapping, rotation, etc...
            if (splash != null)
            {
                // Update the coordinates of the splash screen image.
                PositionImage();
            }
        }
        
        private void OnShowWindowTimer(object sender, object e)
        {
            showWindowTimer.Stop();
            // Activate/show the window, now that the splash image has rendered
            Window.Current.Activate();
        }

        //https://msdn.microsoft.com/en-us/library/windows/apps/hh465338.aspx:
        //"Flicker occurs if you activate the current window (by calling Window.Current.Activate)
        //before the content of the page finishes rendering. You can reduce the likelihood of seeing
        //a flicker by making sure your extended splash screen image has been read before you activate
        //the current window. Additionally, you should use a timer to try to avoid the flicker by
        //making your application wait briefly, 50ms for example, before you activate the current window.
        //Unfortunately, there is no guaranteed way to prevent the flicker because XAML renders content
        //asynchronously and there is no guaranteed way to predict when rendering will be complete."
        private void extendedSplashImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            // ImageOpened means the file has been read, but the image hasn't been painted yet.
            // Start a short timer to give the image a chance to render, before showing the window
            // and starting the animation.
            showWindowTimer = new DispatcherTimer();
            showWindowTimer.Interval = TimeSpan.FromMilliseconds(50);
            showWindowTimer.Tick += OnShowWindowTimer;
            showWindowTimer.Start();
        }
    }
}

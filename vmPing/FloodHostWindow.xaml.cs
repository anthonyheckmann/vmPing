﻿using System;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace vmPing
{
    /// <summary>
    /// Interaction logic for FloodHostWindow.xaml
    /// </summary>
    public partial class FloodHostWindow : Window
    {
        FloodHostNode _floodHost = new FloodHostNode();


        public FloodHostWindow(bool alwaysOnTop)
        {
            InitializeComponent();

            this.DataContext = _floodHost;

            // Set window topmost attribute if specified in the application options.
            this.Topmost = alwaysOnTop;

            // Set initial focus to text box.
            Loaded += (sender, e) =>
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }


        private void btnFloodHost_Click(object sender, RoutedEventArgs e)
        {
            lblInformation.Visibility = Visibility.Collapsed;
            FloodHost(_floodHost);
        }


        public void FloodHost(FloodHostNode node)
        {
            if (!node.IsActive)
            {
                if (txtHostname.Text.Length == 0)
                    return;

                if (node.BgWorker != null)
                    node.BgWorker.CancelAsync();

                node.DestinationAddress = txtHostname.Text;
                node.PacketsSent = 0;
                node.PacketsReceived = 0;
                node.PacketsLost = 0;
                node.StartTime = DateTime.Now;
                node.IsActive = true;

                node.BgWorker = new BackgroundWorker();
                node.ResetEvent = new AutoResetEvent(false);
                node.BgWorker.DoWork += new DoWorkEventHandler(backgroundThread_FloodHost);
                node.BgWorker.WorkerSupportsCancellation = true;
                node.BgWorker.RunWorkerAsync(node);
            }
            else
            {
                node.BgWorker.CancelAsync();
                node.ResetEvent.WaitOne();
                node.IsActive = false;
            }
        }


        public void backgroundThread_FloodHost(object sender, DoWorkEventArgs e)
        {
            var bgWorker = sender as BackgroundWorker;
            var node = e.Argument as FloodHostNode;

            var pingBuffer = Encoding.ASCII.GetBytes(Constants.PING_DATA);
            var pingOptions = new PingOptions(Constants.PING_TTL, true);

            while (!bgWorker.CancellationPending && node.IsActive)
            {
                using (Ping ping = new Ping())
                {
                    try
                    {
                        ++node.PacketsSent;
                        if (ping.Send(node.DestinationAddress, 100, pingBuffer, pingOptions).Status == IPStatus.Success)
                            ++node.PacketsReceived;
                        else
                            ++node.PacketsLost;

                        node.ResetEvent.Set();
                    }
                    catch
                    {
                        e.Cancel = true;
                        node.ResetEvent.Set();
                        node.IsActive = false;
                        return;
                    }
                }
            }

            node.ResetEvent.Set();
        }
    }



    public class FloodHostButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value == false)
                return "Flood Host";
            else
                return "Stop Flood";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToHiddenVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value == false)
                return Visibility.Hidden;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Management.Infrastructure;

namespace OnlineDisk
{
    public partial class MainWindow : Window
    {
        const string version = "3.0";

        CimSession MSFTSession;
        Dictionary<UInt32, CimInstance> DiskTable;

        public MainWindow()
        {
            InitializeComponent();
            MSFTSession = CimSession.Create(null);
            DiskTable = new Dictionary<UInt32, CimInstance>();
            var DiskList = MSFTSession.QueryInstances(@"Root\Microsoft\Windows\Storage", "WQL", "SELECT * FROM MSFT_DISK");
            var PartitionList = MSFTSession.QueryInstances(@"Root\Microsoft\Windows\Storage", "WQL", "SELECT * FROM MSFT_PARTITION");
            foreach (var Disk in DiskList)
            {
                DiskDataGridRow NewRow = new DiskDataGridRow
                {
                    DiskNum = (UInt32)Disk.CimInstanceProperties["Number"].Value,
                    IsSystem = (Boolean)Disk.CimInstanceProperties["IsSystem"].Value,
                    IsOnline = !(Boolean)Disk.CimInstanceProperties["IsOffline"].Value,
                    DiskName = (string)Disk.CimInstanceProperties["FriendlyName"].Value
                };
                List<Char> VolumeList = new List<char>();
                DiskTable.Add(NewRow.DiskNum, Disk);
                foreach (var Par in PartitionList)
                {
                    if (NewRow.DiskNum == (UInt32)Par.CimInstanceProperties["DiskNumber"].Value)
                    {
                        Char DriveLetter = (Char)Par.CimInstanceProperties["DriveLetter"].Value;
                        if (Char.IsLetter(DriveLetter))
                            VolumeList.Add(DriveLetter);
                    }
                }
                VolumeList.Sort();
                foreach (Char VolumeLetter in VolumeList)
                {
                    NewRow.Volumes += (VolumeLetter + @":\; ");
                }
                DiskDataGrid.Items.Add(NewRow);
            }
            DiskDataGrid.Items.SortDescriptions.Add(new SortDescription("DiskNum", ListSortDirection.Ascending));
        }

        void Menu_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void Menu_About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void OnlineButton_Click(object sender, RoutedEventArgs e)
        {
            UInt32 ReturnValue = (UInt32)MSFTSession.InvokeMethod(DiskTable[((DiskDataGridRow)DiskDataGrid.SelectedItem).DiskNum], "Online", new CimMethodParametersCollection()).ReturnValue.Value;
            if (ReturnValue != 0)
            {
                MessageBox.Show("Operation \"Online\" Failed with Code " + ReturnValue, "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            DiskDataGridRow SelectedRow = (DiskDataGridRow)DiskDataGrid.SelectedItem;
            SelectedRow.IsOnline = true;
            DiskDataGrid.Items.Refresh();
            UpdateButton();
        }

        private void OfflineButton_Click(object sender, RoutedEventArgs e)
        {
            UInt32 ReturnValue = (UInt32)MSFTSession.InvokeMethod(DiskTable[((DiskDataGridRow)DiskDataGrid.SelectedItem).DiskNum], "Offline", new CimMethodParametersCollection()).ReturnValue.Value;
            if (ReturnValue != 0)
            {
                MessageBox.Show("Operation \"Offline\" Failed with Code " + ReturnValue, "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            DiskDataGridRow SelectedRow = (DiskDataGridRow)DiskDataGrid.SelectedItem;
            SelectedRow.IsOnline = false;
            DiskDataGrid.Items.Refresh();
            UpdateButton();
        }

        private void DiskDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButton();
        }

        private void UpdateButton()
        {
            if (((DiskDataGridRow)DiskDataGrid.SelectedItem).IsSystem)
            {
                OnlineButton.IsEnabled = false;
                OfflineButton.IsEnabled = false;
            }
            else
            {

                if (((DiskDataGridRow)DiskDataGrid.SelectedItem).IsOnline)
                {
                    OnlineButton.IsEnabled = false;
                    OfflineButton.IsEnabled = true;
                }
                else
                {
                    OnlineButton.IsEnabled = true;
                    OfflineButton.IsEnabled = false;
                }
            }
        }
    }

    public class DiskDataGridRow
    {
        public Boolean IsSystem
        {
            get => (System == "Yes") ? true : false;
            set => System = value ? "Yes" : "No";
        }

        public Boolean IsOnline
        {
            get => (Online == "Yes") ? true : false;
            set => Online = value ? "Yes" : "No";
        }

        public UInt32 DiskNum { get; set; }
        public string System { get; set; }
        public string Online { get; set; }
        public string Volumes { get; set; }
        public string DiskName { get; set; }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace SharingApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


            ListBoxItem li = listMain.SelectedItem as ListBoxItem;
            string title = "", link = "";
            if (li != null)
            {
                link = "http://" + li.Content.ToString();
                title = li.Content.ToString().Split(new char[] { '.' })[0];
                NavigationService.Navigate(new Uri("/Sharing.xaml?link=" + HttpUtility.HtmlEncode(link) + "&title=" + title, UriKind.Relative));

                listMain.SelectedIndex = -1;
            }

            

            
        }
    }
}
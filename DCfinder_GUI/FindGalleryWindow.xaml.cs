using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Library;

namespace DCfinder_GUI
{
    /// <summary>
    /// FindGallery.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FindGalleryWindow : Window
    {
        private DCfinder dcfinder = new DCfinder();
        public FindGalleryWindow()
        {
            InitializeComponent();
        }

        private GalleryDictionary dic;
        private void findGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            findGallery();
        }

        private void findGalleryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                findGallery();
            }
        }

        private async void findGallery()
        {
            string keyword = findGalleryTextBox.Text;
            dic = await Task.Run(() => dcfinder.GetGalleryByName(keyword));
            noItemTextBlock.Visibility = Visibility.Hidden;
            findGalleryListView.Items.Clear();

            foreach (var key in dic.Keys)
            {
                if (key.Contains(keyword))
                {
                    findGalleryListView.Items.Add(dic[key]);
                }
            }

            if (findGalleryListView.Items.Count == 0)
            {
                noItemTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void findGalleryListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListView listview = (ListView)sender;
            string id = ((Gallery)listview.SelectedItem).gallery_id;
            // Window mainwindow = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow;
            MainWindow.Instance.galleryTextBox.Text = id;
            this.Close();
        }
    }
}

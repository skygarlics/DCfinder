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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Library;
using static Library.DCfinder;
using System.Threading;
using System.Windows.Media.Animation;

namespace DCfinder_GUI
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Instance;
        private static DCfinder dcfinder;
        private ArticleCollection searchResult = new ArticleCollection();

        public MainWindow()
        {
            InitializeComponent();
            Analytics.Init();
            Instance = this;

            articleListView.ItemsSource = searchResult;

            setProgressHeight(0);
        }

        private void setProgressHeight(int pixels)
        {
            mainGrid.RowDefinitions[2].Height = new GridLength(pixels);
        }

        private bool isSearching = false;

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSearching)
            {
                EndSearchGallery();
            }
            else
            {
                BeginSearchGallery();
            }
        }

        private void BeginSearchGallery()
        {
            searchResult.Clear();
            searchProgressBar.SetPercent(0,0);
            setProgressHeight(25);
            searchButton.Content = "중지";

            isSearching = true;
            SearchGallery();
        }

        private void EndSearchGallery()
        {
            isSearching = false;
            searchButton.Content = "검색";
            setProgressHeight(0);
        }

        private async void SearchGallery()
        {
            string gallery_id = galleryTextBox.Text;
            string keyword = keywordTextBox.Text;
            string query = ((SearchOption)optionComboBox.SelectedItem).Query;
            uint depth = Convert.ToUInt32(depthTextBox.Text);
            bool minor = (bool)minorGallCheckBox.IsChecked;
            bool recommend = (bool)recOnlyCheckBox.IsChecked;

            if (minor)
                dcfinder = new MDCfinder();
            else
                dcfinder = new DCfinder();

            uint searchpos = await Task.Run(() => dcfinder.GetSearchPos(gallery_id, keyword, query));

            if (searchpos == 987654321)
            {
                if (dcfinder.GetType() == typeof(DCfinder)) {
                    // DCfinder로 갤러리 접근을 시도했으나 마이너갤러리로 리디렉션 되는 경우
                    minorGallCheckBox.IsChecked = true;
                    dcfinder = new MDCfinder();
                }
                else if (dcfinder.GetType() == typeof(MDCfinder))
                {
                    // MDCfinder로 갤러리 접근을 시도했으나 메이저 갤러리가 나오는 경우
                    minorGallCheckBox.IsChecked = false;
                    dcfinder = new DCfinder();
                }
                else
                {
                    throw new TypeAccessException();
                }
                searchpos = await Task.Run(() => dcfinder.GetSearchPos(gallery_id, keyword, query));
            }

            if (searchpos == 0)
            {
                MessageBox.Show("갤러리가 존재하지 않습니다", "X (");
                EndSearchGallery();
                return;
            }

            for (uint idx = 0; idx < depth; idx++)
            {
                if (!isSearching)
                {
                    break;
                }
                ArticleCollection articles = await Task.Run(() => dcfinder.CrawlSearch(gallery_id, keyword, query, searchpos - (idx * 10000), recommend));
                searchProgressBar.SetPercent((idx + 1) / (double)depth * 100.0);
                for (int article_idx = 0; article_idx < articles.Count; ++article_idx)
                {
                    searchResult.Add(articles[article_idx]);
                }
            }
            EndSearchGallery();
        }

        private void articleListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (articleListView.SelectedItems.Count == 1)
            { 
                Article article = (Article)articleListView.SelectedItem;
                string url = String.Format("http://gall.dcinside.com/board/view/?id={0}&no={1}", galleryTextBox.Text, article.notice);
                System.Diagnostics.Process.Start(url);
            }
        }

        private void depthTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BeginSearchGallery();
            }
            else
            {
                var isNumber = (Key.D0 <= e.Key && e.Key <= Key.D9) || (Key.NumPad0 <= e.Key && e.Key <= Key.NumPad9);
                if (!isNumber)
                {
                    e.Handled = true;
                }
            }
        }

        private FindGalleryWindow findWindow;
        private FindGalleryWindow FindWindow
        {
            get
            {
                if (findWindow == null || !findWindow.IsLoaded)
                {
                    findWindow = new FindGalleryWindow();
                }
                return findWindow;
            }
        }
        private void findGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            FindWindow.Show();
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void keywordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BeginSearchGallery();
            }
        }

        private void galleryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BeginSearchGallery();
            }
        }

        private void depthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }

    public static class ProgressBarExtensions
    {
        public static void SetPercent(this ProgressBar progressBar, double percentage)
        {
            SetPercent(progressBar, percentage, 0.5);
        }

        public static void SetPercent(this ProgressBar progressBar, double percentage, double duration)
        {
            DoubleAnimation animation = new DoubleAnimation(percentage, TimeSpan.FromSeconds(duration));
            progressBar.BeginAnimation(ProgressBar.ValueProperty, animation);
        }
    }
}

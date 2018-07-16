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
using HtmlAgilityPack;

namespace DCfinder_GUI
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Instance;
        private static DCfinder dcfinder;
        public ArticleCollection searchResult = new ArticleCollection();
        public CancellationTokenSource tokenSource;
        private static HtmlDocument parser = new HtmlDocument();
        private const int pos_per_depth = 10000;

        public MainWindow()
        {
            InitializeComponent();
            #if (!DEBUG)
            Analytics.Init();
            #endif
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
            tokenSource = new CancellationTokenSource();
            SearchGallery(tokenSource.Token);
        }

        private async void EndSearchGallery()
        {
            tokenSource.Cancel();
            // wait until tasks over
            while (isSearching == true)
            {
                await Task.Delay(10);
            }
            tokenSource.Dispose();
            searchButton.Content = "검색";
            setProgressHeight(0);
        }

        private async void SearchGallery(CancellationToken token)
        {
            string gallery_id = galleryTextBox.Text;
            string keyword = keywordTextBox.Text;
            string search_type = ((SearchOption)optionComboBox.SelectedItem).Query;
            uint depth = Convert.ToUInt32(depthTextBox.Text);
            bool minor = (bool)minorGallCheckBox.IsChecked;
            bool recommend = (bool)recOnlyCheckBox.IsChecked;

            if (minor)
                dcfinder = new MDCfinder();
            else
                dcfinder = new DCfinder();

            uint searchpos = await Task.Run(() => dcfinder.GetSearchPos(gallery_id, keyword, search_type));

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
                searchpos = await Task.Run(() => dcfinder.GetSearchPos(gallery_id, keyword, search_type));
            }

            if (searchpos == 0)
            {
                MessageBox.Show("갤러리가 존재하지 않습니다", "X (");
                EndSearchGallery();
                return;
            }

            double percent_per_depth = 100 / (double)depth;

            for (uint depth_idx = 0; depth_idx < depth; depth_idx++)
            {
                if (token.IsCancellationRequested)
                {
                    isSearching = false;
                    break;
                }

                uint search_pos = searchpos - (depth_idx * pos_per_depth);

                string search_query = "&page={0}&search_pos=-{1}&s_type={2}&s_keyword={3}";
                if (recommend)
                {
                    search_query += "&exception_mode=recommend";
                }
                string board_url = dcfinder.gall_base_url + "/board/lists/?id=" + gallery_id;
                string request_url = board_url + String.Format(search_query, 1, search_pos, search_type, keyword);

                // get first page
                string html = await dcfinder.RequestPageAsync(request_url);
                parser.LoadHtml(html);
                HtmlNode page_btns = parser.DocumentNode.SelectSingleNode("//div[@id='dgn_btn_paging']");

                // get last page number
                int page_len = 1;
                if (dcfinder.CountNextBtn(page_btns.OuterHtml) > 1)
                {
                    // board length > 10
                    HtmlNode last_btn = page_btns.ChildNodes[12];
                    page_len = dcfinder.GetLastPage(last_btn);
                }
                else
                {
                    page_len = dcfinder.CountPages(page_btns);
                }

                // get articles of page1, which already loaded
                {
                    ArticleCollection articles = new ArticleCollection(html);
                    foreach (var article in articles)
                    {
                        searchResult.Add(article);
                    }
                }

                double percent_per_page = percent_per_depth / page_len;
                searchProgressBar.SetPercent(percent_per_depth * depth_idx + percent_per_page);

                // process rest of pages
                List<Task<ArticleCollection>> tasks = new List<Task<ArticleCollection>>();
                {
                    const int MAX_TASK = 10;
                    int page_idx;
                    int cnt;
                    ArticleCollection[] articleCollections;
                    for (page_idx = 2, cnt = 1; page_idx <= page_len; ++page_idx, ++cnt)
                    {
                        if (token.IsCancellationRequested)
                        {
                            isSearching = false;
                            tasks.Clear();
                            break;
                        }
                        request_url = board_url + String.Format(search_query, page_idx, search_pos, search_type, keyword);
                        tasks.Add(dcfinder.GetArticlesAsync(request_url));

                        if (cnt >= MAX_TASK)
                        {
                            articleCollections = await Task.WhenAll<ArticleCollection>(tasks);
                            foreach (var articles in articleCollections)
                                foreach (var article in articles)
                                    searchResult.Add(article);

                            tasks.Clear();
                            cnt = 1;
                            searchProgressBar.SetPercent(percent_per_depth * depth_idx + percent_per_page * page_idx);
                        }
                    }
                    // final
                    articleCollections = await Task.WhenAll<ArticleCollection>(tasks);
                    foreach (var articles in articleCollections)
                        foreach (var article in articles)
                            searchResult.Add(article);
                    tasks.Clear();
                }
                searchProgressBar.SetPercent(percent_per_depth * (depth_idx + 1));
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

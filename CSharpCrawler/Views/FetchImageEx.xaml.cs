﻿using CSharpCrawler.Controls;
using CSharpCrawler.Model;
using CSharpCrawler.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

namespace CSharpCrawler.Views
{
    /// <summary>
    /// FetchImageEx.xaml 的交互逻辑
    /// </summary>
    public partial class FetchImageEx : Page
    {
        GlobalDataUtil globalData = GlobalDataUtil.GetInstance();
        object obj = new object();
        ObservableCollection<UrlStruct> imageCollection = new ObservableCollection<UrlStruct>();
        List<UrlStruct> ToVisitList = new List<UrlStruct>();
        List<UrlStruct> VisitedList = new List<UrlStruct>();

        int globalIndex = 1;
        string baseUrl = "";

        public FetchImageEx()
        {
            InitializeComponent();
        }

        private void btn_Surfing_Click(object sender, RoutedEventArgs e)
        {
            string url = this.tbox_Url.Text;

            if (string.IsNullOrEmpty(url))
            {
                ShowStatusText("请输入Url");
                return;
            }

            if (globalData.CrawlerConfig.CommonConfig.UrlCheck == true)
            {
                if (RegexUtil.IsUrl(url) == false)
                {
                    ShowStatusText("网址输入有误");
                    return;
                }
            }

            baseUrl = UrlUtil.FixUrl(url);

            Reset();
            Surfing(url);
        }

        public void ShowStatusText(string content)
        {
            this.Dispatcher.Invoke(() => {
                this.lbl_Status.Content = content;
            });
        }

        public void Surfing(string url)
        {
            ShowStatusText($"正在从{url}抓取图像");
            if (globalData.CrawlerConfig.ImageConfig.DynamicGrab == true)
            {
                SurfingByCEF(url);
            }
            else
            {
                SurfingByFCL(url);
            }
        }

        private void SurfingByCEF(string url)
        {
            globalData.Browser.GetHtmlSourceDynamic(url, ExtractImageCallBack);
        }

        private async void SurfingByFCL(string url)
        {
            try
            {
                string html = await WebUtil.GetHtmlSource(url);
                StartExtractThread(html);
            }
            catch (Exception ex)
            {
                //TODO
                ShowStatusText(ex.Message);
            }
        }

        private void ExtractImage(object html)
        {
            switch (globalData.CrawlerConfig.ImageConfig.FetchMode)
            {
                case 0:
                    ExtractImageMixed(html);
                    break;
                case 1:
                    ExtractImageWithRegex(html);
                    break;
                case 2:
                    ExtractImageWithHtmlAgilityPack(html);
                    break;
                default:
                    break;
            }
        }

        private void ExtractImageWithRegex(object html)
        {
            try
            {
                string value = "";
                MatchCollection mc = RegexUtil.Match(html.ToString(), RegexPattern.TagImgPattern);
                foreach (Match item in mc)
                {
                    value = item.Groups["image"].Value;
                    if (value.Contains("//") == false)
                    {
                        value = baseUrl + value;
                    }
                    AddToCollection(new UrlStruct() { Id = globalIndex, Status = "", Title = "", Url = value });
                }
                ShowStatusText($"已抓取到{mc.Count}个图像");
            }
            catch (Exception ex)
            {
                ShowStatusText(ex.Message);
            }
        }

        private void ExtractImageWithHtmlAgilityPack(object html)
        {
            try
            {
                string value = "";
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html.ToString());

                HtmlAgilityPack.HtmlNodeCollection imgNodeCollection = doc.DocumentNode.SelectNodes("//img");
                for (int i = 0; i < imgNodeCollection.Count; i++)
                {
                    value = imgNodeCollection[i].Attributes["src"].Value;
                    if (value.StartsWith("//"))
                    {
                        value = "http:" + value;
                    }

                    if (value.Contains(":") == false)
                    {
                        value = baseUrl + value;
                    }
                    AddToCollection(new UrlStruct() { Id = globalIndex, Status = "", Title = "", Url = value });
                }
                ShowStatusText($"已抓取到{imgNodeCollection.Count}个图像");
                ShowImage(imageCollection);
            }
            catch (Exception ex)
            {
                ShowStatusText(ex.Message);
            }
        }

        private void ExtractImageMixed(object html)
        {

        }

        private void StartExtractThread(object html)
        {
            Thread extractThread = new Thread(new ParameterizedThreadStart(ExtractImage));
            extractThread.IsBackground = true;
            extractThread.Start(html);
        }

        private void ExtractImageCallBack(string html)
        {
            StartExtractThread(html);
        }

        public void AddToCollection(UrlStruct urlStruct)
        {
            lock (obj)
            {
                var query = imageCollection.Where(x => x.Url == urlStruct.Url).FirstOrDefault();
                if (query != null)
                    return;
                Dispatcher.Invoke(() => {
                    imageCollection.Add(urlStruct);
                    ToVisitList.Add(urlStruct);
                });
                globalIndex++;
            }
        }

        public void ClearCollection()
        {
            Dispatcher.Invoke(() => {
                imageCollection.Clear();
            });
        }

        public void Reset()
        {
            ClearCollection();
            globalIndex = 1;
            ShowStatusText("");
        }

        public void ShowImage(ObservableCollection<UrlStruct> list)
        {
            int count = list.Count;

            this.Dispatcher.Invoke(()=> {
                this.grid_Content.ColumnDefinitions.Clear();
                this.grid_Content.Children.Clear();
            }); 

            for (int i = 0; i < count; i++)
            {
                this.Dispatcher.Invoke(()=> {
                    grid_Content.ColumnDefinitions.Add(new ColumnDefinition());

                    ListImage image = new ListImage();
                    image.Margin = new Thickness(10);
                    image.Text = "我独自坐在这静静的静静的静静的窗台上";
                    image.Image = new BitmapImage(new Uri(list[i].Url));                 
                    Grid.SetColumn(image, i);
                    grid_Content.Children.Add(image);
                });             
            }
        }

        private void scroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset - 50);
            }
            else
            {
                scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset + 50);
            }
        }
    }
}
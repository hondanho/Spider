
using DotnetCrawler.Core.RabitMQ;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Downloader;
using DotnetCrawler.Scheduler;
using HtmlAgilityPack;
using System.Threading.Tasks;

namespace DotnetCrawler.Core
{
    public interface ICrawlerCore<T> where T : class
    {
        CrawlerCore<T> AddRequest(SiteConfigDb request);
        CrawlerCore<T> AddDownloader(IDotnetCrawlerDownloader downloader);
        CrawlerCore<T> AddScheduler(IDotnetCrawlerScheduler scheduler);
        Task Crawle(bool isReCrawleSmall = false);


        void JobCategory(CategoryMessage categoryMessage);
        void JobPost(PostMessage post);
        void JobPostDetail(PostDetailMessage post);
        void JobChap(ChapMessage chapMessage);
        }
}



namespace DotnetCrawler.Data.Constants
{
    public static class QueueName
    {
        /// <summary>
        /// category url process crawle
        /// </summary>
        public const string QueueCrawleCategory = "category-crawle-queue";

        /// <summary>
        /// post url process crawle
        /// </summary>
        public const string QueueCrawlePost = "post-crawle-queue";

        /// <summary>
        /// post url paging process crawle
        /// </summary>
        public const string QueueCrawlePostDetail = "post-detail-crawle-queue";

        /// <summary>
        /// chap url process crawle
        /// </summary>
        public const string QueueCrawleChap = "chap-crawle-queue";

        /// <summary>
        /// post data process sync
        /// </summary>
        public const string QueueSyncPost = "post-sync-queue";

        /// <summary>
        /// chap data process sync
        /// </summary>
        public const string QueueSyncChap = "chap-sync-queue";
    }
}

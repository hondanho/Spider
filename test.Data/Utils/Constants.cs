

namespace DotnetCrawler.Data.Constants
{
    public class QueueName
    {
        /// <summary>
        /// category url process crawle
        /// </summary>
        public const string QueueCrawleCategory = "crawle-queue-category";

        /// <summary>
        /// post url process crawle
        /// </summary>
        public const string QueueCrawlePost = "crawle-queue-post";

        /// <summary>
        /// post url paging process crawle
        /// </summary>
        public const string QueueCrawlePostDetail = "crawle-queue-post-detail";

        /// <summary>
        /// chap url process crawle
        /// </summary>
        public const string QueueCrawleChap = "crawle-queue-chap";

        /// <summary>
        /// post data process sync
        /// </summary>
        public const string QueueSyncPost = "sync-queue-post";

        /// <summary>
        /// chap data process sync
        /// </summary>
        public const string QueueSyncChap = "sync-queue-chap";
    }

    public class MetaFieldPost
    {
        public const string TacGia = "tw_author";
        public const string AlternativeName = "tw_alternative_name";
        public const string Genre = "tw_genre";
        public const string Source = "tw_source";
        public const string Status = "tw_status";
    }

    public class PostStatus
    {
        public const string Completed = "Completed";
        public const string OnGoing = "ongoing";
    }
}

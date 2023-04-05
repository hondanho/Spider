using System;
using System.Collections.Generic;
using System.Text;

namespace DotnetCrawler.Data.Constants
{
    public static class QueueName
    {
        /// <summary>
        /// category url process
        /// </summary>
        public const string QueueCategoryName = "category-queue";
        /// <summary>
        /// post url process
        /// </summary>
        public const string QueuePostName = "post-queue";
        /// <summary>
        /// post url paging process
        /// </summary>
        public const string QueuePostDetailName = "post-detail-queue";
        /// <summary>
        /// chap url process
        /// </summary>
        public const string QueueChapName = "chap-queue";
    }
}

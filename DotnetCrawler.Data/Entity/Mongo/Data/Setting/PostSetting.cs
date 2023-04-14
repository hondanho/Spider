using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DotnetCrawler.Data.Setting {

    public class PostSetting {
        [Required]
        /// <summary>
        /// string xpath title
        /// </summary>
        public string Titlte { get; set; }

        [Required]
        /// <summary>
        /// string xpth description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// string xpath slug
        /// </summary>
        public string Slug { get; set; }

        /// <summary>
        /// string xpath ảnh đại diện
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// bài viết con
        /// </summary>
        public bool IsHasChapter { get; set; } = false;

        /// <summary>
        ///  remove element by css selector
        /// </summary>
        public List<string> RemoveElementCssSelector { get; set; }

        /// <summary>
        /// key, xpath example tw_status, .col-truyen-main .info div:last-child a
        /// </summary>
        public List<Dictionary> Metadatas { get; set; }
        /// <summary>
        ///  remove element như script, link, iframe, video
        /// </summary>
        public List<string> RemoveNodeElement { get; set; }

        /// <summary>
        /// Xác định phần tử HTML của URL trang tiếp theo, Mặc định sẽ lấy thuộc tính "href" của thẻ "a". Ví dụ: .pagination > a.next. Nếu bạn khai báo nhiều bộ lọc, phát hiện đầu tiên sẽ được sử dụng.	
        /// </summary>
        public string LinkChapSelector { get; set; }
        /// <summary>
        /// Xác định phần tử HTML của mỗi URL trang tiếp theo, Ví dụ: .post > .parts > a. Nếu bạn khai báo nhiều bộ lọc, phát hiện đầu tiên sẽ được sử dụng.
        /// </summary>
        public string PagingSelector { get; set; }

    }

    public class Dictionary {
        public string Key { get; set; }
        public string Value { get; set; }
        public List<string> RemoveElement { get; set; }
    }
}

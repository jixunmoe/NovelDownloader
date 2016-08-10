﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom.Html;
using moe.Jixun.Plugin;
using H = moe.Jixun.Plugin.PluginHelper;

namespace moe.jixun.Plugin.PiaoTian
{
    public class PiaoTian: IPluginProvider
    {
        internal static PiaoTian Instance;
        public PiaoTian()
        {
            if (Instance == null)
                Instance = this;
        }

        public PluginType Type => PluginType.SiteProvider;
        public string PackageName => "Plugin.PiaoTian";
        public string DisplayName => "飘天文学";

        private const string BaseUrl = "http://piaotian.net/";
        private const string SearchUrl = "http://piaotian.net/s.php";
        private const string ChapterUrl = "http://www.piaotian.net/html/{0}/{1}/index.html";
        public async Task<List<IBookMeta>> SearchBook(string bookName)
        {
            var data = new Dictionary<string, string>();
            /*
             * type:articlename
             * s:%CE%D2%D5%E6%CA%C7%B4%F3%C3%F7%D0%C7
             * Submit:+%CB%D1+%CB%F7+
             */
            data["type"] = "articlename";
            data["s"] = bookName;

            var html = await H.RequestGbkAsync(SearchUrl, data: data);
            var doc = H.ParseHtml(html);
            return doc.QuerySelectorAll(".cover > p.line")
                .Select(line => line.GetElementsByTagName("a"))
                .Where(line => line.Length == 3)
                .Select(links =>
                {
                    var title = (IHtmlAnchorElement) links[1];
                    var author = (IHtmlAnchorElement) links[2];

                    return (IBookMeta)new PiaoTianBook(
                        title.TextContent, title.Href,
                        author.TextContent, author.Href
                    );
                }).ToList();
        }

        public Task<IBookMeta> GetBookMeta(string bookId)
        {
            throw new System.NotImplementedException();
        }

        private static readonly Regex DigitsRegex = new Regex(@"\d+");
        public async Task DownloadChapters(PiaoTianBook book)
        {
            // var urlBook = new Url(new Url(BaseUrl), book.BookId);
            // 提取纯数字的书籍 ID

            var m = DigitsRegex.Match(book.BookId);
            if (m.Groups.Count == 0)
                return;
            
            var id = m.Groups[0].Value;
            var prefixId = id.Length < 4
                ? "0"
                : id.Substring(0, id.Length - 3);

            var chaptersUrl = string.Format(ChapterUrl, prefixId, id);
            var data = await H.RequestGbkAsync(chaptersUrl);
            var doc = H.ParseHtml(data);

            book.Chapters.Clear();
            book.Chapters.AddRange(doc
                .QuerySelectorAll(".centent > ul a")
                .Skip(4)
                .Select(chapter => new PiaoTianChapter(book, chapter.TextContent))
                .ToList());
        }
    }

    public class PiaoTianBook : IBookMeta
    {
        public IPluginProvider Plugin => PiaoTian.Instance;
        public PiaoTianBook(string name, string bookId, string author, string authorId)
        {
            Name = name;
            BookId = bookId;
            Author = author;
            AuthorId = authorId;
            Chapters = new List<IBookChapter>();
        }

        public string Name { get; }
        public string BookId { get; }
        public string Author { get; }
        public string AuthorId { get; }
        public List<IBookChapter> Chapters { get; }

        public async Task DownloadChapterList()
        {
            await PiaoTian.Instance.DownloadChapters(this);
        }
    }

    public class PiaoTianChapter : IBookChapter
    {
        private readonly PiaoTianBook _book;
        public PiaoTianChapter(PiaoTianBook book, string name)
        {
            _book = book;
            Name = name;
        }

        public string Name { get; }
        public IBookMeta Book => _book;

        public string Download()
        {
            throw new System.NotImplementedException();
        }
    }
}
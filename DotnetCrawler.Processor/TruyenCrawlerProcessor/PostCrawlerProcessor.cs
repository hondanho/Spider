using DotnetCrawler.Data.Attributes;
using DotnetCrawler.Data.AutoMap;
using DotnetCrawler.Data.Models.Novel;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Setting;
using DotnetCrawler.Request;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotnetCrawler.Processor
{
    public class PostCrawlerProcessor : IPostCrawlerProcessor
    {
        private readonly IDotnetCrawlerRequest _request;

        public PostCrawlerProcessor(IDotnetCrawlerRequest request) {
            _request = request;
        }

        public async Task<PostDb> Process(HtmlDocument document)
        {
            var processorEntity = GetColumnNameValuePairsFromHtml(document);

            //Initializing AutoMapper
            //var mapper = AutoMapperHelper.InitializeAutomapper();
            //var processorEntity = mapper.Map<PostSetting, PostDb>(postData);
            return processorEntity;
        }

        private static PostDb GetColumnNameValuePairsFromHtml(HtmlDocument document)
        {
            var entity = new PostDb();
            var entityNode = document.DocumentNode;

            foreach (var expression in propertyExpressions)
            {
                var columnName = expression.Key;
                object columnValue = null;
                var fieldExpression = expression.Value.Item2;

                switch (expression.Value.Item1)
                {
                    case SelectorType.XPath:
                        var node = entityNode.SelectSingleNode(fieldExpression);
                        if (node != null)
                            columnValue = node.InnerText;
                        break;
                    case SelectorType.CssSelector:
                        var nodeCss = entityNode.QuerySelector(fieldExpression);
                        if (nodeCss != null)
                            columnValue = nodeCss.InnerText;
                        break;
                    case SelectorType.FixedValue:
                        if (Int32.TryParse(fieldExpression, out var result))
                        {
                            columnValue = result;
                        }
                        break;
                    default:
                        break;
                }
                columnNameValueDictionary.Add(columnName, columnValue);
            }

            return columnNameValueDictionary;
        }
    }
}

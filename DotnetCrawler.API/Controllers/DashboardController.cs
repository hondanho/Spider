using DotnetCrawler.Api.Service;
using DotnetCrawler.Data.Model;
using DotnetCrawler.Data.ModelDb;
using DotnetCrawler.Data.Models;
using DotnetCrawler.Data.Repository;
using DotnetCrawler.Data.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Remotion.Linq.Clauses.ResultOperators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetCrawler.Api.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase {
        private readonly IMongoRepository<CategoryDb> _categoryDbRepository;
        private readonly IMongoRepository<ChapDb> _chapDbRepository;
        private readonly IMongoRepository<PostDb> _postDbRepository;

        public DashboardController(
            ILogger<SiteController> logger,
            IMongoRepository<CategoryDb> categoryDbRepository,
            IMongoRepository<SiteConfigDb> siteConfigDbRepository,
            IMongoRepository<PostDb> postDbRepository,
            IMongoRepository<ChapDb> chapDbRepository,
            ICrawlerService crawlerService) {
            _categoryDbRepository = categoryDbRepository;
            _postDbRepository = postDbRepository;
            _chapDbRepository = chapDbRepository;
        }

        [HttpGet("validate")]
        public CheckerDataModel GetDuplicateByPostSlug() {
            var result = new CheckerDataModel();

            var duplicateCategory = _categoryDbRepository.AsQueryable()
                                .ToList()?.GroupBy(p => p.Slug, p => p.Titlte)
                               .Where(g => g.Count() > 1)
                               .Select(g => new DuplicateRecord {
                                   Slug = g.Key,
                                   Count = g.Count()
                               }).ToList();
            result.CategoryDuplicate = new Duplicate {
                DuplicateRecords = duplicateCategory,
                Count = duplicateCategory.Count()
            };

            var duplicatePost = _postDbRepository.AsQueryable()
                                .ToList()?.GroupBy(p => p.Slug, p => p.CategorySlug)
                               .Where(g => g.Count() > 1)
                               .Select(g => new DuplicateRecord {
                                   Slug = g.Key,
                                   Count = g.Count()
                               }).ToList();
            result.PostDuplicate = new Duplicate {
                DuplicateRecords = duplicatePost,
                Count = duplicatePost.Count
            };

            var duplicateChap = _chapDbRepository.AsQueryable()
                                .ToList()?.GroupBy(p => p.Slug, p => p.PostSlug)
                               .Where(g => g.Count() > 1)
                               .Select(g => new DuplicateRecord {
                                   Slug = g.Key,
                                   Count = g.Count()
                               }).ToList();
            result.ChapDuplicate = new Duplicate {
                DuplicateRecords = duplicateChap,
                Count = duplicateChap.Count()
            };

            return result;
        }


        [HttpGet("data-verify")]
        public async Task<List<CategoryInfo>> GetSiteInfos(int pageNumber, int take) {
            if(pageNumber < 1)
                pageNumber = 1;
            if(take < 1)
                take = 3;

            var result = new List<CategoryInfo>();
            var categoryDbs = _categoryDbRepository.AsQueryable().ToList();
            if(categoryDbs.Any()) {
                foreach(var category in categoryDbs) {
                    var postInfos = new List<PostInfo>();

                    var postDbs = _postDbRepository.FilterBy(pdb => pdb.CategorySlug == category.Slug).AsQueryable().Skip((pageNumber - 1) * take).Take(take).ToList();
                    var postCount = _postDbRepository.AsQueryable().Count(pdb => pdb.CategorySlug == category.Slug);
                    foreach(var postDb in postDbs) {
                        var chapCount = _chapDbRepository.AsQueryable().Count(chap => chap.PostSlug == postDb.Slug);
                        var chapInfos = _chapDbRepository.AsQueryable().Skip((pageNumber - 1) * take).Take(take).ToList();

                        postInfos.Add(new PostInfo {
                            PostDb = postDb,
                            ChapCount = chapCount,
                            ChapInfos = chapInfos
                        });
                    }

                    result.Add(new CategoryInfo {
                        CategoryDb = category,
                        PostCount = postCount,
                        PostInfos = postInfos
                    });
                }
            }

            return result;
        }

        [HttpGet("count-total")]
        public async Task<CountSiteInfo> CountTotalSiteInfo(DatePreset? datePreset) {
            var result = new CountSiteInfo();

            if (datePreset != null) {
                var startDate = DateTime.Now.AddDays(-(int)datePreset);
                var endDate = DateTime.Now.AddDays(1);

                var filterCategory = Builders<CategoryDb>.Filter.And(
                    Builders<CategoryDb>.Filter.Gte(x => x.CreatedAt, startDate),
                    Builders<CategoryDb>.Filter.Lte(x => x.CreatedAt, endDate)
                );
                var filterPost = Builders<PostDb>.Filter.And(
                    Builders<PostDb>.Filter.Gte(x => x.CreatedAt, startDate),
                    Builders<PostDb>.Filter.Lte(x => x.CreatedAt, endDate)
                );
                var filterChap = Builders<ChapDb>.Filter.And(
                    Builders<ChapDb>.Filter.Gte(x => x.CreatedAt, startDate),
                    Builders<ChapDb>.Filter.Lte(x => x.CreatedAt, endDate)
                );

                result.Category = _categoryDbRepository.FilterBy(filterCategory).Count();
                result.Post = _postDbRepository.FilterBy(filterPost).Count();
                result.Chap = _chapDbRepository.FilterBy(filterChap).Count();
            } else {
                result.Category = _categoryDbRepository.AsQueryable().Count();
                result.Post = _postDbRepository.AsQueryable().Count();
                result.Chap = _chapDbRepository.AsQueryable().Count();
            }

            return result;
        }
    }
}

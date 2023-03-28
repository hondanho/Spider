using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using DotnetCrawler.Data.Models.Novel;
using DotnetCrawler.Data.Setting;

namespace DotnetCrawler.Data.AutoMap {

    public static class AutoMapperHelper {
        public static readonly IMapper Mapper;

        public static Mapper InitializeAutomapper() {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CategorySetting, CategoryDb>().ReverseMap();
                cfg.CreateMap<PostSetting, PostDb>().ReverseMap();
                cfg.CreateMap<ChapSetting, ChapDb>().ReverseMap();
            });

            var mapper = new Mapper(config);
            return mapper;
        }
    }

}

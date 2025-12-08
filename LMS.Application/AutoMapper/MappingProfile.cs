    using AutoMapper;
    using LMS.Application.DTOs.Books;
    using LMS.Domain.Entities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace LMS.Application.AutoMapper
    {
        public class MappingProfile : Profile
        {
            public MappingProfile()
            {

                CreateMap<Book, BookDTO>();

                CreateMap<CreateBookDTO, Book>()
                    .ForMember(b => b.Id, opt => opt.Ignore())
                    .ForMember(b => b.UserId, opt => opt.Ignore())
                    .ForMember(b => b.CreatedAt, opt => opt.Ignore())
                    .ForMember(b => b.UpdatedAt, opt => opt.Ignore())
                    .ForMember(b => b.IsDeleted, opt => opt.Ignore());

                CreateMap<UpdateBookDTO, Book>()
               .ForMember(b => b.UserId, opt => opt.Ignore()) 
               .ForMember(b => b.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow)) 
               .ForMember(b => b.IsDeleted, opt => opt.Ignore());


            CreateMap<Book, BookListDTO>();

        }

        }
    }

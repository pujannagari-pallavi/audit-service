using AutoMapper;
using AuditService.API.DTOs;
using AuditService.API.Models;
using AuditService.API.Enums;

namespace AuditService.API.Mappings
{
    public class AuditMappingProfile : Profile
    {
        public AuditMappingProfile()
        {
            CreateMap<Audit, AuditResponseDto>()
                .ForMember(dest => dest.AuditType, opt => opt.MapFrom(src => src.AuditType.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.AuditorUserIds, opt => opt.MapFrom(src => src.AuditAuditors.Select(aa => aa.UserId).ToList()));
            CreateMap<CreateAuditDto, Audit>()
                .ForMember(dest => dest.AuditId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.AuditAuditors, opt => opt.Ignore())
                .ForMember(dest => dest.AuditHistories, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    dest.Status = AuditStatus.Draft;
                    dest.CreatedAt = DateTime.UtcNow;
                    dest.IsDeleted = false;
                });
            CreateMap<UpdateAuditDto, Audit>()
                .ForMember(dest => dest.AuditId, opt => opt.Ignore())
                .ForMember(dest => dest.AuditCode, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.AuditAuditors, opt => opt.Ignore())
                .ForMember(dest => dest.AuditHistories, opt => opt.Ignore());


        }




    }
}

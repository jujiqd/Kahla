using AutoMapper;
using Newtonsoft.Json;
using System.Linq;

namespace Kahla.SDK.Models.ApiViewModels
{
    public class ContactInfo
    {
        public string DisplayName { get; set; }
        public string DisplayImagePath { get; set; }
        public Message LatestMessage { get; set; }
        public int UnReadAmount { get; set; }
        public int ConversationId { get; set; }
        public string Discriminator { get; set; }
        public string UserId { get; set; }
        public string AesKey { get; set; }
        public bool Muted { get; set; }
        public bool SomeoneAtMe { get; set; }
        public bool Online { get; set; }
        [JsonIgnore]
        public bool EnableInvisiable { get; set; }
        [JsonIgnore]
        public KahlaUser Sender { get; set; }
    }

    public class PrivateConversationProfile : Profile
    {

        public PrivateConversationProfile()
        {
            string userId = null;
            CreateMap<PrivateConversation, ContactInfo>()
                .ForMember(dest => dest.ConversationId,
                    opt => opt.MapFrom(t => t.Id))
                .ForMember(dest => dest.Discriminator,
                    opt => opt.MapFrom(t => nameof(PrivateConversation)))
                .ForMember(dest => dest.DisplayName,
                    opt => opt.MapFrom(t => userId == t.RequesterId ? t.TargetUser.NickName : t.RequestUser.NickName))
                .ForMember(dest => dest.DisplayImagePath,
                    opt => opt.MapFrom(t => userId == t.RequesterId ? t.TargetUser.IconFilePath : t.RequestUser.IconFilePath))
                .ForMember(dest => dest.UserId,
                    opt => opt.MapFrom(t => userId == t.RequesterId ? t.TargetId : t.RequesterId))
                .ForMember(dest => dest.UnReadAmount,
                    opt => opt.MapFrom(t => t.Messages.Count(p => !p.Read && p.SenderId != userId)))
                .ForMember(dest => dest.LatestMessage,
                    opt => opt.MapFrom(t => t.Messages.OrderByDescending(p => p.SendTime).FirstOrDefault()))
                .ForMember(dest => dest.Sender,
                    opt => opt.MapFrom(t => t.Messages.OrderByDescending(p => p.SendTime).Select(t => t.Sender).FirstOrDefault()))
                .ForMember(dest => dest.Muted,
                    opt => opt.MapFrom(t => false))
                .ForMember(dest => dest.AesKey,
                    opt => opt.MapFrom(t => t.AESKey))
                .ForMember(dest => dest.SomeoneAtMe,
                    opt => opt.MapFrom(t => false))
                .ForMember(dest => dest.EnableInvisiable,
                    opt => opt.MapFrom(t => userId == t.RequesterId ? t.TargetUser.EnableInvisiable : t.RequestUser.EnableInvisiable));

            CreateMap<GroupConversation, ContactInfo>()
                .ForMember(dest => dest.ConversationId,
                    opt => opt.MapFrom(t => t.Id))
                .ForMember(dest => dest.Discriminator,
                    opt => opt.MapFrom(t => nameof(GroupConversation)))
                .ForMember(dest => dest.DisplayName,
                    opt => opt.MapFrom(t => t.GroupName))
                .ForMember(dest => dest.DisplayImagePath,
                    opt => opt.MapFrom(t => t.GroupImagePath))
                .ForMember(dest => dest.UserId,
                    opt => opt.MapFrom(t => t.OwnerId))
                .ForMember(dest => dest.UnReadAmount,
                    opt => opt.MapFrom(t => t.Messages.Count(m => m.SendTime > t.Users.SingleOrDefault(u => u.UserId == userId).ReadTimeStamp)))
                .ForMember(dest => dest.LatestMessage,
                    opt => opt.MapFrom(t => t.Messages.OrderByDescending(p => p.SendTime).FirstOrDefault()))
                .ForMember(dest => dest.Sender,
                    opt => opt.MapFrom(t => t.Messages.OrderByDescending(p => p.SendTime).Select(t => t.Sender).FirstOrDefault()))
                .ForMember(dest => dest.Muted,
                    opt => opt.MapFrom(t => t.Users.SingleOrDefault(u => u.UserId == userId).Muted))
                .ForMember(dest => dest.AesKey,
                    opt => opt.MapFrom(t => t.AESKey))
                .ForMember(dest => dest.SomeoneAtMe,
                    opt => opt.MapFrom(t => t.Messages
                        .Where(m => m.SendTime > t.Users.SingleOrDefault(u => u.UserId == userId).ReadTimeStamp)
                        .Any(p => p.Ats.Any(k => k.TargetUserId == userId))))
                .ForMember(dest => dest.EnableInvisiable,
                    opt => opt.MapFrom(t => false));
        }
    }
}

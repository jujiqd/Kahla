﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using Kahla.SDK.Models;
using Kahla.SDK.Models.ApiViewModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Kahla.Server.Data
{
    public class ContactInfoProfile : Profile
    {
        public ContactInfoProfile()
        {
            string userId = null;
            CreateMap<Conversation, ContactInfo>()
                .Include<PrivateConversation, ContactInfo>()
                .Include<GroupConversation, ContactInfo>();
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

    public class KahlaDbContext : IdentityDbContext<KahlaUser>
    {
        private readonly IConfiguration _configuration;

        public KahlaDbContext(
            DbContextOptions<KahlaDbContext> options,
            IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<PrivateConversation> PrivateConversations { get; set; }
        public DbSet<GroupConversation> GroupConversations { get; set; }
        public DbSet<UserGroupRelation> UserGroupRelations { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<At> Ats { get; set; }

        public IQueryable<ContactInfo> MyContacts(string userId)
        {
            var config = new MapperConfiguration(t => t.AddProfile<ContactInfoProfile>());
            return Conversations
                .AsNoTracking()
                .Where(t => !(t is PrivateConversation) || ((PrivateConversation)t).RequesterId == userId || ((PrivateConversation)t).TargetId == userId)
                .Where(t => !(t is GroupConversation) || ((GroupConversation)t).Users.Any(p => p.UserId == userId))
                .ProjectTo<ContactInfo>(config, new { userId })
                .OrderByDescending(t => t.SomeoneAtMe)
                .ThenByDescending(t => t.LatestMessage == null ? DateTime.MinValue : t.LatestMessage.SendTime);
        }

        public async Task<UserGroupRelation> GetRelationFromGroup(string userId, int groupId)
        {
            return await UserGroupRelations
                .SingleOrDefaultAsync(t => t.UserId == userId && t.GroupId == groupId);
        }

        public Task<PrivateConversation> FindConversationAsync(string userId1, string userId2)
        {
            return PrivateConversations.Where(t =>
                    (t.RequesterId == userId1 && t.TargetId == userId2) ||
                    (t.RequesterId == userId2 && t.TargetId == userId1)).FirstOrDefaultAsync();
        }

        public async Task<bool> AreFriends(string userId1, string userId2)
        {
            return await FindConversationAsync(userId1, userId2) != null;
        }

        public async Task<int> RemoveFriend(string userId1, string userId2)
        {
            var relation = await PrivateConversations.SingleOrDefaultAsync(t => t.RequesterId == userId1 && t.TargetId == userId2);
            var belation = await PrivateConversations.SingleOrDefaultAsync(t => t.RequesterId == userId2 && t.TargetId == userId1);
            if (relation != null)
            {
                PrivateConversations.Remove(relation);
                return relation.Id;
            }
            if (belation != null)
            {
                PrivateConversations.Remove(belation);
                return belation.Id;
            }
            return -1;
        }

        public async Task<GroupConversation> CreateGroup(string groupName, string creatorId, string joinPassword)
        {
            var newGroup = new GroupConversation
            {
                GroupName = groupName,
                GroupImagePath = _configuration["GroupImagePath"],
                AESKey = Guid.NewGuid().ToString("N"),
                OwnerId = creatorId,
                JoinPassword = joinPassword ?? string.Empty
            };
            GroupConversations.Add(newGroup);
            await SaveChangesAsync();
            return newGroup;
        }

        public PrivateConversation AddFriend(string userId1, string userId2)
        {
            var conversation = new PrivateConversation
            {
                RequesterId = userId1,
                TargetId = userId2,
                AESKey = Guid.NewGuid().ToString("N")
            };
            PrivateConversations.Add(conversation);
            return conversation;
        }

        public async Task<DateTime> SetLastRead(Conversation conversation, string userId)
        {
            if (conversation is PrivateConversation)
            {
                var query = Messages
                    .Where(t => t.ConversationId == conversation.Id)
                    .Where(t => t.SenderId != userId);
                try
                {
                    return (await query
                        .Where(t => t.Read)
                        .OrderByDescending(t => t.SendTime)
                        .FirstOrDefaultAsync())
                        ?.SendTime ?? DateTime.MinValue;
                }
                finally
                {
                    await query
                        .Where(t => t.Read == false)
                        .ForEachAsync(t => t.Read = true);
                }
            }
            else if (conversation is GroupConversation)
            {
                var relation = await UserGroupRelations
                        .SingleOrDefaultAsync(t => t.UserId == userId && t.GroupId == conversation.Id);
                try
                {
                    return relation.ReadTimeStamp;
                }
                finally
                {
                    relation.ReadTimeStamp = DateTime.UtcNow;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}

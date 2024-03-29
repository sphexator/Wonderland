﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordHackWeek.Shared.Command;
using DiscordHackWeek.Interactive.Callbacks;
using DiscordHackWeek.Interactive.Criteria;
using Qmmands;

namespace DiscordHackWeek.Interactive.Paginator
{
    public class PaginatedMessageCallback : IReactionCallback
    {
        private readonly PaginatedMessage _pager;
        private readonly int pages;
        private int page = 1;


        public PaginatedMessageCallback(InteractiveService interactive,
            SocketCommandContext sourceContext,
            PaginatedMessage pager,
            ICriterion<SocketReaction> criterion = null)
        {
            Interactive = interactive;
            Context = sourceContext;
            Criterion = criterion ?? new EmptyCriterion<SocketReaction>();
            _pager = pager;
            pages = _pager.Pages.Count();
        }

        public InteractiveService Interactive { get; }
        public IUserMessage Message { get; private set; }

        private PaginatedAppearanceOptions options => _pager.Options;
        public SocketCommandContext Context { get; }

        public RunMode RunMode => RunMode.Parallel;
        public ICriterion<SocketReaction> Criterion { get; }

        public TimeSpan? Timeout => options.Timeout;

        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            var emote = reaction.Emote;

            if (emote.Equals(options.First))
            {
                page = 1;
            }
            else if (emote.Equals(options.Next))
            {
                if (page >= pages)
                    return false;
                ++page;
            }
            else if (emote.Equals(options.Back))
            {
                if (page <= 1)
                    return false;
                --page;
            }
            else if (emote.Equals(options.Last))
            {
                page = pages;
            }
            else if (emote.Equals(options.Stop))
            {
                await Message.DeleteAsync().ConfigureAwait(false);
                return true;
            }
            else if (emote.Equals(options.Jump))
            {
                _ = Task.Run(async () =>
                {
                    var criteria = new Criteria<SocketMessage>()
                        .AddCriterion(new EnsureSourceChannelCriterion())
                        .AddCriterion(new EnsureFromUserCriterion(reaction.UserId))
                        .AddCriterion(new EnsureIsIntegerCriterion());
                    var response = await Interactive.NextMessageAsync(Context, criteria, TimeSpan.FromSeconds(15));
                    var request = int.Parse(response.Content);
                    if (request < 1 || request > pages)
                    {
                        _ = response.DeleteAsync().ConfigureAwait(false);
                        await Interactive.ReplyAndDeleteAsync(Context, options.Stop.Name);
                        return;
                    }

                    page = request;
                    _ = response.DeleteAsync().ConfigureAwait(false);
                    await RenderAsync().ConfigureAwait(false);
                });
            }
            else if (emote.Equals(options.Info))
            {
                await Interactive.ReplyAndDeleteAsync(Context, options.InformationText, timeout: options.InfoTimeout);
                return false;
            }

            _ = Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            await RenderAsync().ConfigureAwait(false);
            return false;
        }

        public async Task DisplayAsync()
        {
            var embed = BuildEmbed();
            var message = await Context.Channel.SendMessageAsync(_pager.Content, embed: embed).ConfigureAwait(false);
            Message = message;
            Interactive.AddReactionCallback(message, this);
            // Reactions take a while to add, don't wait for them
            _ = Task.Run(async () =>
            {
                await message.AddReactionAsync(options.First);
                await message.AddReactionAsync(options.Back);
                await message.AddReactionAsync(options.Next);
                await message.AddReactionAsync(options.Last);

                var manageMessages = Context.Channel is IGuildChannel guildChannel
                    ? (Context.User as IGuildUser).GetPermissions(guildChannel).ManageMessages
                    : false;

                if (options.JumpDisplayOptions == JumpDisplayOptions.Always
                    || options.JumpDisplayOptions == JumpDisplayOptions.WithManageMessages && manageMessages)
                    await message.AddReactionAsync(options.Jump);

                await message.AddReactionAsync(options.Stop);

                if (options.DisplayInformationIcon)
                    await message.AddReactionAsync(options.Info);
            });
            // TODO: (Next major version) timeouts need to be handled at the service-level!
            if (Timeout.HasValue && Timeout.Value != null)
                _ = Task.Delay(Timeout.Value).ContinueWith(_ =>
                {
                    Interactive.RemoveReactionCallback(message);
                    _ = Message.DeleteAsync();
                });
        }

        protected Embed BuildEmbed()
        {
            var embed = new EmbedBuilder();
            embed.WithAuthor(_pager.Author);
            embed.WithColor(_pager.Color);
            if (_pager.Pages is IEnumerable<ImagePager> imgPages)
            {
                var index = imgPages.ElementAt(page - 1);
                embed.WithDescription(index.Content);
                embed.WithImageUrl(index.Image);
            }
            else embed.WithDescription(_pager.Pages.ElementAt(page - 1).ToString());
            embed.WithFooter(f => f.Text = string.Format(options.FooterFormat, page, pages));
            embed.WithTitle(_pager.Title);
            return embed.Build();
            /*
            return new EmbedBuilder()
                .WithAuthor(_pager.Author)
                .WithColor(_pager.Color)
                .WithDescription(_pager.Pages.ElementAt(page - 1).ToString())
                .WithFooter(f => f.Text = string.Format(options.FooterFormat, page, pages))
                .WithTitle(_pager.Title)
                .Build();
                */
        }

        private async Task RenderAsync()
        {
            var embed = BuildEmbed();
            await Message.ModifyAsync(m => m.Embed = embed).ConfigureAwait(false);
        }
    }
}
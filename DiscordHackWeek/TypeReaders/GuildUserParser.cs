﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordHackWeek.Shared.Command;
using Qmmands;

namespace DiscordHackWeek.TypeReaders
{
    public class GuildUserParser : Shared.Command.TypeParser<SocketGuildUser>
    {
        public override ValueTask<TypeParserResult<SocketGuildUser>> ParseAsync(Parameter parameter, string value,
    SocketCommandContext context, IServiceProvider provider)
        {
            if (MentionUtils.TryParseUser(value, out var id))
            {
                var user = context.Guild.GetUser(id);
                return user != null
                    ? TypeParserResult<SocketGuildUser>.Successful(user)
                    : TypeParserResult<SocketGuildUser>.Unsuccessful("Failed to parse user");
            }

            if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                var user = context.Guild.GetUser(id);
                return user != null
                    ? TypeParserResult<SocketGuildUser>.Successful(user)
                    : TypeParserResult<SocketGuildUser>.Unsuccessful("Failed to parse user");
            }

            var index = value.LastIndexOf('#');
            if (index >= 0)
            {
                var username = value.Substring(0, index);
                if (ushort.TryParse(value.Substring(index + 1), out var discriminator))
                {
                    var user = context.Guild.Users.FirstOrDefault(x => x.DiscriminatorValue == discriminator &&
                                                                       string.Equals(username, x.Username,
                                                                           StringComparison.OrdinalIgnoreCase));
                    return user != null
                        ? TypeParserResult<SocketGuildUser>.Successful(user)
                        : TypeParserResult<SocketGuildUser>.Unsuccessful("Failed to parse user");
                }
            }

            var userNick = context.Guild.Users.FirstOrDefault(x => x.Nickname == value);
            if (userNick != null) return TypeParserResult<SocketGuildUser>.Successful(userNick);

            var usernameParse = context.Guild.Users.FirstOrDefault(x => x.Username == value);
            return usernameParse != null
                ? TypeParserResult<SocketGuildUser>.Successful(usernameParse)
                : TypeParserResult<SocketGuildUser>.Unsuccessful("Failed to parse user");
        }
    }
}
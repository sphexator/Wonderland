﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordHackWeek.Shared.Command;
using Qmmands;

namespace DiscordHackWeek.TypeReaders
{
    public class RoleParser : Shared.Command.TypeParser<SocketRole>
    {
        public override ValueTask<TypeParserResult<SocketRole>> ParseAsync(Parameter parameter, string value,
            SocketCommandContext context, IServiceProvider provider)
        {
            if (MentionUtils.TryParseRole(value, out var id))
            {
                var role = context.Guild.GetRole(id);
                return role != null
                    ? TypeParserResult<SocketRole>.Successful(role)
                    : TypeParserResult<SocketRole>.Unsuccessful("Couldn't parse role");
            }

            if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                var role = context.Guild.GetRole(id);
                return role != null
                    ? TypeParserResult<SocketRole>.Successful(role)
                    : TypeParserResult<SocketRole>.Unsuccessful("Couldn't parse role");
            }

            var roleCheck = context.Guild.Roles.FirstOrDefault(x => x.Name == value);
            return roleCheck != null
                ? TypeParserResult<SocketRole>.Successful(roleCheck)
                : TypeParserResult<SocketRole>.Unsuccessful("Couldn't parse role");
        }
    }
}

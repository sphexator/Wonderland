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
    public class TextChannelParser : Shared.Command.TypeParser<SocketTextChannel>
    {
        public override ValueTask<TypeParserResult<SocketTextChannel>> ParseAsync(Parameter parameter, string value,
            SocketCommandContext context, IServiceProvider provider)
        {
            if (MentionUtils.TryParseChannel(value, out var id))
                return context.Guild.GetChannel(id) is SocketTextChannel txCh
                    ? TypeParserResult<SocketTextChannel>.Successful(txCh)
                    : TypeParserResult<SocketTextChannel>.Unsuccessful("Couldn't parse text channel");

            if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                return context.Guild.GetTextChannel(id) is SocketTextChannel txCh
                    ? TypeParserResult<SocketTextChannel>.Successful(txCh)
                    : TypeParserResult<SocketTextChannel>.Unsuccessful("Couldn't parse text channel");

            return context.Guild.TextChannels.FirstOrDefault(x => x.Name == value) is SocketTextChannel txChCheck
                ? TypeParserResult<SocketTextChannel>.Successful(txChCheck)
                : TypeParserResult<SocketTextChannel>.Unsuccessful("Couldn't parse text channel");
        }
    }
}

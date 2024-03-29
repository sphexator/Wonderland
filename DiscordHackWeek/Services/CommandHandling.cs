﻿using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordHackWeek.Entities;
using DiscordHackWeek.Shared.Command;
using Qmmands;

namespace DiscordHackWeek.Services
{
    public class CommandHandling : INService, IRequired
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _command;
        private readonly IServiceProvider _provider;

        public CommandHandling(DiscordSocketClient client, CommandService command, IServiceProvider provider)
        {
            _client = client;
            _command = command;
            _provider = provider;

            _client.MessageReceived += message =>
            {
                _ = _client_MessageReceived(message);
                return Task.CompletedTask;
            };
        }

        private async Task _client_MessageReceived(SocketMessage message)
        {
            if (!(message is SocketUserMessage msg)) return;
            if (msg.Author.IsBot) return;
            if (!CommandUtilities.HasPrefix(msg.Content, "-", StringComparison.CurrentCultureIgnoreCase,
                out var output)) return;
            await _command.ExecuteAsync(output, new SocketCommandContext(_client, msg, msg.Author), _provider);
        }
    }
}

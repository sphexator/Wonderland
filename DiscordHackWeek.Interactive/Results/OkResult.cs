﻿using Qmmands;

namespace DiscordHackWeek.Interactive.Results
{
    public class OkResult : CommandResult
    {
        public OkResult(string reason = null)
        {
        }

        public override bool IsSuccessful { get; }
    }
}
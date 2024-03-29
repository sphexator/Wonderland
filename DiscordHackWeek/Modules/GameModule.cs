﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordHackWeek.Entities;
using DiscordHackWeek.Entities.Combat;
using DiscordHackWeek.Extensions;
using DiscordHackWeek.Interactive;
using DiscordHackWeek.Interactive.Paginator;
using DiscordHackWeek.Services;
using DiscordHackWeek.Services.Combat;
using DiscordHackWeek.Services.Database;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace DiscordHackWeek.Modules
{
    [Name("Game")]
    public class GameModule : InteractiveBase
    {
        private readonly CombatHandling _combat;
        private readonly ImageHandling _image;
        public GameModule(CombatHandling combat, ImageHandling image)
        {
            _combat = combat;
            _image = image;
        }

        [Name("Search")]
        [Description("Searches for a game")]
        [Command("search")]
        public async Task PlayAsync()
        {
            using var db = new DbService();
            await _combat.SearchAsync(Context, db);
        }

        [Name("talent")]
        [Description("Spend a talent point into damage or heal")]
        [Command("talent")]
        public async Task TalentAsync(TalentType type = TalentType.Damage)
        {
            using var db = new DbService();
            var user = await db.Users.FindAsync(Context.User.Id);
            if (user == null) return;
            if (user.UnspentTalentPoints == 0)
            {
                await Context.ReplyAsync("No available talent points left to spend", Color.Red.RawValue);
                return;
            }

            if (type == TalentType.Damage)
            {
                user.DamageTalent++;
                await Context.ReplyAsync("Put a talent point into damage!");
            }
            else
            {
                user.HealthTalent++;
                await Context.ReplyAsync($"Put a talent point into health!");
            }
            await db.SaveChangesAsync();
        }

        [Name("GoTo")]
        [Description("Changes zone to a different zone by name")]
        [Command("goto", "travel")]
        [Priority(2)]
        public async Task TravelAsync([Remainder] string name)
        {
            using var db = new DbService();
            var zone = await db.Zones.FirstOrDefaultAsync(x => x.Name == name);
            if (zone == null)
            {
                await Context.ReplyAsync("Couldn't find a zone with that name!", Color.Red.RawValue);
                return;
            }

            var user = await db.Users.FindAsync(Context.User.Id);
            await Context.ReplyAsync($"Wanna move over to {zone.Name}? (y/n)");
            var response = await NextMessageAsync();
            if (response == null || response.Content.ToLower() != "y") return;
            user.ZoneId = zone.Id;
            await db.SaveChangesAsync();
            await Context.ReplyAsync($"Changed zone to {zone.Name}!");
        }

        [Name("GoTo")]
        [Description("Changes zone to a different zone by ID")]
        [Command("goto", "travel")]
        [Priority(1)]
        public async Task TravelAsync(int id)
        {
            using var db = new DbService();
            var zone = await db.Zones.FirstOrDefaultAsync(x => x.Id == id);
            if (zone == null)
            {
                await Context.ReplyAsync("Couldn't find a zone with that id!", Color.Red.RawValue);
                return;
            }

            var user = await db.Users.FindAsync(Context.User.Id);
            await Context.ReplyAsync($"Wanna move over to {zone.Name}? (y/n)");
            var response = await NextMessageAsync();
            if (response == null || response.Content.ToLower() != "y") return;
            user.ZoneId = zone.Id;
            await db.SaveChangesAsync();
            await Context.ReplyAsync($"Changed zone to {zone.Name}!");
        }

        [Name("Zones")]
        [Description("List of zones")]
        [Command("zones")]
        public async Task ZonesAsync()
        {
            using var db = new DbService();
            var zones = new List<ImagePager>();
            foreach (var x in db.Zones)
            {
                if(string.IsNullOrEmpty(x.ImageSrc)) continue;
                zones.Add(new ImagePager
                {
                    Image = x.ImageSrc,
                    Content = x.Description
                });
            }

            await PagedReplyAsync(new PaginatedMessage
            {
                Color = Color.Purple,
                Pages = zones
            });
        }

        [Name("Profile")]
        [Description("Display your, or a friends profile!")]
        [Command("Profile")]
        public async Task ProfileAsync(SocketGuildUser user = null)
        {
            using var db = new DbService();
            if (user == null) user = Context.User;
            var userData = await db.Users.FindAsync(Context.User.Id);
            if (user == null) return;
            await Context.Channel.TriggerTypingAsync();
            var img = await _image.ProfileBuilder(user, userData, await _combat.BuildCombatUserAsync(user, userData, db), db);
            img.Position = 0;
            await Context.Channel.SendFileAsync(img, "Profile.png");
        }

        [Name("Inventory")]
        [Description("Displays your profile")]
        [Command("inventory", "inv")]
        public async Task InventoryAsync()
        {
            using var db = new DbService();
            var inventory = await db.Inventories
                .Where(x => x.UserId == Context.User.Id).ToListAsync();
            if (inventory.Count == 0)
            {
                await Context.ReplyAsync("Your inventory is empty");
                return;
            }

            var result = new List<string>();
            for (var i = 0; i < inventory.Count; i++)
            {
                var x = inventory[i];
                var item = await db.Items.FirstOrDefaultAsync(z => z.Id == x.ItemId);
                if (item == null) continue;
                result.Add($"{item.Name} - Amount: {x.Amount}");
            }

            if (result.Count == 0)
            {
                await Context.ReplyAsync("Your inventory is empty");
                return;
            }

            await PagedReplyAsync(result.PaginateBuilder(Context.User, $"Inventory for {Context.User}", null));
        }
    }
}
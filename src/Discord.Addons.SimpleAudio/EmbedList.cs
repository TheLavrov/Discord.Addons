﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Addons.SimpleAudio
{
    internal sealed class EmbedList
    {
        internal const string SFirst = "\u23EE";
        internal const string SBack = "\u25C0";
        internal const string SNext = "\u25B6";
        internal const string SLast = "\u23ED";
        internal const string SDelete = "\u274C";

        private static IEmote EFirst { get; } = new Emoji(SFirst);
        private static IEmote EBack { get; } = new Emoji(SBack);
        private static IEmote ENext { get; } = new Emoji(SNext);
        private static IEmote ELast { get; } = new Emoji(SLast);
        private static IEmote EDelete { get; } = new Emoji(SDelete);

        internal IUserMessage Message { get; }
        private AudioService Service { get; }

        private readonly int _songsPerPage = 20;
        private readonly uint _totalPages;
        private uint _currentPage = 0;

        public EmbedList(IMessageChannel channel, AudioService service)
        {
            Service = service;

            _totalPages = (uint)Math.Ceiling((Service.GetAvailableFiles().Count() / (double)_songsPerPage));

            Message = channel.SendMessageAsync("", embed: GetPage(0)).GetAwaiter().GetResult();
            Task.Run(async () =>
            {
                await Message.AddReactionAsync(EFirst).ConfigureAwait(false);
                await Message.AddReactionAsync(EBack).ConfigureAwait(false);
                await Message.AddReactionAsync(ENext).ConfigureAwait(false);
                await Message.AddReactionAsync(ELast).ConfigureAwait(false);
                await Message.AddReactionAsync(EDelete).ConfigureAwait(false);
            }).GetAwaiter().GetResult();
        }

        private Embed GetPage(int page)
        {
            var songs = Service.GetAvailableFiles()
                .Skip(page * _songsPerPage)
                .Take(_songsPerPage)
                .Select(s => Path.GetFileNameWithoutExtension(s.Name));

            return new EmbedBuilder
            {
                Title = $"Page {page + 1} of {_totalPages}",
                Description = String.Join("\n", songs),
                Footer = new EmbedFooterBuilder
                {
                    Text = "You can use partial file names, but be as specific as possible."
                }
            }.Build();
        }

        public async Task First(IUser user)
        {
            await Message.RemoveReactionAsync(new Emoji(SFirst), user);
            if (_currentPage == 0) return;

            await Message.ModifyAsync(m => m.Embed = GetPage(0));

        }

        public async Task Next(IUser user)
        {
            await Message.RemoveReactionAsync(new Emoji(SNext), user);
            if (_currentPage == (_totalPages - 1)) return;

            await Message.ModifyAsync(m => m.Embed = GetPage((int)++_currentPage));
        }

        public async Task Back(IUser user)
        {
            await Message.RemoveReactionAsync(new Emoji(SBack), user);
            if (_currentPage == 0) return;

            await Message.ModifyAsync(m => m.Embed = GetPage((int)--_currentPage));
        }

        public async Task Last(IUser user)
        {
            await Message.RemoveReactionAsync(new Emoji(SLast), user);
            if (_currentPage == (_totalPages - 1)) return;

            await Message.ModifyAsync(m => m.Embed = GetPage((int)_totalPages - 1));
        }

        public Task Delete()
        {
            return Message.DeleteAsync();
        }
    }
}

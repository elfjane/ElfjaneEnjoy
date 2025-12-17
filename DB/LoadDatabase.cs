using ElfjaneEnjoy.AutoAnnouncer.Models;
using ElfjaneEnjoy.AutoAnnouncer.Parser;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ElfjaneEnjoy.DB
{
    internal class LoadDatabase
    {
        public static void LoadAllConfig()
        {
            LoadAutoAnnouncerMessagesConfig();
            LoadWordListConfig();
        }


        public static void LoadAutoAnnouncerMessagesConfig()
        {
            var json = File.ReadAllText(Path.Combine(Config.ConfigPath, "auto_announcer_messages.json"));
            var parser = new MessageParser();
            IEnumerable<AutoAnnouncerMessage> messages = parser.Parse(json);

            Database.clearAutoAnnouncerMessages();

            foreach (AutoAnnouncerMessage message in messages)
            {
                Database.addAutoAnnouncerMessages(message);
            }

        }

        public static void LoadWordListConfig()
        {
            var path = Path.Combine(Config.ConfigPath, "minigame_wordlist.json");
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var parser = new ElfjaneEnjoy.MiniGame.Parser.WordListParser();
            var words = parser.Parse(json);

            Database.clearWordList();

            foreach (var w in words)
            {
                Database.addWordEntry(w);
            }
        }
    }
}

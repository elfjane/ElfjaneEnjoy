using Bloody.Core.API.v1;
using Bloody.Core.GameData.v1;
using ElfjaneEnjoy.DB;
using System.Linq;
using VampireCommandFramework;


namespace ElfjaneEnjoy.Command
{
    [CommandGroup("elf")]
    internal class ElfjaneEnjoyCommand
    {
        [Command("online", "o", description: "To reload the configuration of the user messages online, offline or death of the VBlood boss", adminOnly: false)]
        public static void Online(ChatCommandContext ctx)
        {
            ctx.Reply("在線玩家:");
            foreach (var user in GameData.Users.Online.OrderBy(x => x.IsAdmin))
            {
                if (user.IsAdmin)
                {
                    ctx.Reply($"{FontColorChatSystem.Green("[管理者]")} {FontColorChatSystem.Yellow(user.CharacterName)}");
                }
                else
                {
                    ctx.Reply($"{FontColorChatSystem.Yellow(user.CharacterName)}");
                }
            }
        }


        [Command("reload", "rl", description: "To reload the configuration of the user messages online, offline or death of the VBlood boss", adminOnly: true)]
        public static void RealoadMod(ChatCommandContext ctx)
        {

            LoadDatabase.LoadAutoAnnouncerMessagesConfig();

            ctx.Reply("重新載入 ElfjaneEnjoy 模組.");

        }

        [Command("config", "cfg", usage: "[ auto, motd, newuser, online, offline, vblood ] true/false", description: "Enabled / Disabled the features of the mod. [ auto, motd, newuser, online, offline, vblood ]", adminOnly: true)]
        public static void ConfigMod(ChatCommandContext ctx, NotifyFeature feature, bool isEnabled)
        {

            Database.EnabledFeatures[feature] = isEnabled;

            var message = feature switch
            {
                NotifyFeature.auto => $"Auto Announcer:",
                NotifyFeature.minigame => $"MiniGame:",
                _ => throw new System.NotImplementedException(),
            };

            var enabled = FontColorChatSystem.Yellow(isEnabled ? "Enabled" : "Disabled");

            ctx.Reply(FontColorChatSystem.Green($"{message} {enabled}"));
        }

        [Command("answer", "ans", description: "Answer current MiniGame question", adminOnly: false)]
        public static void Answer(ChatCommandContext ctx, int answer)
        {
            if (!Database.EnabledFeatures.ContainsKey(NotifyFeature.minigame) || !Database.EnabledFeatures[NotifyFeature.minigame])
            {
                ctx.Reply("MiniGame 未啟用。");
                return;
            }
            var player = ctx.Event.SenderCharacterEntity;
            var correct = MiniGame.MiniGameFunction.TryAnswer(ctx, answer);

            if (correct)
            {
                ctx.Reply(FontColorChatSystem.Green("回答正確，恭喜！"));
            }
            else
            {
                ctx.Reply(FontColorChatSystem.Yellow("回答錯誤或目前沒有題目。"));
            }
        }

        [Command("answer_english", "e", description: "Answer current MiniGame question (文字答案)", adminOnly: false)]
        public static void Answer(ChatCommandContext ctx, string answer)
        {
            if (!Database.EnabledFeatures.ContainsKey(NotifyFeature.minigame) || !Database.EnabledFeatures[NotifyFeature.minigame])
            {
                ctx.Reply("MiniGame 未啟用。");
                return;
            }

            var correct = MiniGame.MiniGameFunction.TryEnglishAnswer(ctx, answer);

            if (correct)
            {
                ctx.Reply(FontColorChatSystem.Green("回答正確，恭喜！"));
            }
            else
            {
                ctx.Reply(FontColorChatSystem.Yellow("回答錯誤或目前沒有題目。"));
            }
        }

        [Command("minigame", "mg", usage: "start", description: "管理 MiniGame (start)", adminOnly: true)]
        public static void MiniGameAdmin(ChatCommandContext ctx, string action)
        {
            if (action == "start")
            {
                MiniGame.MiniGameFunction.ForceStart();
                ctx.Reply("已手動觸發 MiniGame 題目。");
            }
            else if (action == "word")
            {
                MiniGame.MiniGameFunction.ForceStartWord();
                ctx.Reply("已手動觸發 MiniGame 單字題目。");
            }
        }

    }
}

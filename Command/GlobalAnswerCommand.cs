using Bloody.Core.API.v1;
using Bloody.Core.GameData.v1;
using ElfjaneEnjoy.DB;
using System.Linq;
using VampireCommandFramework;

namespace ElfjaneEnjoy.Command
{
    internal class GlobalAnswerCommand
    {
        // 數字答案：.ans 5
        [Command("ans", description: "Answer current MiniGame question", adminOnly: false)]
        public static void Answer(ChatCommandContext ctx, int answer)
        {
            if (!Database.EnabledFeatures.ContainsKey(NotifyFeature.minigame) ||
                !Database.EnabledFeatures[NotifyFeature.minigame])
            {
                ctx.Reply("MiniGame 未啟用。");
                return;
            }

            var correct = MiniGame.MiniGameFunction.TryAnswer(ctx, answer);

            ctx.Reply(correct
                ? FontColorChatSystem.Green("回答正確，恭喜！")
                : FontColorChatSystem.Yellow("回答錯誤或目前沒有題目。"));
        }

        [Command("eng", description: "Answer current MiniGame question (文字答案)", adminOnly: false)]
        public static void AnswerEnglish(ChatCommandContext ctx, string answer)
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
    }
}

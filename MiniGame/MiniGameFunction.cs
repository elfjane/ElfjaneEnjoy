using Bloody.Core.API.v1;
using ElfjaneEnjoy.MiniGame.Models;
using ElfjaneEnjoy.DB;
using System;
using System.Collections;
using System.Collections.Generic;
using VampireCommandFramework;
using ProjectM;
using Unity.Collections;
using Unity.Entities;
using Stunlock.Core;
using ProjectM.Scripting;
using Bloody.Core;
using Bloody.Core.GameData.v1;

namespace ElfjaneEnjoy.MiniGame
{
    public class MiniGameFunction
    {
        private static readonly Random _rnd = new Random();

        private static bool _active = false;
        private static bool _waitingForTimeout = false;

        private static int _answer = 0;
        private static string _answerWord = null;
        private static bool _isWordGame = false;

        private static List<WordEntry> _wordList = null;

        private static string _question = "";
        private static int _level = 1;
        private static string _winner = null;

        private static int _timeLimitSeconds = 30;

        // ✅ Server-safe time (seconds since epoch)
        private static double _roundEndTime;

        private static bool _hint10Sent = false;
        private static bool _hint5Sent = false;


        // =============================
        // Entry
        // =============================
        public static void StartMiniGame()
        {
            static void MiniGameAction()
            {
                TickMiniGame();

                if (Database.EnabledFeatures.TryGetValue(NotifyFeature.minigame, out var enabled)
                    && enabled)
                {
                    if (!_active)
                    {
                        if (_rnd.Next(0, 2) == 0)
                            StartRound();
                        else
                            StartWordRound();
                    }
                }
            }

            CoroutineHandler.StartRepeatingCoroutine(
                MiniGameAction,
                Math.Max(10, Database.getIntervalMiniGame())
            );
        }

        // =============================
        // Math Round
        // =============================
        private static void StartRound()
        {
            int a, b;
            int op = _rnd.Next(0, 4);

            switch (op)
            {
                case 0:
                    a = _rnd.Next(1, 10);
                    b = _rnd.Next(1, 10);
                    _question = $"{a} + {b}";
                    _answer = a + b;
                    _level = 1;
                    break;

                case 1:
                    b = _rnd.Next(1, 10);
                    a = _rnd.Next(b, 11);
                    _question = $"{a} - {b}";
                    _answer = a - b;
                    _level = 1;
                    break;

                case 2:
                    a = _rnd.Next(1, 100);
                    b = _rnd.Next(1, 100);
                    _question = $"{a} + {b}";
                    _answer = a + b;
                    _level = 2;
                    break;

                case 3:
                    b = _rnd.Next(1, 100);
                    a = _rnd.Next(b, 101);
                    _question = $"{a} - {b}";
                    _answer = a - b;
                    _level = 2;
                    break;
            }

            _active = true;
            _waitingForTimeout = true;
            _winner = null;
            _isWordGame = false;

            StartTimeout();

            SendGreen($"[MiniGame] 題目: {_question} = ?");
            SendGreen($"回答請使用: .ans <數字>");
        }

        // =============================
        // Word Round
        // =============================
        private static void StartWordRound()
        {
            EnsureWordListLoaded();
            if (_wordList.Count == 0) return;

            var entry = _wordList[_rnd.Next(_wordList.Count)];
            _answerWord = entry.en.Trim();
            _isWordGame = true;
            _active = true;
            _waitingForTimeout = true;
            _winner = null;

            StartTimeout();

            var masked = MaskWord(_answerWord);

            SendGreen($"[MiniGame-單字] 中文意思: {entry.zh}");
            SendGreen($"填入缺失的英文: {masked}");
            SendGreen($"回答請使用: .eng <英文>");
        }

        // =============================
        // Answer
        // =============================
        public static bool TryAnswer(ChatCommandContext ctx, int ans)
        {
            if (!_active || !_waitingForTimeout || _winner != null)
                return false;

            if (ans != _answer)
                return false;

            HandleWinner(ctx.Event.SenderCharacterEntity, ans.ToString());
            return true;
        }

        public static bool TryEnglishAnswer(ChatCommandContext ctx, string ans)
        {
            if (!_active || !_isWordGame || _winner != null)
                return false;

            if (!string.Equals(ans?.Trim(), _answerWord, StringComparison.OrdinalIgnoreCase))
                return false;

            HandleWinner(ctx.Event.SenderCharacterEntity, _answerWord);
            return true;
        }

        // =============================
        // Timeout Tick (Server-safe)
        // =============================
        private static void TickMiniGame()
        {
            if (!_waitingForTimeout)
                return;

            double now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond;
            double remain = _roundEndTime - now;

            // ===== 倒數提示 =====
            if (!_hint10Sent && remain <= 10 && remain > 5)
            {
                _hint10Sent = true;
                SendGreen($"[MiniGame] ⏰ 剩下 10 秒！");
            }

            if (!_hint5Sent && remain <= 5 && remain > 0)
            {
                _hint5Sent = true;
                SendGreen($"[MiniGame] ⏰ 剩下 5 秒！");
            }

            // ===== 還沒結束 =====
            if (remain > 0)
                return;

            // ===== 時間到 =====
            _waitingForTimeout = false;

            if (!_active || _winner != null)
                return;

            _active = false;

            if (_isWordGame)
            {
                SendGreen($"[MiniGame-單字] ⌛ 時間到，答案是 {_answerWord}");

                _answerWord = null;
                _isWordGame = false;
            }
            else
            {
                SendGreen($"[MiniGame] ⌛ 時間到，答案是 {_answer}");
            }
        }

        // =============================
        // Helpers
        // =============================
        private static void StartTimeout()
        {
            double now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond;

            _roundEndTime = now + _timeLimitSeconds;

            _hint10Sent = false;
            _hint5Sent = false;
        }


        private static void HandleWinner(Entity playerEntity, string answer)
        {
            _winner = GetPlayerName(playerEntity);
            _active = false;
            _waitingForTimeout = false;

            SendGreen($"[MiniGame] 恭喜 {_winner} 答對！答案是 {answer}");

            GrantRewardToWinner(playerEntity, _winner);
        }

        private static void Send(FixedString512Bytes msg)
        {
            ServerChatUtils.SendSystemMessageToAllClients(
                Plugin.SystemsCore.EntityManager,
                ref msg
            );
        }

        private static void SendGreen(string msg)
        {
            FixedString512Bytes fixedString = new FixedString512Bytes(FontColorChatSystem.Green(msg));
            ServerChatUtils.SendSystemMessageToAllClients(
                Plugin.SystemsCore.EntityManager,
                ref fixedString
            );
        }

        private static string GetPlayerName(Entity playerEntity)
        {
            var em = Plugin.SystemsCore.EntityManager;
            var pc = em.GetComponentData<PlayerCharacter>(playerEntity);
            return pc.Name.ToString();
        }

        private static void EnsureWordListLoaded()
        {
            if (_wordList != null) return;
            _wordList = Database.getWordList() ?? new List<WordEntry>();
        }

        private static string MaskWord(string word)
        {
            if (string.IsNullOrEmpty(word) || word.Length <= 2)
                return word;

            var chars = word.ToCharArray();
            for (int i = 1; i < chars.Length - 1; i++)
                if (_rnd.NextDouble() > 0.4)
                    chars[i] = '_';

            return new string(chars);
        }

        private static void GrantRewardToWinner(Entity playerEntity, string playerName)
        {
            var rewardItem = Database.getMiniGameRewardItem();
            var rewardAmount = Database.getMiniGameRewardAmount() * _level;

            if (rewardItem == 0)
            {
                SendGreen($"[MiniGame] 備註: 未設定獎勵物品，請管理員手動發放。");
                return;
            }

            // Best-effort: try to find the player and give the item. Implementation depends on Bloody.Core API.
            // If not possible, inform admins how to give the reward manually.

            PrefabGUID rewardItemGuid = new PrefabGUID(rewardItem);
            //var lineUser = (FixedString512Bytes)$"你答對了！獲得 {rewardItemName}x{rewardAmount}，請查看背包。";
            string rewardItemName = GetPrefabDisplayName(rewardItemGuid) ?? rewardItemGuid.ToString();
            GiveToUser(playerEntity, rewardItemGuid, rewardAmount);
            SendGreen($"{playerName} 答對了！獲得 水晶x{rewardAmount}。");
        }
        private static string GetPrefabDisplayName(PrefabGUID guid)
        {
            try
            {
                var gdType = typeof(Bloody.Core.GameData.v1.GameData);
                var props = new[] { "Items", "Prefabs", "Prototypes" };

                foreach (var p in props)
                {
                    var prop = gdType.GetProperty(p);
                    if (prop == null) continue;

                    var collection = prop.GetValue(null) as IEnumerable;
                    if (collection == null) continue;

                    foreach (var item in collection)
                    {
                        var itemType = item.GetType();
                        var guidProp = itemType.GetProperty("Prefab") ?? itemType.GetProperty("PrefabGUID") ?? itemType.GetProperty("GUID") ?? itemType.GetProperty("PrefabGuid");
                        if (guidProp == null) continue;

                        var val = guidProp.GetValue(item);
                        if (val == null) continue;

                        if (val.Equals(guid))
                        {
                            var nameProp = itemType.GetProperty("LocalizedName") ?? itemType.GetProperty("DisplayName") ?? itemType.GetProperty("Name") ?? itemType.GetProperty("CharacterName") ?? itemType.GetProperty("LocalizationKey");
                            var name = nameProp?.GetValue(item)?.ToString();
                            if (!string.IsNullOrEmpty(name)) return name;
                        }
                    }
                }
            }
            catch { }

            return null;
        }
        public static void GiveItem(Entity playerEntity, PrefabGUID itemGuid, int amount)
        {
            UserSystem.TryAddInventoryItemOrDrop(playerEntity, itemGuid, amount);

        }
        public static void GiveToUser(Entity userEntity, PrefabGUID itemGuid, int amount)
        {
            GiveItem(userEntity, itemGuid, amount);
        }
        // Admin helper to force start a round
        public static void ForceStart()
        {
            if (!_active)
            {
                StartRound();
            }
        }

        public static void ForceStartWord()
        {
            if (!_active)
            {
                StartWordRound();
            }
        }
    }
}

using Bloody.Core.API.v1;
using ElfjaneEnjoy.MiniGame.Models;
using ElfjaneEnjoy.DB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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
        private static int _answer = 0;
        private static string _answerWord = null;
        private static bool _isWordGame = false;
        private static List<WordEntry> _wordList = null;
        private static string _question = "";
        private static int _level = 1;
        private static string _winner = null;
        private static int _timeLimitSeconds = 30;

        public static void StartMiniGame()
        {
            static void MiniGameAction()
            {
                if (Database.EnabledFeatures.ContainsKey(NotifyFeature.minigame) && Database.EnabledFeatures[NotifyFeature.minigame])
                {
                    if (!_active)
                    {
                        // 隨機選擇一個遊戲
                        if (_rnd.Next(0, 2) == 0)
                            StartRound();      // 算術題
                        else
                            StartWordRound();  // 單字填空
                    }
                }
            }

            CoroutineHandler.StartRepeatingCoroutine(MiniGameAction, Math.Max(10, Database.getIntervalMiniGame()));
        }

        private static void StartRound()
        {
            // generate a simple 1~2 digit math question
            int a, b;
            int op = _rnd.Next(0, 4); // 0:+ 1:- 2:*

            switch (op)
            {
                case 0: // 加法
                    a = _rnd.Next(1, 10); // 1-99
                    b = _rnd.Next(1, 10);
                    _question = $"{a} + {b}";
                    _answer = a + b;
                    _level = 1;
                    break;
                case 1: // 減法
                    b = _rnd.Next(1, 10);
                    a = _rnd.Next(b, 11); // a >= b
                    _question = $"{a} - {b}";
                    _answer = a - b;
                    _level = 1;
                    break;
                case 2: // 加法 (兩位數)
                    a = _rnd.Next(1, 100); // 1-99
                    b = _rnd.Next(1, 100);
                    _question = $"{a} + {b}";
                    _answer = a - b;
                    _level = 2;
                    break;
                case 3: // 減法 (兩位數)
                    b = _rnd.Next(1, 100);
                    a = _rnd.Next(b, 101); // a >= b
                    _question = $"{a} - {b}";
                    _answer = a - b;
                    _level = 2;
                    break;
                default: // 乘法
                    a = _rnd.Next(1, 10); // 1-99
                    b = _rnd.Next(1, 10);
                    _question = $"{a} * {b}";
                    _answer = a * b;
                    _level = 5;
                    break;
            }

            _active = true;
            _winner = null;

            var line1 = (FixedString512Bytes)$"[MiniGame] 題目: {_question} = ? (第一個答對者得獎)";
            var line2 = (FixedString512Bytes)$"回答請使用: .elf ans <數字>";

            ServerChatUtils.SendSystemMessageToAllClients(Plugin.SystemsCore.EntityManager, ref line1);
            ServerChatUtils.SendSystemMessageToAllClients(Plugin.SystemsCore.EntityManager, ref line2);

            // start timeout
            _ = EndRoundAfterTimeoutAsync(_timeLimitSeconds);
        }

        private static async Task EndRoundAfterTimeoutAsync(int seconds)
        {
            await Task.Delay(seconds * 1000);

            if (_active && _winner == null)
            {
                _active = false;
                if (_isWordGame)
                {
                    var line = (FixedString512Bytes)$"[MiniGame-單字] 時間到，沒有人答對。答案是 {_answerWord}.";
                    ServerChatUtils.SendSystemMessageToAllClients(Plugin.SystemsCore.EntityManager, ref line);
                    _isWordGame = false;
                    _answerWord = null;
                }
                else
                {
                    var line = (FixedString512Bytes)$"[MiniGame] 時間到，沒有人答對。答案是 {_answer}.";
                    ServerChatUtils.SendSystemMessageToAllClients(Plugin.SystemsCore.EntityManager, ref line);
                }
            }
        }

        public static bool TryAnswer(ChatCommandContext ctx, int ans)
        {
            if (!_active) return false;

            if (_winner != null) return false;

            if (ans == _answer)
            {
                Entity playerEntity = ctx.Event.SenderCharacterEntity;
                string playerName = GetPlayerNameFromContext(playerEntity);
                _winner = playerName ?? "Unknown";
                _active = false;

                var line = (FixedString512Bytes)$"[MiniGame] 恭喜 {_winner} 答對了! 答案是 {_answer}.";
                ServerChatUtils.SendSystemMessageToAllClients(Plugin.SystemsCore.EntityManager, ref line);

                // grant reward (best-effort)

                GrantRewardToWinner(playerEntity, playerName);

                return true;
            }

            return false;
        }

        public static bool TryEnglishAnswer(ChatCommandContext ctx, string ans)
        {
            if (!_active) return false;

            if (_winner != null) return false;

            if (!_isWordGame) return false;

            if (string.Equals(ans?.Trim(), _answerWord, StringComparison.OrdinalIgnoreCase))
            {
                Entity playerEntity = ctx.Event.SenderCharacterEntity;
                string playerName = GetPlayerNameFromContext(playerEntity);
                _winner = playerName ?? "Unknown";
                _active = false;

                var line = (FixedString512Bytes)$"[MiniGame-單字] 恭喜 {_winner} 答對了! 答案是 {_answerWord}.";
                ServerChatUtils.SendSystemMessageToAllClients(Plugin.SystemsCore.EntityManager, ref line);

                GrantRewardToWinner(playerEntity, playerName);

                _isWordGame = false;
                _answerWord = null;

                return true;
            }

            return false;
        }

        private static void EnsureWordListLoaded()
        {
            if (_wordList != null) return;

            // Load from Database which is populated by LoadDatabase.LoadWordListConfig
            try
            {
                _wordList = Database.getWordList();
                if (_wordList == null) _wordList = new List<WordEntry>();
            }
            catch
            {
                _wordList = new List<WordEntry>();
            }
        }

        private static string MaskWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return word;
            word = word.Trim();
            if (word.Length <= 2) return word;

            var chars = word.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (i == 0 || i == chars.Length - 1) continue; // keep first and last
                if (!char.IsLetter(chars[i])) continue;

                // reveal some letters randomly (~40% chance)
                if (_rnd.NextDouble() < 0.4)
                {
                    // keep
                }
                else
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        private static void StartWordRound()
        {
            EnsureWordListLoaded();
            if (_wordList == null || _wordList.Count == 0) return;

            var entry = _wordList[_rnd.Next(0, _wordList.Count)];
            _answerWord = entry.en.Trim();
            _isWordGame = true;
            _active = true;
            _winner = null;

            var masked = MaskWord(_answerWord);

            var line1 = (FixedString512Bytes)$"[MiniGame-單字] 中文意思: {entry.zh}";
            var line2 = (FixedString512Bytes)$"填入缺失的英文: {masked}";
            var line3 = (FixedString512Bytes)$"回答請使用: .elf e <英文>";

            ServerChatUtils.SendSystemMessageToAllClients(Plugin.SystemsCore.EntityManager, ref line1);
            ServerChatUtils.SendSystemMessageToAllClients(Plugin.SystemsCore.EntityManager, ref line2);
            ServerChatUtils.SendSystemMessageToAllClients(Plugin.SystemsCore.EntityManager, ref line3);

            _ = EndRoundAfterTimeoutAsync(_timeLimitSeconds);
        }

        private static string GetPlayerNameFromContext(Entity playerEntity)
        {
            var entityManager = Plugin.SystemsCore.EntityManager;

            if (!entityManager.HasComponent<PlayerCharacter>(playerEntity))
            {
                // 理論上不會發生，保險用
                return "未知玩家";
            }

            var playerCharacter = entityManager.GetComponentData<PlayerCharacter>(playerEntity);

            // 角色名稱
            string playerName = playerCharacter.Name.ToString();
            return playerName;
        }

        private static void GrantRewardToWinner(Entity playerEntity, string playerName)
        {
            var rewardItem = Database.getMiniGameRewardItem();
            var rewardAmount = Database.getMiniGameRewardAmount() * _level;

            if (rewardItem == 0)
            {
                var line = (FixedString512Bytes)$"[MiniGame] 備註: 未設定獎勵物品，請管理員手動發放。";
                ServerChatUtils.SendSystemMessageToAllClients(Plugin.SystemsCore.EntityManager, ref line);
                return;
            }

            // Best-effort: try to find the player and give the item. Implementation depends on Bloody.Core API.
            // If not possible, inform admins how to give the reward manually.

            PrefabGUID rewardItemGuid = new PrefabGUID(rewardItem);
            var rewardItemName = GetPrefabDisplayName(rewardItemGuid) ?? rewardItemGuid.ToString();
            var lineAdmin = (FixedString512Bytes)$"{playerName} 答對了！獲得 水晶x{rewardAmount}。";
            //var lineUser = (FixedString512Bytes)$"你答對了！獲得 {rewardItemName}x{rewardAmount}，請查看背包。";
            GiveToUser(playerEntity, rewardItemGuid, rewardAmount);
            ServerChatUtils.SendSystemMessageToAllClients(Plugin.SystemsCore.EntityManager, ref lineAdmin);
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

        public static void GiveItem(Entity playerEntity, PrefabGUID itemGuid, int amount)
        {
            UserSystem.TryAddInventoryItemOrDrop(playerEntity, itemGuid, amount);

        }
        public static void GiveToUser(Entity userEntity, PrefabGUID itemGuid, int amount)
        {
            GiveItem(userEntity, itemGuid, amount);
        }

    }
}

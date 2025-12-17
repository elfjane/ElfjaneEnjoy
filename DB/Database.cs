using ElfjaneEnjoy.AutoAnnouncer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElfjaneEnjoy.DB
{
    internal class Database
    {
        private static int IntervalAutoAnnouncer = 0;
        private static List<AutoAnnouncerMessage> AutoAnnouncerMessages = new List<AutoAnnouncerMessage>();
        private static int IntervalMiniGame = 0;
        private static int MiniGameRewardItem = 0;
        private static int MiniGameRewardAmount = 1;
        private static List<ElfjaneEnjoy.MiniGame.Models.WordEntry> MiniGameWordList = new List<ElfjaneEnjoy.MiniGame.Models.WordEntry>();

        static Database()
        {
            setAllFeatures(false);
        }

        internal static Dictionary<NotifyFeature, bool> EnabledFeatures = new();

        internal static void setAllFeatures(bool isEnabled)
        {
            foreach (NotifyFeature feature in System.Enum.GetValues(typeof(NotifyFeature)))
            {
                EnabledFeatures[feature] = isEnabled;
            }
        }

        public static int getIntervalAutoAnnouncer()
        {
            return IntervalAutoAnnouncer;
        }

        public static void setIntervalAutoAnnouncer(int intervalAutoAnnouncer)
        {
            IntervalAutoAnnouncer = intervalAutoAnnouncer;
        }

        public static int getIntervalMiniGame()
        {
            return IntervalMiniGame;
        }

        public static void setIntervalMiniGame(int interval)
        {
            IntervalMiniGame = interval;
        }

        public static int getMiniGameRewardItem()
        {

            return MiniGameRewardItem;
        }

        public static int getMiniGameRewardAmount()
        {
            return MiniGameRewardAmount;
        }

        public static void setMiniGameReward(int item, int amount)
        {
            MiniGameRewardItem = item;
            MiniGameRewardAmount = amount;
        }

        public static List<AutoAnnouncerMessage> getAutoAnnouncerMessages()
        {
            return AutoAnnouncerMessages;
        }

        public static void addAutoAnnouncerMessages(AutoAnnouncerMessage autoAnnouncerMessages)
        {
            AutoAnnouncerMessages.Add(autoAnnouncerMessages);
        }
        public static void clearAutoAnnouncerMessages()
        {
            AutoAnnouncerMessages.Clear();
        }

        public static List<ElfjaneEnjoy.MiniGame.Models.WordEntry> getWordList()
        {
            return MiniGameWordList;
        }

        public static void addWordEntry(ElfjaneEnjoy.MiniGame.Models.WordEntry entry)
        {
            MiniGameWordList.Add(entry);
        }

        public static void clearWordList()
        {
            MiniGameWordList.Clear();
        }
    }
}

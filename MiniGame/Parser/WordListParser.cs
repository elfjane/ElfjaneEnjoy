using ElfjaneEnjoy.MiniGame.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ElfjaneEnjoy.MiniGame.Parser
{
    public class WordListParser
    {
        public IEnumerable<WordEntry> Parse(string content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (string.IsNullOrWhiteSpace(content)) yield break;

            var arr = JsonSerializer.Deserialize<WordEntry[]>(content);
            if (arr == null) yield break;

            foreach (var e in arr)
            {
                yield return e;
            }
        }
    }
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SenAnAPI.HostedServices
{
    public class SenAnHostedService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<SenAnHostedService> _logger;
        private Timer _timer;
        private readonly SQLiteConnection _SqliteConnection;
        const string Absolute_dbpath = @"C:\Projects\german_sentence_analyzer\LANG_DB_DE.db";

        public SenAnHostedService(ILogger<SenAnHostedService> logger)
        {
            _logger = logger;
            _SqliteConnection = CreateConnection();
        }

        static SQLiteConnection CreateConnection()
        {
            SQLiteConnection sqlite_conn;
            // Create a new database connection:
            sqlite_conn = new SQLiteConnection($"Data Source={Absolute_dbpath}; Version = 3; New = False; Compress = True; ");

            // Open the connection:
            sqlite_conn.Open();
            
            return sqlite_conn;
        }

        public int GetCount()
        {
            return executionCount;
        }

        private string CapitalizeFirstChar(string word)
        {
            if (word.Length == 0)
                return word;
            else if (word.Length == 1)
                return char.ToUpper(word[0]).ToString();
            else
                return char.ToUpper(word[0]).ToString() + word.Substring(1);
        }

        private Tree<string> CheckTableForEntry(string wortForm, string tableName)
        {
            if (string.IsNullOrEmpty(wortForm) || string.IsNullOrWhiteSpace(wortForm)) return null;

            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = _SqliteConnection.CreateCommand();
            sqlite_cmd.CommandText = $@"
            SELECT *
            FROM {tableName}
            WHERE Wortform = @Param1 or Wortform = @Param2
            ";
            sqlite_cmd.Parameters.AddWithValue("@Param1", wortForm.ToLower());
            sqlite_cmd.Parameters.AddWithValue("@Param2", CapitalizeFirstChar(wortForm.ToLower()));

            sqlite_datareader = sqlite_cmd.ExecuteReader();

            Tree<string> retVal = new Tree<string>(tableName);

            while (sqlite_datareader.Read())
            {
                // Worttyp: tableName
                // ID: sqlite_datareader.GetDecimal(0)
                // Lemma: sqlite_datareader.GetString(1)
                // Detail: sqlite_datareader.GetString(2)
                // Wortform: sqlite_datareader.GetString(3)
                retVal.TryAddSubTree(sqlite_datareader.GetString(2));
            }

            return retVal;
        }

        private Tree<string> CheckForEntryInAllTables(string singleWord)
        {
            if (string.IsNullOrEmpty(singleWord) || string.IsNullOrWhiteSpace(singleWord))
                return null;

            Tree<string> wordTree = new Tree<string>(singleWord);

            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Abkürzung"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Adjektiv"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Adposition"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Adverb"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Affix"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Artikel"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Formel"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Gebundenes_Lexem"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Geflügeltes_Wort"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Konjunktion"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Kontraktion"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Merkspruch"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Numerale"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Ortsnamengrundwort"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Partikel"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Pronomen"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Redewendung"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Sprichwort"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Substantiv"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Verb"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Wortverbindung"));
            wordTree.TryAddFilledSubTree(CheckTableForEntry(singleWord, "Satzzeichen"));

            if(wordTree.CountSubTrees() == 0)
            {
                wordTree.TryAddSubTree("Unbekannt");
            }

            return wordTree;
        }

        public class Tree<K>
        {
            private readonly K _Key;
            private readonly List<Tree<K>> _SubTrees;

            public Tree(K key)
            {
                _Key = key;
                _SubTrees = new List<Tree<K>>();
            }

            public int TryAddSubTree(K key)
            {
                _SubTrees.Add(new Tree<K>(key));

                return _SubTrees.Count - 1;
            }

            public int TryAddSubTree(Tree<K> existingSubTree)
            {
                if (existingSubTree == null) return -1;

                _SubTrees.Add(existingSubTree);

                return _SubTrees.Count - 1;
            }

            public int TryAddFilledSubTree(Tree<K> existingSubTree)
            {
                if (existingSubTree == null) return -1;
                if (existingSubTree.CountSubTrees() == 0) return -1;

                _SubTrees.Add(existingSubTree);

                return _SubTrees.Count - 1;
            }

            public Tree<K> GetSubTree(int subTreeIndex)
            {
                return _SubTrees[subTreeIndex];
            }

            public override string ToString()
            {
                string retVal = "";

                retVal += @$"{JsonSerializer.Serialize(_Key.ToString())}:" + "{";
                foreach(var singleSubTree in _SubTrees)
                {
                    retVal += singleSubTree.ToString();
                    retVal += ",";
                }
                retVal = retVal.TrimEnd(',');
                retVal += "}";
                
                return retVal;
            }

            public int CountSubTrees()
            {
                return _SubTrees.Count;
            }
        }

        private Tree<string> ProcessSentence(string sentence)
        {
            Tree<string> sentenceTree = new Tree<string>(sentence);
            string pattern = "([\\.!\\? ,\"\\\\(\\)<>])";
            string[] substrings = Regex.Split(sentence, pattern);
            foreach (var singleWord in substrings)
            {
                sentenceTree.TryAddFilledSubTree(CheckForEntryInAllTables(singleWord));
            }

            return sentenceTree;
        }

        public string ReadData(string text)
        {
            Tree<string> textTree = new Tree<string>("text");

            string pattern = "([\\.!\\?])";
            string[] pattern_raw = new string[] { ".", "!", "?" };

            string[] substrings = Regex.Split(text, pattern);    // Split on hyphens
            List<string> sentences = new List<string>();
            foreach (var sentence in substrings)
            {
                if (string.IsNullOrEmpty(sentence) || string.IsNullOrWhiteSpace(sentence))
                    continue;

                var patternMatch = "";

                foreach(var single_pattern in pattern_raw)
                {
                    if(string.Equals(sentence, single_pattern))
                    {
                        patternMatch = single_pattern;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(patternMatch) && !string.IsNullOrWhiteSpace(patternMatch) && sentences.Count > 0)
                {
                    sentences[sentences.Count - 1] += sentence;
                }
                else
                {
                    sentences.Add(sentence);
                }
            }

            foreach (var sentence in sentences)
            {
                textTree.TryAddFilledSubTree(ProcessSentence(sentence));
            }

            return "{" + textTree.ToString() + "}";
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            executionCount++;

            _logger.LogInformation(
                "Timed Hosted Service is working. Count: {Count}", executionCount);
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            _SqliteConnection.Close();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}

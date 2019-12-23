using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SQLite;

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

        public string ReadData(string singleWord)
        {
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            string tableName = "Verb";
            sqlite_cmd = _SqliteConnection.CreateCommand();
            sqlite_cmd.CommandText = $@"
            SELECT *
            FROM {tableName}
            WHERE Wortform = @Param1
            ";
            sqlite_cmd.Parameters.AddWithValue("@Param1", singleWord);

            sqlite_datareader = sqlite_cmd.ExecuteReader();

            string retVal = "";

            while (sqlite_datareader.Read())
            {
                retVal += "(" + sqlite_datareader.GetDecimal(0) + "|" + sqlite_datareader.GetString(1) + "|" + sqlite_datareader.GetString(2) + "|" + sqlite_datareader.GetString(3) + ")" + "\n";
            }
            
            return retVal;
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

using DevOps.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace DevOps.Util.DotNet
{
    public sealed class GeneralUtil : IDisposable
    {
        public ILogger Logger { get; }
        public SqlConnection SqlConnection { get; }

        public GeneralUtil(string sqlConnectionString, ILogger logger = null)
        {
            Logger = logger ?? Util.CreateConsoleLogger();
            SqlConnection = new SqlConnection(sqlConnectionString);
        }

        public void Dispose()
        {
            SqlConnection.Dispose();
        }

        public async Task UploadBuildEvent(int buildId, string content)
        {
            await SqlConnection.EnsureOpenAsync().ConfigureAwait(false);
            using var command = SqlConnection.CreateCommand();
            command.CommandText = "INSERT INTO dbo.BuildEvent (BuildId, Content) VALUES (@BuildId, @Content)";
            command.Parameters.AddWithValue("@BuildId", buildId);
            command.Parameters.Add(getCompressedData());
            var result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            if (result < 0)
            {
                Logger.LogError($"Unable to upload the data");
            }

            SqlParameter getCompressedData()
            {
                using var memoryStream = new MemoryStream();
                using var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal);
                var inputBytes = Encoding.UTF8.GetBytes(content);
                gzipStream.Write(inputBytes, 0, inputBytes.Length);
                gzipStream.Flush();
                var bytes = memoryStream.ToArray();

                var parameter = new SqlParameter("@Content", SqlDbType.VarBinary, bytes.Length);
                parameter.Value = bytes;
                return parameter;
            }
        }

        public async Task DumpBuildEvents()
        {
            await SqlConnection.EnsureOpenAsync().ConfigureAwait(false);
            using var command = SqlConnection.CreateCommand();
            command.CommandText = "SELECT * FROM dbo.BuildEvent";
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (reader.Read())
            {
                var buildId = reader.GetInt32(0);
                var compressedConent = reader.GetSqlBinary(1);
                using var memoryStream = new MemoryStream();
                memoryStream.Write(compressedConent.Value, 0, compressedConent.Length);
                memoryStream.Position = 0;
                using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress, leaveOpen: true);
                using var streamReader = new StreamReader(gzipStream, Encoding.UTF8);
                var text = streamReader.ReadToEnd();
                Logger.LogInformation($"{buildId}: {text}");
            }
        }
    }
}

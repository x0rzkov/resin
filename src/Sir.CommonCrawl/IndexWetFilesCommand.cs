﻿using Microsoft.Extensions.Logging;
using Sir.Search;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Sir.CommonCrawl
{
    public class IndexWetFilesCommand : ICommand
    {
        public void Run(IDictionary<string, string> args, ILogger logger)
        {
            var fileName = args["fileName"];
            var model = new TextModel();
            var collectionId = "cc_wet".ToHash();
            var storedFieldNames = new HashSet<string> { "url" };
            var indexedFieldNames = new HashSet<string> { "description" };

            var writeJob = new WriteJob(
                collectionId,
                ReadWetFile(fileName),
                model,
                storedFieldNames,
                indexedFieldNames);

            using (var sessionFactory = new SessionFactory(new KeyValueConfiguration("sir.ini"), logger))
            {
                sessionFactory.Truncate(collectionId);

                sessionFactory.Write(writeJob, reportSize: 1000);
            }
        }

        private static IEnumerable<IDictionary<string, object>> ReadWetFile(string fileName)
        {
            const string uriLabel = "WARC-Target-URI: ";
            const string contentLabel = "Content-Length: ";
            const string contentEndLabel = "WARC/1.0";

            string url = null;
            var content = new StringBuilder();
            bool isContent = false;

            var lines = ReadAllLinesFromGz(fileName).Skip(15);

            foreach (var line in lines)
            {
                if (isContent)
                {
                    if (line.Length > 0)
                        content.AppendLine(line);
                }

                if (line.StartsWith(contentEndLabel))
                {
                    isContent = false;

                    if (content.Length > 0)
                    {
                        yield return new Dictionary<string, object>
                    {
                        { "url", url},
                        { "description", content.ToString() }
                    };

                        content = new StringBuilder();
                    }
                }
                else if (line.StartsWith(uriLabel))
                {
                    url = line.Replace(uriLabel, "");
                }
                else if (line.StartsWith(contentLabel))
                {
                    isContent = true;
                }
            }
        }

        private static IEnumerable<string> ReadAllLinesFromGz(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            using (var zip = new GZipStream(stream, CompressionMode.Decompress))
            using (var reader = new StreamReader(zip))
            {
                var line = reader.ReadLine();

                while (line != null)
                {
                    yield return line;

                    line = reader.ReadLine();
                }
            }
        }
    }
}

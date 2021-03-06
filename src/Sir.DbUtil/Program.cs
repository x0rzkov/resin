﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sir.Search;

namespace Sir.DbUtil
{
    class Program
    {
        static void Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("Sir.DbUtil.Program", LogLevel.Information)
                    .AddConsole()
                    .AddDebug();
            });

            var logger = loggerFactory.CreateLogger("dbutil");

            logger.LogInformation($"processing command: {string.Join(" ", args)}");

            var model = new TextModel();
            var command = args[0].ToLower();
            var flags = ParseArgs(args);
            var plugin = ResolvePlugin(command);

            if (plugin != null)
            {
                try
                {
                    plugin.Run(flags, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
            }
            else if ((command == "slice"))
            {
                Slice(flags);
            }
            else if (command == "truncate")
            {
                Truncate(flags["collection"], logger);
            }
            else if (command == "truncate-index")
            {
                TruncateIndex(flags["collection"], logger);
            }
            else if (command == "optimize")
            {
                Optimize(flags, model, logger);
            }
            else
            {
                logger.LogInformation("unknown command: {0}", command);
            }

            logger.LogInformation($"executed {command}");
        }

        private static ICommand ResolvePlugin(string command)
        {
            var reader = new PluginReader(Directory.GetCurrentDirectory());
            var plugins = reader.Read<ICommand>("command");

            if (!plugins.ContainsKey(command))
                return null;

            return plugins[command];
        }

        private static IDictionary<string, string> ParseArgs(string[] args)
        {
            var dic = new Dictionary<string, string>();

            for (int i = 1; i < args.Length; i += 2)
            {
                dic.Add(args[i].Replace("-", ""), args[i + 1]);
            }

            return dic;
        }

        /// <summary>
        /// Required args: collection, skip, take, batchSize
        /// </summary>
        private static void Optimize(IDictionary<string, string> args, TextModel model, ILogger logger)
        {
            var collection = args["collection"];
            var skip = int.Parse("skip");
            var take = int.Parse("take");
            var batchSize = int.Parse("batchSize");

            using (var sessionFactory = new SessionFactory(new KeyValueConfiguration("sir.ini"), logger))
            {
                sessionFactory.Optimize(
                    collection, 
                    new HashSet<string> { "title", "description", "url", "filename" },
                    new HashSet<string> { "title", "description", "url" },
                    model,
                    skip,
                    take,
                    batchSize);
            }
        }

        /// <summary>
        /// Required args: sourceFileName, resultFileName, length
        /// </summary>
        private static void Slice(IDictionary<string, string> args)
        {
            var file = args["sourceFileName"];
            var slice = args["resultFileName"];
            var len = int.Parse(args["length"]);

            Span<byte> buf = new byte[len];

            using (var fs = File.OpenRead(file))
            using (var target = File.Create(slice))
            {
                fs.Read(buf);
                target.Write(buf);
            }
        }

        /// <summary>
        /// Required args: collection
        /// </summary>
        private static void Truncate(string collection, ILogger log)
        {
            var collectionId = collection.ToHash();

            using (var sessionFactory = new SessionFactory(new KeyValueConfiguration("sir.ini"), log))
            {
                sessionFactory.Truncate(collectionId);
            }
        }

        /// <summary>
        /// Required args: collection
        /// </summary>
        private static void TruncateIndex(string collection, ILogger log)
        {
            var collectionId = collection.ToHash();

            using (var sessionFactory = new SessionFactory(new KeyValueConfiguration("sir.ini"), log))
            {
                sessionFactory.TruncateIndex(collectionId);
            }
        }

        private static void Serialize(IEnumerable<object> docs, Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
            {
                JsonSerializer ser = new JsonSerializer();
                ser.Serialize(jsonWriter, docs);
                jsonWriter.Flush();
            }
        }
    }
}


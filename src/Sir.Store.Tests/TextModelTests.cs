using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Sir.Search;
using Sir.VectorSpace;
using System;
using System.Diagnostics;
using System.IO;

namespace Sir.Tests
{
    public class TextModelTests
    {
        private ILoggerFactory _loggerFactory;
        private SessionFactory _sessionFactory;

        private readonly string[] _data = new string[] { "apple", "apples", "apricote", "apricots", "avocado", "avocados", "banana", "bananas", "blueberry", "blueberries", "cantalope" };

        [Test]
        public void Can_traverse_index_in_memory()
        {
            var model = new TextModel();
            var tree = GraphBuilder.CreateTree(model, _data);

            Debug.WriteLine(PathFinder.Visualize(tree));

            Assert.DoesNotThrow(() => 
            {
                foreach (var word in _data)
                {
                    foreach (var queryVector in model.Tokenize(word))
                    {
                        var hit = PathFinder.ClosestMatch(tree, queryVector, model);

                        if (hit == null)
                        {
                            throw new Exception($"unable to find {word} in tree.");
                        }

                        if (hit.Score < model.IdenticalAngle)
                        {
                            throw new Exception($"unable to score {word}.");
                        }

                        Debug.WriteLine($"{word} matched with {hit.Node.Vector.Label} with {hit.Score * 100}% certainty.");
                    }
                }
            });
        }

        [Test]
        public void Can_traverse_streamed()
        {
            var model = new TextModel();
            var tree = GraphBuilder.CreateTree(model, _data);

            using (var indexStream = new MemoryStream())
            using (var vectorStream = new MemoryStream())
            using (var pageStream = new MemoryStream())
            {
                using (var writer = new ColumnStreamWriter(indexStream, keepStreamOpen:true))
                {
                    writer.CreatePage(tree, vectorStream, new PageIndexWriter(pageStream, keepStreamOpen:true));
                }

                pageStream.Position = 0;

                Assert.DoesNotThrow(() =>
                {
                    using (var reader = new ColumnStreamReader(new PageIndexReader(pageStream), indexStream, vectorStream, _sessionFactory, _loggerFactory.CreateLogger<ColumnStreamReader>()))
                    {
                        foreach (var word in _data)
                        {
                            foreach (var queryVector in model.Tokenize(word))
                            {
                                var hit = reader.ClosestMatch(queryVector, model);

                                if (hit == null)
                                {
                                    throw new Exception($"unable to find {word} in tree.");
                                }

                                if (hit.Score < model.IdenticalAngle)
                                {
                                    throw new Exception($"unable to score {word}.");
                                }

                                Debug.WriteLine($"{word} matched vector in disk with {hit.Score * 100}% certainty.");
                            }
                        }
                    }
                });
            }
        }

        [SetUp]
        public void Setup()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("Sir.DbUtil.Program", LogLevel.Debug)
                    .AddDebug();
            });

            _sessionFactory = new SessionFactory(
                new KeyValueConfiguration("sir.ini"),
                _loggerFactory.CreateLogger<SessionFactory>());
        }

        [TearDown]
        public void TearDown()
        {
            _sessionFactory.Dispose();
        }
    }
}
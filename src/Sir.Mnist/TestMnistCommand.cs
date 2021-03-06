﻿using Microsoft.Extensions.Logging;
using Sir.Search;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sir.Mnist
{
    /// <summary>
    /// Test the accuracy of a MNIST index.
    /// </summary>
    /// <example>
    /// testmnist --imageFileName C:\temp\mnist\t10k-images.idx3-ubyte --labelFileName C:\temp\mnist\t10k-labels.idx1-ubyte --collection mnist
    /// </example>
    public class TestMnistCommand : ICommand
    {
        public void Run(IDictionary<string, string> args, ILogger logger)
        {
            var time = Stopwatch.StartNew();
            var images = new MnistReader(args["imageFileName"], args["labelFileName"]).Read();
            var collection = args["collection"];
            var count = 0;
            var errors = 0;
            var model = new ImageModel();

            using (var sessionFactory = new SessionFactory(new KeyValueConfiguration("sir.ini"), logger))
            using (var querySession = sessionFactory.CreateQuerySession(model))
            {
                var queryParser = new QueryParser<IImage>(sessionFactory, model, logger);

                foreach (var image in images)
                {
                    var query = queryParser.Parse(collection, image, field: "image", select: "label", and: true, or: false);
                    var result = querySession.Query(query, 0, 1);

                    count++;

                    if (result.Total == 0)
                    {
                        errors++;
                    }
                    else
                    {
                        var imageLabel = image.Label.ToString();
                        var documentLabel = result.Documents.First()["label"].ToString();

                        if (!documentLabel.Equals(imageLabel))
                        {
                            errors++;

                            logger.LogDebug($"error. label: {imageLabel} document label: {documentLabel}\n{((MnistImage)image).Print()}");
                        }
                    }

                    logger.LogInformation($"errors: {errors}. total tests {count}. error rate: {(float)errors / count * 100}%");
                }
            }

            logger.LogInformation($"tested {count} mnist images in {time.Elapsed}");
        }
    }
}
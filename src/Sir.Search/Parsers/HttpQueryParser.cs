﻿using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Sir.Store
{
    /// <summary>
    /// Parse text from a http request message into a query.
    /// </summary>
    public class HttpQueryParser
    {
        private readonly SessionFactory _sessionFactory;
        private readonly IStringModel _model;
        private readonly QueryParser _parser;

        public HttpQueryParser(SessionFactory sessionFactory, IStringModel model)
        {
            _sessionFactory = sessionFactory;
            _model = model;
            _parser = new QueryParser(sessionFactory, model);
        }

        public IEnumerable<Query> Parse(ulong collectionId, HttpRequest request)
        {
            string[] fields = request.Query["fields"].ToArray();
            bool and = request.Query.ContainsKey("AND");
            bool or = !and;
            const bool not = false;
            var isFormatted = request.Query.ContainsKey("qf");

            if (isFormatted)
            {
                var formattedQuery = request.Query["qf"].ToString();
                foreach (var q in FromFormattedString(collectionId, formattedQuery, _model, and, or, not))
                    yield return q;
            }
            else
            {
                var naturalLanguage = request.Query["q"].ToString();

                foreach (var q in _parser.Parse(collectionId, naturalLanguage, fields, and, or, not))
                {
                    yield return q;
                }
            }
        }

        public IEnumerable<Query> FromFormattedString(ulong collectionId, string formattedQuery, IStringModel model, bool and, bool or, bool not)
        {
            var document = JsonConvert.DeserializeObject<IDictionary<string, object>>(formattedQuery);

            foreach (var field in document)
            {
                if (field.Value is string)
                {
                    long keyId;

                    if (_sessionFactory.TryGetKeyId(collectionId, field.Key.ToHash(), out keyId))
                    {
                        yield return new Query(keyId, model.Tokenize((string)field.Value), and, or, not);
                    }
                }
            }
        }

        public IEnumerable<Query> FromDocument(ulong collectionId, IDictionary<string, object> document, IStringModel model, bool and, bool or, bool not)
        {
            foreach (var field in document)
            {
                if (field.Value is string)
                {
                    long keyId;

                    if (_sessionFactory.TryGetKeyId(collectionId, field.Key.ToHash(), out keyId))
                    {
                        yield return new Query(keyId, model.Tokenize((string)field.Value), and, or, not);
                    }
                }
            }
        }
    }
}
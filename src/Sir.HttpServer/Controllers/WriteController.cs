﻿using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sir.Search;

namespace Sir.HttpServer.Controllers
{
    [Route("write")]
    public class WriteController : Controller
    {
        private readonly IHttpWriter _writer;
        private readonly ITextModel _model;
        private readonly ILogger<WriteController> _logger;
        private readonly IConfigurationProvider _config;

        public WriteController(
            IHttpWriter writer,
            ITextModel tokenizer, 
            ILogger<WriteController> logger,
            IConfigurationProvider config)
        {
            _writer = writer;
            _model = tokenizer;
            _logger = logger;
            _config = config;
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public IActionResult Post(string accessToken)
        {
            if (!IsValidToken(accessToken))
            {
                return StatusCode((int)HttpStatusCode.MethodNotAllowed);
            }

            if (string.IsNullOrWhiteSpace(Request.ContentType))
            {
                throw new NotSupportedException();
            }

            try
            {
                _writer.Write(Request, _model);

                return Ok();
            }
            catch (Exception ew)
            {
                _logger.LogError(ew.ToString());

                throw ew;
            }
        }

        private bool IsValidToken(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return false;

            return _config.Get("admin_password").Equals(accessToken);
        }
    }
}
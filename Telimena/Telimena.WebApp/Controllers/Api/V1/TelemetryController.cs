﻿using System;
using System.Collections.Generic;
using System.IdentityModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using DotNetLittleHelpers;
using Hangfire;
using Newtonsoft.Json;
using Telimena.Portal.Api.Models.RequestMessages;
using Telimena.WebApp.Controllers.Api.V1.Helpers;
using Telimena.WebApp.Core.DTO;
using Telimena.WebApp.Core.DTO.AppInsightsTelemetryModel;
using Telimena.WebApp.Core.DTO.MappableToClient;
using Telimena.WebApp.Core.Interfaces;
using Telimena.WebApp.Core.Messages;
using Telimena.WebApp.Core.Models;
using Telimena.WebApp.Core.Models.Telemetry;
using Telimena.WebApp.Infrastructure;
using Telimena.WebApp.Infrastructure.Security;
using Telimena.WebApp.Infrastructure.UnitOfWork;
using Telimena.WebApp.Utils;
using JsonSerializer = Microsoft.ApplicationInsights.Extensibility.Implementation.JsonSerializer;

namespace Telimena.WebApp.Controllers.Api.V1
{
    /// <summary>
    ///     Controls the telemetry process
    /// </summary>
    [TelimenaApiAuthorize(Roles = TelimenaRoles.Developer)]
    [RoutePrefix("api/v{version:apiVersion}/telemetry")]
    public partial class TelemetryController : ApiController
    {
        /// <summary>
        ///     New instance
        /// </summary>
        /// <param name="work"></param>
        public TelemetryController(ITelemetryUnitOfWork work)
        {
            this.work = work;
        }

        private readonly ITelemetryUnitOfWork work;

        /// <summary>
        ///     Executes the query to get telemetry data as specified in the request object parameters
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous] //todo - this should not be anon
        [HttpPost,Route("execute-query", Name = Routes.ExecuteQuery)]
        public async Task<IHttpActionResult> ExecuteQuery(TelemetryQueryRequest request)
        {
            if (request.TelemetryKey == Guid.Empty)
            {
                return this.BadRequest("Empty telemetry key");
            }

            TelemetryRootObject prg = await this.work.GetMonitoredProgram(request.TelemetryKey).ConfigureAwait(false);
            if (prg == null)
            {
                return this.BadRequest($"Program with key [{request.TelemetryKey}] not found");
            }


             TelemetryQueryResponse queryResult = TelemetryQueryResponseCreator.Create(request, prg);

            return this.Ok(queryResult);
        }

        /// <summary>
        ///     Initialization of telemetry
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost, Route("initialize", Name = Routes.Initialize)]
        public async Task<TelemetryInitializeResponse> Initialize(TelemetryInitializeRequest request)
        {
            try
            {
                if (!ApiRequestsValidator.IsRequestValid(request))
                {
                    return new TelemetryInitializeResponse() { Exception = new BadRequestException("Request is not valid") };
                }

                TelemetryRootObject program = await work.GetMonitoredProgram(request.TelemetryKey).ConfigureAwait(false);
                if (program == null)
                {
                    {
                       return new TelemetryInitializeResponse { Exception = new InvalidOperationException($"Program [{request.TelemetryKey}] is null") };
                    }
                }

                string ip = this.Request.GetClientIp();
                ClientAppUser clientAppUser = await TelemetryControllerHelpers.GetUserOrAddIfMissing(this.work, request.UserInfo, ip).ConfigureAwait(false);

                await this.work.CompleteAsync().ConfigureAwait(false);
                TelemetryInitializeResponse response = new TelemetryInitializeResponse {UserId = clientAppUser.PublicId};
                return response;
            }
            catch (Exception ex)
            {
                return new TelemetryInitializeResponse {Exception = new InvalidOperationException("Error while processing telemetry initialize request", ex)};
            }
        }

        /// <summary>
        ///     For Hangfire to support the job creation
        /// </summary>
        /// <param name="program"></param>
        /// <param name="ip"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        [NonAction]
        public Task InsertDataInternal(IEnumerable<TelemetryItem> items, TelemetryRootObject program, string ip)
        {
            return TelemetryControllerHelpers.InsertData(this.work, items.ToList(), program, ip);
        }

        /// <summary>
        ///     Posts telemetry entries to a program with the specified telemetry key
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("{telemetryKey}", Name = Routes.Post)]
        public async Task<IHttpActionResult> Post(Guid telemetryKey)
        {
            TelemetryRootObject program = await this.work.GetMonitoredProgram(telemetryKey).ConfigureAwait(false);
            if (program == null)
            {
                throw new BadRequestException($"Program with telemetry key [{telemetryKey}] does not exist");
            }

            IEnumerable<AppInsightsTelemetry> appInsightsTelemetries = AppInsightsDeserializer.Deserialize(await this.Request.Content.ReadAsByteArrayAsync().ConfigureAwait(false), true);

            IEnumerable<TelemetryItem> telemetryItems = AppInsightsTelemetryMapper.Map(appInsightsTelemetries);

            string ip = this.Request.GetClientIp();

            await this.InsertDataInternal(telemetryItems, program, ip).ConfigureAwait(false);

            return await Task.FromResult(this.StatusCode(HttpStatusCode.Accepted)).ConfigureAwait(false);     
        }

        /// <summary>
        ///     Posts telemetry entries with TelemetryKeys being sent in the item properties. This way a batch of entries from single request can go to multiple programs separately.
        ///     This is available since Client 2.9.0
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("", Name = Routes.PostWithVariousKeys)]
        public async Task<IHttpActionResult> PostWithVariousKeys()
        {
            IEnumerable<AppInsightsTelemetry> appInsightsTelemetries = AppInsightsDeserializer.Deserialize(await this.Request.Content.ReadAsByteArrayAsync().ConfigureAwait(false), true);

            IEnumerable<TelemetryItem> telemetryItems = AppInsightsTelemetryMapper.Map(appInsightsTelemetries);
            string ip = this.Request.GetClientIp();

            //items might come with various telemetry key in one request (when there are multiple instances of telemetry client)
            IEnumerable<IGrouping<Guid, TelemetryItem>> groups = telemetryItems.GroupBy(telemetry => telemetry.TelemetryKey);
            //todo - parallelism?
            foreach (IGrouping<Guid, TelemetryItem> grouping in groups)
            {
                TelemetryRootObject program = await this.work.GetMonitoredProgram(grouping.Key).ConfigureAwait(false);
                await this.InsertDataInternal(grouping, program, ip).ConfigureAwait(false);
            }

            return await Task.FromResult(this.StatusCode(HttpStatusCode.Accepted)).ConfigureAwait(false);
        }

        /// <summary>
        ///   Provides a simplified telemetry endpoint for 1-to-1 requests
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("{telemetryKey}/basic", Name = Routes.PostBasic)]
        public async Task<IHttpActionResult> PostBasic(Guid telemetryKey, BasicTelemetryItem request)
        {
            if (request == null)
            {
                request = new BasicTelemetryItem();
            }

            TelemetryRootObject program = await this.work.GetMonitoredProgram(telemetryKey).ConfigureAwait(false);
            if (program == null)
            {
                throw new BadRequestException($"Program with telemetry key [{telemetryKey}] does not exist");
            }

            if (!Version.TryParse(request.ProgramVersion, out _))
            {
                request.ProgramVersion = "0.0.0.0";
            }

            TelemetryItem item = new TelemetryItem
            {
                UserIdentifier = request.UserIdentifier
                , EntryKey = request.EventName
                , Properties = request.Properties
                , Measurements = request.Metrics
                , VersionData = new VersionData(request.ProgramVersion, request.ProgramVersion)
                , Timestamp = request.Timestamp
            };


            string ip = this.Request.GetClientIp();

            await this.InsertDataInternal(new []{item}, program, ip).ConfigureAwait(false);

            return await Task.FromResult(this.StatusCode(HttpStatusCode.OK)).ConfigureAwait(false);
        }








    }
}
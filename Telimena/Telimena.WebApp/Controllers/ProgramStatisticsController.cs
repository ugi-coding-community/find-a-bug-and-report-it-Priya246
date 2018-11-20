﻿using System;
using System.Collections.Generic;
using System.IdentityModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Results;
using System.Web.Mvc;
using DataTables.AspNet.Core;
using DataTables.AspNet.Mvc5;
using MvcAuditLogger;
using Newtonsoft.Json;
using Telimena.WebApp.Core.DTO;
using Telimena.WebApp.Core.Interfaces;
using Telimena.WebApp.Core.Models;
using Telimena.WebApp.Infrastructure.Repository;
using Telimena.WebApp.Infrastructure.Security;
using Telimena.WebApp.Models.ProgramStatistics;

namespace Telimena.WebApp.Controllers
{
    [TelimenaAuthorize(Roles = TelimenaRoles.Developer)]
    public class ProgramStatisticsController : Controller
    {
        public ProgramStatisticsController(IProgramsDashboardUnitOfWork work)
        {
            this.Work = work;
        }

        private IProgramsDashboardUnitOfWork Work { get; }

        [Audit]
        [HttpGet]
        public async Task<ActionResult> Index(Guid telemetryKey)
        {
            Program program = await this.Work.Programs.SingleOrDefaultAsync(x => x.TelemetryKey == telemetryKey);

            if (program == null)
            {
                return this.RedirectToAction("Index", "Home");
            }

            ProgramStatisticsViewModel model = new ProgramStatisticsViewModel() {TelemetryKey = program.TelemetryKey, ProgramName = program.Name};

            return this.View("Index", model);
        }


        [Audit]
        [HttpGet]
        public async Task<ActionResult> ExportViewUsageCustomData(Guid telemetryKey, bool includeGenericData)
        {
            Program program = await this.Work.Programs.SingleOrDefaultAsync(x => x.TelemetryKey == telemetryKey);
            if (program == null)
            {
                throw new BadRequestException($"Program with key {telemetryKey} does not exist");
            }

            dynamic obj = await this.Work.ExportViewsUsageCustomData(program.Id, includeGenericData);
            string content = JsonConvert.SerializeObject(obj);
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            FileContentResult result = new FileContentResult(bytes, "text/plain");
            result.FileDownloadName = $"{DateTime.UtcNow:yyyy-MM-dd HH-mm-ss}_ViewsCustomDataExport_{program.Name}.json";
            return result;
        }


        [HttpGet]
        public async Task<JsonResult> GetProgramUsageData(Guid telemetryKey, IDataTablesRequest request)
        {
            IEnumerable<Tuple<string, bool>> sorts = request.Columns.Where(x => x.Sort != null).OrderBy(x=>x.Sort.Order).Select(x => new Tuple<string, bool>(x.Name, x.Sort.Direction == SortDirection.Descending));
                
            UsageDataTableResult result = await this.Work.GetProgramUsageData(telemetryKey, request.Start, request.Length, sorts);

            DataTablesResponse response = DataTablesResponse.Create(request, result.TotalCount, result.FilteredCount, result.UsageData);

            return new DataTablesJsonResult(response, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetProgramViewsUsageData(Guid telemetryKey, IDataTablesRequest request)
        {
            IEnumerable<Tuple<string, bool>> sorts = request.Columns.Where(x => x.Sort != null).OrderBy(x => x.Sort.Order).Select(x => new Tuple<string, bool>(x.Name, x.Sort.Direction == SortDirection.Descending));

            UsageDataTableResult result = await this.Work.GetProgramViewsUsageData(telemetryKey, request.Start, request.Length, sorts);

            DataTablesResponse response = DataTablesResponse.Create(request, result.TotalCount, result.FilteredCount, result.UsageData);

            return new DataTablesJsonResult(response, JsonRequestBehavior.AllowGet);
        }

    }
}
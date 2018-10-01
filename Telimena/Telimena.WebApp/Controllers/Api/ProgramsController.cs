﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Telimena.WebApp.Core.Interfaces;
using Telimena.WebApp.Core.Models;
using Telimena.WebApp.Infrastructure.Security;
using Telimena.WebApp.Infrastructure.UnitOfWork;

namespace Telimena.WebApp.Controllers.Api
{
    #region Using

    #endregion

    [TelimenaAuthorize(Roles = TelimenaRoles.Developer)]
    public class ProgramsController : ApiController
    {
        public ProgramsController(IProgramsUnitOfWork work)
        {
            this.Work = work;
        }

        private IProgramsUnitOfWork Work { get; }

        [HttpDelete]
        public async Task<IHttpActionResult> Delete(int id)
        {
            var prg = await this.Work.Programs.FirstOrDefaultAsync(x => x.Id == id);
            try
            {
                this.Work.Programs.Remove(prg);
                await this.Work.CompleteAsync();
            }
            catch (Exception ex)
            {
                return this.InternalServerError(new InvalidOperationException($"Error while deleting program {prg.Name} (ID: {id})", ex));
            }
            return this.Ok($"Program {prg.Name} (ID: {id}) deleted successfully");
        }


        [HttpGet]
        public async Task<IEnumerable<Program>> GetPrograms(int developerId)
        {
            return await this.Work.Programs.GetAsync(x => x.DeveloperAccount.Id == developerId);
        }
    }
}
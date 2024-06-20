using Internship_Backend_cpt.Models;
using Internship_Backend_cpt.Models.DbModels;
using Internship_Backend_cpt.Services.Main;
using Internship_Backend_cpt.Services.MsSql;
using Internship_Backend_cpt.Services.Postgres;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Internship_Backend_cpt.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComparerController : ControllerBase
    {
        private readonly MsSqlService _msSqlService;
        private readonly PostgreSqlService _postgreSqlService;
        private readonly ComparerService _compareService;

        public ComparerController(MsSqlService msSqlService,
            PostgreSqlService postgreSqlService,
            ComparerService compareService)
        {
            _msSqlService = msSqlService;
            _postgreSqlService = postgreSqlService;
            _compareService = compareService;
        }

        [HttpPost]
        [Route("GetComparerResult")]
        public IActionResult GetComparerResult(Guid uniqueId)
        {
            var cookieValue = Request.Cookies["complexCookieData"];

            if (cookieValue != null)
            {
                var connectionModel = JsonConvert.DeserializeObject<ConnectionModel>(cookieValue);

                if (connectionModel.Guid == uniqueId)
                {
                    var twoSchemas = GetTwoSchemas(connectionModel);

                    return Ok(_compareService.Compare(twoSchemas.Result.Item1, twoSchemas.Result.Item2));
                }
                else
                {
                    return BadRequest("Данные не соответствуют переданному идентификатору");
                }
            }
            else
            {
                return NotFound();
            }
        }

        private Task<(SchemaModel, SchemaModel)> GetTwoSchemas(ConnectionModel connectionModel)
        {
            SchemaModel parentSchemaModel;
            SchemaModel childSchemaModel;

            if (connectionModel.FirstProviderName == "MsSql" && connectionModel.SecondProviderName == "Postgres")
            {
                parentSchemaModel = _msSqlService.GetSchemaMsSql(connectionModel.FirstConnectionString);
                childSchemaModel = _postgreSqlService.GetSchemaPostgreSql(connectionModel.SecondConnectionString);
                return Task.FromResult((parentSchemaModel, childSchemaModel));

            }
            else if (connectionModel.FirstProviderName == "Postgres" && connectionModel.SecondProviderName == "MsSql")
            {
                parentSchemaModel = _msSqlService.GetSchemaMsSql(connectionModel.SecondConnectionString);
                childSchemaModel = _postgreSqlService.GetSchemaPostgreSql(connectionModel.FirstConnectionString);
                return Task.FromResult((parentSchemaModel, childSchemaModel));
            }
            else if (connectionModel.FirstProviderName == "Postgres" && connectionModel.SecondProviderName == "Postgres")
            {
                parentSchemaModel = _postgreSqlService.GetSchemaPostgreSql(connectionModel.FirstConnectionString);
                childSchemaModel = _postgreSqlService.GetSchemaPostgreSql(connectionModel.SecondConnectionString);

                return Task.FromResult((parentSchemaModel, childSchemaModel));
            }
            else
            {
                parentSchemaModel = _msSqlService.GetSchemaMsSql(connectionModel.FirstConnectionString);
                childSchemaModel = _msSqlService.GetSchemaMsSql(connectionModel.SecondConnectionString);

                return Task.FromResult((parentSchemaModel, childSchemaModel));
            }
        }
    }
}

using Internship_Backend_cpt.Enums;
using Internship_Backend_cpt.Models;
using Internship_Backend_cpt.Services.Main;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Internship_Backend_cpt.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionController : ControllerBase
    {
        readonly Dictionary<string, ProvidersEnum> providerMapping = new()
        {
            { "Postgres", ProvidersEnum.Postgres },
            { "MsSql", ProvidersEnum.MsSql }
        };

        private readonly DatabaseService _databaseService;

        public ConnectionController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpPost]
        [Route("GetConnectionStrings")]
        public IActionResult GetConnectionStrings(string firstConnectionString, string secondConnectionString, string firstProviderName, string secondProviderName)
        {

            if (providerMapping.ContainsKey(firstProviderName)
                && providerMapping.ContainsKey(secondProviderName))
            {
                ProvidersEnum firstProvider = providerMapping[firstProviderName];
                ProvidersEnum secondProvider = providerMapping[secondProviderName];

                if ( !_databaseService.TestConnection(firstConnectionString, firstProvider))
                    return BadRequest("Не удалось установить подключение к первой базе данных." +
                        " Пожалуйста, проверьте строки подключения.");
                else if (!_databaseService.TestConnection(secondConnectionString, secondProvider))
                    return BadRequest("Не удалось установить подключение ко второй базе данных." +
                        " Пожалуйста, проверьте строки подключения.");
                else
                {
 Guid uniqueId = Guid.NewGuid();

                ConnectionModel connectionModel = new()
                {
                    Guid = uniqueId,
                    FirstConnectionString = firstConnectionString,
                    SecondConnectionString = secondConnectionString,
                    FirstProviderName = firstProviderName,
                    SecondProviderName = secondProviderName
                };

                Response.Cookies.Append("complexCookieData", JsonConvert.SerializeObject(connectionModel), new CookieOptions
                {
                    Secure = true,
                    HttpOnly = true,
                });

                return Ok(uniqueId);
                }
            }
            return BadRequest("Не удалось определить провайдеров баз данных. Пожалуйста, проверьте имена провайдеров.");
        }
    }
}

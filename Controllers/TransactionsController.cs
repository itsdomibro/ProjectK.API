using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.API.Data;

namespace ProjectK.API.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {


            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById()
        {
            return Ok();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Edit()
        {
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete()
        {
            return Ok();
        }
    }
}

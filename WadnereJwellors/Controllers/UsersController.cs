using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.Business.Services;
using WadnereJwellors.Models;

namespace WadnereJwellors.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService) {
            _userService = userService;
        }

        /// <summary>
        /// Gets all users.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers() {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// Gets a specific user by ID.
        /// </summary>
        /// <param name="id">The user ID</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUserById(int id) {
            var user = await _userService.GetUserByIdAsync(id);
            return Ok(user);
        }

        /// <summary>
        /// Register User
        /// </summary>
        [HttpPost("/api/Register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterUser([FromBody] RegistrationDto register) {
            await _userService.RegisterUserAsync(register);
            return Ok(new { message = "User registered successfully." });
        }

        [HttpPut("/api/Register")]
        public async Task<IActionResult> UpdateRegisterUser([FromBody] RegistrationDto register) {
            await _userService.UpdateRegisterUserAsync(register);
            return Ok(new { message = "User updated successfully." });
        }

        [HttpGet("/api/Register")]
        [ProducesResponseType(typeof(IEnumerable<RegistrationDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RegistrationDto>>> GetAllRegisterUsers() {
            var registerUsers = await _userService.GetAllRegisterUsersAsync();
            return Ok(registerUsers);
        }

    }
}

using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Services;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace ABCRetailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _context;
        private readonly IFunctionsApi _api;

        public LoginController(AuthDbContext context, IFunctionsApi api)
        {
            _context = context;
            _api = api;
        }

        // -------------------- LOGIN --------------------
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to home
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users.FirstOrDefault(u => u.Username == model.Username && u.PasswordHash == model.PasswordHash);
            if (user != null)
            {
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);

                // Redirect based on role
                if (user.Role == "Customer")
                {
                    return RedirectToAction("Index", "Product"); // Customers go directly to products
                }
                else
                {
                    return RedirectToAction("Index", "Home"); // Admins go to dashboard
                }
            }

            ModelState.AddModelError("", "Invalid credentials");
            return View(model);
        }

        // -------------------- REGISTER --------------------
        [HttpGet]
        public IActionResult Register()
        {
            // If already logged in, redirect to home
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // For Admin role, manually clear customer fields to avoid validation
            if (model.Role == "Admin")
            {
                model.Name = null;
                model.Surname = null;
                model.Email = null;
                model.ShippingAddress = null;

                // Remove validation errors for these fields
                ModelState.Remove("Name");
                ModelState.Remove("Surname");
                ModelState.Remove("Email");
                ModelState.Remove("ShippingAddress");
            }

            // Now check if model is valid
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model validation failed after Admin adjustment");
                return View(model);
            }

            try
            {
                var existingUser = _context.Users.FirstOrDefault(u => u.Username == model.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Username already exists");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = model.PasswordHash,
                    Role = model.Role
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Only create customer profile for Customer role
                if (model.Role == "Customer")
                {
                    var customer = new Customer
                    {
                        Username = model.Username,
                        Name = model.Name ?? string.Empty,
                        Surname = model.Surname ?? string.Empty,
                        Email = model.Email ?? string.Empty,
                        ShippingAddress = model.ShippingAddress ?? string.Empty
                    };

                    await _api.CreateCustomerAsync(customer);
                }

                TempData["Success"] = "Registration successful! Please login with your credentials.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Registration failed: {ex.Message}");
                return View(model);
            }
        }

        // -------------------- LOGOUT --------------------
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }
    }
}
using ABCRetailers.Authorization;
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    [RequireAuth]
    public class CustomerController : Controller
    {
        private readonly IFunctionsApi _api;
        public CustomerController(IFunctionsApi api) => _api = api;

        // ADMIN: View all customers
        public async Task<IActionResult> Index()
        {
            try
            {
                if (UserHelper.IsAdmin(HttpContext))
                {
                    // Admin can see all customers
                    var customers = await _api.GetCustomersAsync();
                    return View(customers ?? new List<Customer>());
                }
                else
                {
                    // Customer can only see their own profile
                    var username = UserHelper.GetUsername(HttpContext);
                    var allCustomers = await _api.GetCustomersAsync();
                    var customer = allCustomers?.FirstOrDefault(c => c.Username == username);

                    if (customer != null)
                    {
                        return View(new List<Customer> { customer });
                    }
                    else
                    {
                        TempData["Error"] = "Customer profile not found.";
                        return View(new List<Customer>());
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading customers.";
                return View(new List<Customer>());
            }
        }

        // CUSTOMER: View their own profile
        public async Task<IActionResult> Profile()
        {
            try
            {
                var username = UserHelper.GetUsername(HttpContext);
                var allCustomers = await _api.GetCustomersAsync();
                var customer = allCustomers?.FirstOrDefault(c => c.Username == username);

                if (customer != null)
                {
                    return View(customer);
                }
                else
                {
                    TempData["Error"] = "Customer profile not found. Please contact administrator.";
                    return View(new Customer()); // Return empty customer
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading your profile.";
                return View(new Customer());
            }
        }

        // CUSTOMER: Edit their own profile
        public async Task<IActionResult> EditProfile()
        {
            try
            {
                var username = UserHelper.GetUsername(HttpContext);
                var allCustomers = await _api.GetCustomersAsync();
                var customer = allCustomers?.FirstOrDefault(c => c.Username == username);

                if (customer != null)
                {
                    return View(customer);
                }
                else
                {
                    TempData["Error"] = "Customer profile not found. Please contact administrator.";
                    return RedirectToAction("Profile");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading profile for editing.";
                return RedirectToAction("Profile");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(Customer customer)
        {
            if (!ModelState.IsValid) return View(customer);

            try
            {
                // Ensure the customer can only edit their own profile
                var username = UserHelper.GetUsername(HttpContext);

                // Verify this customer belongs to the logged-in user
                var allCustomers = await _api.GetCustomersAsync();
                var existingCustomer = allCustomers?.FirstOrDefault(c => c.Username == username && c.Id == customer.Id);

                if (existingCustomer == null)
                {
                    TempData["Error"] = "Access denied. You can only edit your own profile.";
                    return RedirectToAction("Profile");
                }

                // Update the customer profile
                await _api.UpdateCustomerAsync(customer.Id, customer);
                TempData["Success"] = "Profile updated successfully!";

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error saving profile: {ex.Message}");
                return View(customer);
            }
        }

        public IActionResult Create()
        {
            // Only Admin can create customer profiles
            if (!UserHelper.IsAdmin(HttpContext))
            {
                TempData["Error"] = "Access denied. Only administrators can create customer profiles.";
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            // Only Admin can create customer profiles
            if (!UserHelper.IsAdmin(HttpContext))
            {
                TempData["Error"] = "Access denied. Only administrators can create customer profiles.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid) return View(customer);
            try
            {
                await _api.CreateCustomerAsync(customer);
                TempData["Success"] = "Customer created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                return View(customer);
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var customer = await _api.GetCustomerAsync(id);
            if (customer is null) return NotFound();

            // Check permissions - only Admin or the customer themselves can edit
            if (!UserHelper.IsAdmin(HttpContext) && !UserHelper.IsCurrentUser(HttpContext, customer.Username))
            {
                TempData["Error"] = "Access denied. You can only edit your own profile.";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            // First, get the existing customer to check permissions
            var existingCustomer = await _api.GetCustomerAsync(customer.Id);
            if (existingCustomer is null) return NotFound();

            // Check permissions before editing
            if (!UserHelper.IsAdmin(HttpContext) && !UserHelper.IsCurrentUser(HttpContext, existingCustomer.Username))
            {
                TempData["Error"] = "Access denied. You can only edit your own profile.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid) return View(customer);

            try
            {
                // Ensure username cannot be changed by customers
                if (!UserHelper.IsAdmin(HttpContext))
                {
                    customer.Username = existingCustomer.Username; // Prevent username change
                }

                await _api.UpdateCustomerAsync(customer.Id, customer);
                TempData["Success"] = "Customer updated successfully!";

                if (UserHelper.IsAdmin(HttpContext))
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Customers return to their own profile view
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                return View(customer);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            // Only Admin can delete customers
            if (!UserHelper.IsAdmin(HttpContext))
            {
                TempData["Error"] = "Access denied. Only administrators can delete customers.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _api.DeleteCustomerAsync(id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
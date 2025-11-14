using ABCRetailers.Authorization;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    [RequireAuth]
    public class OrderController : Controller
    {
        private readonly IFunctionsApi _api;
        public OrderController(IFunctionsApi api) => _api = api;

        // LIST - Now handles both Admin and Customer views
        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");
            
            try
            {
                var allOrders = await _api.GetOrdersAsync();
                
                if (role == "Customer")
                {
                    // For customers, only show their own orders
                    var customerOrders = allOrders
                        .Where(o => o.CustomerId == username) // Assuming CustomerId is the username
                        .OrderByDescending(o => o.OrderDateUtc)
                        .ToList();
                    
                    return View("CustomerOrders", customerOrders);
                }
                else
                {
                    // For admins, show all orders (existing logic)
                    return View(allOrders.OrderByDescending(o => o.OrderDateUtc).ToList());
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading orders.";
                if (role == "Customer")
                    return View("CustomerOrders", new List<Order>());
                else
                    return View(new List<Order>());
            }
        }

        // CUSTOMER ORDERS VIEW (Separate view for customers)
        public async Task<IActionResult> CustomerOrders()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (role != "Customer")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            try
            {
                var allOrders = await _api.GetOrdersAsync();
                var allCustomers = await _api.GetCustomersAsync();

                // Get the customer's actual ID
                var customer = allCustomers.FirstOrDefault(c => c.Username == username);

                if (customer == null)
                {
                    TempData["Error"] = "Customer profile not found.";
                    return View(new List<Order>());
                }

                // Match orders by customer ID
                var customerOrders = allOrders
                    .Where(o => o.CustomerId == customer.Id)  // Use customer ID, not username
                    .OrderByDescending(o => o.OrderDateUtc)
                    .ToList();

                return View(customerOrders);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading your orders.";
                return View(new List<Order>());
            }
        }
        

        // CUSTOMER ORDER DELETE (Customers can only delete their own orders)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomerOrder(string id)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (role != "Customer")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            try
            {
                // Verify the order belongs to the current customer
                var order = await _api.GetOrderAsync(id);
                if (order == null || order.CustomerId != username)
                {
                    TempData["Error"] = "Order not found or access denied.";
                    return RedirectToAction("CustomerOrders");
                }

                await _api.DeleteOrderAsync(id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction("CustomerOrders");
        }

        // CREATE (GET) - ORIGINAL UNCHANGED
        public async Task<IActionResult> Create()
        {
            var customers = await _api.GetCustomersAsync();
            var products = await _api.GetProductsAsync();

            var vm = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };
            return View(vm);
        }

        // CREATE (POST) - ORIGINAL UNCHANGED
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                // Validate references (optional; Functions can also validate)
                var customer = await _api.GetCustomerAsync(model.CustomerId);
                var product = await _api.GetProductAsync(model.ProductId);

                if (customer is null || product is null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid customer or product selected.");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                if (product.StockAvailable < model.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                // Create order via Function (Function will set UTC time, snapshot price, update stock, enqueue messages)
                var saved = await _api.CreateOrderAsync(model.CustomerId, model.ProductId, model.Quantity);

                TempData["Success"] = "Order created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error creating order: {ex.Message}");
                await PopulateDropdowns(model);
                return View(model);
            }
        }

        // DETAILS - ORIGINAL UNCHANGED
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var order = await _api.GetOrderAsync(id);
            return order is null ? NotFound() : View(order);
        }

        // EDIT (GET) - ORIGINAL UNCHANGED
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var order = await _api.GetOrderAsync(id);
            return order is null ? NotFound() : View(order);
        }

        // EDIT (POST) - ORIGINAL UNCHANGED
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order posted)
        {
            if (!ModelState.IsValid) return View(posted);

            try
            {
                await _api.UpdateOrderStatusAsync(posted.Id, posted.Status.ToString());
                TempData["Success"] = "Order updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating order: {ex.Message}");
                return View(posted);
            }
        }

        // DELETE - ORIGINAL UNCHANGED
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _api.DeleteOrderAsync(id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // AJAX: price/stock lookup - ORIGINAL UNCHANGED
        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _api.GetProductAsync(productId);
                if (product is not null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // AJAX: status update - ORIGINAL UNCHANGED
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                await _api.UpdateOrderStatusAsync(id, newStatus);
                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _api.GetCustomersAsync();
            model.Products = await _api.GetProductsAsync();
        }
    }
}
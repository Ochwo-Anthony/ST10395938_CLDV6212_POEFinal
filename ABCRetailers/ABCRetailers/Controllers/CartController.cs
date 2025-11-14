using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class CartController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly IFunctionsApi _api;

        public CartController(AuthDbContext db, IFunctionsApi api)
        {
            _db = db;
            _api = api;
        }

        // POST: /Cart/Add
        [HttpPost]
        public IActionResult Add(string productId, int quantity)
        {
            var username = HttpContext.Session.GetString("Username");

            if (username == null)
                return RedirectToAction("Login", "Login");

            if (string.IsNullOrEmpty(productId) || quantity < 1)
            {
                TempData["Error"] = "Invalid product or quantity.";
                return RedirectToAction("Index", "Product");
            }

            var existing = _db.Cart
                .FirstOrDefault(c => c.CustomerUsername == username && c.ProductId == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                var item = new Cart
                {
                    CustomerUsername = username,
                    ProductId = productId,
                    Quantity = quantity
                };

                _db.Cart.Add(item);
            }

            _db.SaveChanges();

            TempData["Success"] = "Item added to cart!";
            return RedirectToAction("Index", "Product");
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");

            if (username == null)
                return RedirectToAction("Login", "Login");

            var items = _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToList();

            return View(items);
        }

        // POST: /Cart/Remove/{id}
        [HttpPost]
        public IActionResult Remove(int id)
        {
            var item = _db.Cart.Find(id);
            if (item != null)
            {
                _db.Cart.Remove(item);
                _db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // ================================
        //       CHECKOUT (GET)
        // ================================
        public IActionResult Checkout()
        {
            var username = HttpContext.Session.GetString("Username");
            if (username == null)
                return RedirectToAction("Login", "Login");

            var cartItems = _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToList();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            return View(cartItems);
        }

        // ================================
        //       CHECKOUT CONFIRM (POST)
        // ================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutConfirmed()
        {
            var username = HttpContext.Session.GetString("Username");
            if (username == null)
                return RedirectToAction("Login", "Login");

            var cartItems = _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToList();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            try
            {
                // Try to find customer in API by username
                Customer? apiCustomer = null;
                var allCustomers = await _api.GetCustomersAsync();
                apiCustomer = allCustomers.FirstOrDefault(c => c.Username == username);

                if (apiCustomer == null)
                {
                    // Customer doesn't exist in API - get user data from local database
                    var localUser = _db.Users.FirstOrDefault(u => u.Username == username);

                    if (localUser == null)
                    {
                        TempData["Error"] = "User not found. Please contact administrator.";
                        return RedirectToAction("Checkout");
                    }

                    // Redirect to customer profile creation since we don't have customer details
                    TempData["Error"] = "Please complete your customer profile before checkout.";
                    return RedirectToAction("Create", "Customer"); // Redirect to customer creation
                }

                if (apiCustomer == null || string.IsNullOrEmpty(apiCustomer.Id))
                {
                    TempData["Error"] = "Could not find customer profile in the system.";
                    return RedirectToAction("Checkout");
                }

                // Verify products exist before creating orders
                var successfulOrders = 0;
                var failedOrders = new List<string>();

                foreach (var item in cartItems)
                {
                    try
                    {
                        var product = await _api.GetProductAsync(item.ProductId);
                        if (product == null)
                        {
                            failedOrders.Add($"Product {item.ProductId} not found");
                            continue;
                        }

                        // Check stock availability
                        if (product.StockAvailable < item.Quantity)
                        {
                            failedOrders.Add($"Insufficient stock for {product.ProductName}. Available: {product.StockAvailable}, Requested: {item.Quantity}");
                            continue;
                        }

                        // Create the order
                        await _api.CreateOrderAsync(
                            apiCustomer.Id,   // Use the actual customer ID from API
                            item.ProductId,   // product ID
                            item.Quantity     // quantity
                        );
                        successfulOrders++;
                    }
                    catch (HttpRequestException ex)
                    {
                        failedOrders.Add($"Failed to create order for product {item.ProductId}: {ex.Message}");
                    }
                }

                // Only clear cart for successful orders
                if (successfulOrders > 0)
                {
                    // Remove only the successfully processed cart items
                    var successfulCartItems = cartItems.Take(successfulOrders).ToList();
                    _db.Cart.RemoveRange(successfulCartItems);
                    _db.SaveChanges();
                }

                if (failedOrders.Any())
                {
                    if (successfulOrders > 0)
                    {
                        TempData["Warning"] = $"{successfulOrders} order(s) placed successfully, but {failedOrders.Count} failed: {string.Join("; ", failedOrders)}";
                    }
                    else
                    {
                        TempData["Error"] = $"All orders failed: {string.Join("; ", failedOrders)}";
                        return RedirectToAction("Checkout");
                    }
                }
                else
                {
                    TempData["Success"] = $"Successfully placed {successfulOrders} order(s)!";
                }

                return RedirectToAction("Index", "Order");
            }
            catch (HttpRequestException ex)
            {
                // Log the detailed error
                Console.WriteLine($"Order creation failed: {ex.Message}");
                TempData["Error"] = $"Failed to create order: {ex.Message}";
                return RedirectToAction("Checkout");
            }
            catch (Exception ex)
            {
                // Log other errors
                Console.WriteLine($"Unexpected error: {ex.Message}");
                TempData["Error"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction("Checkout");
            }
        }
    }
}

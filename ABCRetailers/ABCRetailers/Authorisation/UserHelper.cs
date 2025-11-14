// Authorization/UserHelper.cs
using Microsoft.AspNetCore.Http;

namespace ABCRetailers.Authorization
{
    public static class UserHelper
    {
        public static string GetUsername(HttpContext context)
        {
            return context.Session.GetString("Username") ?? string.Empty;
        }

        public static string GetRole(HttpContext context)
        {
            return context.Session.GetString("Role") ?? string.Empty;
        }

        public static bool IsAdmin(HttpContext context)
        {
            return GetRole(context) == "Admin";
        }

        public static bool IsCustomer(HttpContext context)
        {
            return GetRole(context) == "Customer";
        }

        public static bool IsCurrentUser(HttpContext context, string username)
        {
            return GetUsername(context) == username;
        }
    }
}
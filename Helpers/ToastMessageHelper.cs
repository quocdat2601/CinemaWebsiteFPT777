using System.Text.Json;

namespace MovieTheater.Helpers
{
    public static class ToastMessageHelper
    {
        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _messageTemplates;

        static ToastMessageHelper()
        {
            LoadMessageTemplates();
        }

        private static void LoadMessageTemplates()
        {
            try
            {
                string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "js", "toast-message-templates.json");
                string jsonContent = File.ReadAllText(jsonPath);
                _messageTemplates = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(jsonContent);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error loading toast message templates: {ex.Message}");
                _messageTemplates = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            }
        }

        public static string GetMessage(string category, string action, string type)
        {
            try
            {
                return _messageTemplates[category][action][type];
            }
            catch
            {
                return $"{type} message for {category}.{action}";
            }
        }

        public static void SetToastMessage(Microsoft.AspNetCore.Mvc.Controller controller, string category, string action, string type)
        {
            string message = GetMessage(category, action, type);
            if (type == "success")
            {
                controller.TempData["ToastMessage"] = message;
            }
            else
            {
                controller.TempData["ErrorMessage"] = message;
            }
        }
    }
}
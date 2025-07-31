using System;
using System.IO;

namespace MovieTheater.Helpers
{
    /// <summary>
    /// Helper class để validate file path an toàn, ngăn chặn path injection attacks
    /// </summary>
    public static class PathSecurityHelper
    {
        /// <summary>
        /// Validate và tạo file path an toàn từ user input
        /// </summary>
        /// <param name="baseDirectory">Thư mục gốc được phép (phải kết thúc bằng / hoặc \)</param>
        /// <param name="userFileName">Tên file từ user input</param>
        /// <returns>File path an toàn hoặc null nếu không hợp lệ</returns>
        public static string? CreateSecureFilePath(string baseDirectory, string userFileName)
        {
            try
            {
                // 1. Đảm bảo baseDirectory kết thúc bằng path separator
                if (!baseDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()) && 
                    !baseDirectory.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    baseDirectory = baseDirectory + Path.DirectorySeparatorChar;
                }

                // 2. Tạo path từ user input
                string combinedPath = Path.Combine(baseDirectory, userFileName);
                
                // 3. Resolve canonical path (loại bỏ ../, ./)
                string canonicalPath = Path.GetFullPath(combinedPath);
                
                // 4. Validate path có nằm trong thư mục được phép không
                if (IsPathWithinDirectory(canonicalPath, baseDirectory))
                {
                    return canonicalPath;
                }
                
                return null; // Path không hợp lệ
            }
            catch (Exception)
            {
                return null; // Có lỗi xảy ra
            }
        }

        /// <summary>
        /// Kiểm tra xem path có nằm trong thư mục được phép không
        /// </summary>
        /// <param name="filePath">Path cần kiểm tra</param>
        /// <param name="allowedDirectory">Thư mục được phép</param>
        /// <returns>True nếu path hợp lệ</returns>
        private static bool IsPathWithinDirectory(string filePath, string allowedDirectory)
        {
            try
            {
                // Đảm bảo allowedDirectory kết thúc bằng path separator
                if (!allowedDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()) && 
                    !allowedDirectory.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    allowedDirectory = allowedDirectory + Path.DirectorySeparatorChar;
                }

                // Resolve canonical path cho allowed directory
                string canonicalAllowedDirectory = Path.GetFullPath(allowedDirectory);
                
                // Kiểm tra file path có bắt đầu bằng allowed directory không
                return filePath.StartsWith(canonicalAllowedDirectory, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Tạo tên file an toàn từ user input
        /// </summary>
        /// <param name="originalFileName">Tên file gốc từ user</param>
        /// <returns>Tên file đã được sanitize</returns>
        public static string SanitizeFileName(string originalFileName)
        {
            if (string.IsNullOrEmpty(originalFileName))
                return "default.jpg";

            // Loại bỏ các ký tự không hợp lệ
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = originalFileName;
            
            foreach (var invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar.ToString(), "_");
            }

            // Giới hạn độ dài tên file
            if (sanitized.Length > 100)
            {
                var extension = Path.GetExtension(sanitized);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
                sanitized = nameWithoutExt.Substring(0, 100 - extension.Length) + extension;
            }

            return sanitized;
        }
    }
} 
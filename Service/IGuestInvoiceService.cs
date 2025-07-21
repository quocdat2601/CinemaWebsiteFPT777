using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IGuestInvoiceService
    {
        /// <summary>
        /// Tạo và lưu invoice cho guest payment
        /// </summary>
        /// <param name="orderId">ID đơn hàng</param>
        /// <param name="amount">Số tiền</param>
        /// <param name="customerName">Tên khách hàng</param>
        /// <param name="customerPhone">Số điện thoại</param>
        /// <param name="movieName">Tên phim</param>
        /// <param name="showTime">Thời gian chiếu</param>
        /// <param name="seatInfo">Thông tin ghế</param>
        /// <param name="movieShowId">ID suất chiếu</param>
        /// <returns>True nếu thành công, False nếu thất bại</returns>
        Task<bool> CreateGuestInvoiceAsync(string orderId, decimal amount, string customerName, 
            string customerPhone, string movieName, string showTime, string seatInfo, int movieShowId = 1);
        
        /// <summary>
        /// Kiểm tra xem invoice đã tồn tại chưa
        /// </summary>
        /// <param name="orderId">ID đơn hàng</param>
        /// <returns>True nếu đã tồn tại</returns>
        Task<bool> InvoiceExistsAsync(string orderId);
        
        /// <summary>
        /// Lấy thông tin invoice theo orderId
        /// </summary>
        /// <param name="orderId">ID đơn hàng</param>
        /// <returns>Invoice object hoặc null</returns>
        Task<Invoice?> GetInvoiceByOrderIdAsync(string orderId);
    }
} 
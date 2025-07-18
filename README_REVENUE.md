# Hướng dẫn Doanh thu & Báo cáo trên Dashboard

## 1. **Gross Revenue (Doanh thu gộp)**
- **Định nghĩa:**
  - Tổng số tiền của tất cả các hóa đơn (invoice) đã hoàn thành giao dịch (Status = Completed), bất kể có bị hủy (Cancel) hay không.
- **Công thức:**
  - `Gross Revenue = Tổng (TotalMoney) của tất cả invoice có Status = Completed`
- **Ý nghĩa:**
  - Phản ánh tổng doanh thu mà hệ thống đã ghi nhận từ các giao dịch thành công, trước khi trừ đi các khoản hoàn tiền/hủy vé.

## 2. **Total Refund (Tổng hoàn tiền/hủy vé)**
- **Định nghĩa:**
  - Tổng số tiền của tất cả các hóa đơn đã hoàn thành nhưng bị hủy (Status = Completed và Cancel = true).
- **Công thức:**
  - `Total Refund = Tổng (TotalMoney) của tất cả invoice có Status = Completed và Cancel = true`
- **Ý nghĩa:**
  - Phản ánh tổng giá trị các giao dịch đã hoàn thành nhưng sau đó bị hủy/hoàn tiền (ví dụ: khách hủy vé, hoàn voucher).

## 3. **Net Revenue (Doanh thu thuần)**
- **Định nghĩa:**
  - Doanh thu thực nhận sau khi đã trừ đi các khoản hoàn tiền/hủy vé.
- **Công thức:**
  - `Net Revenue = Gross Revenue - Total Refund`
- **Ý nghĩa:**
  - Đây là con số phản ánh chính xác hiệu quả kinh doanh thực tế của hệ thống.
  - Net Revenue là chỉ số quan trọng nhất để đánh giá KPI doanh thu.

## 4. **Revenue Today (Doanh thu hôm nay)**
- **Định nghĩa:**
  - Tổng số tiền của các hóa đơn đã hoàn thành (Status = Completed) và được đặt trong ngày hôm nay (BookingDate = Today).
- **Công thức:**
  - `Revenue Today = Tổng (TotalMoney) của invoice có Status = Completed, BookingDate = Hôm nay`
- **Ý nghĩa:**
  - Cho biết doanh thu thực nhận trong ngày hiện tại, giúp theo dõi hiệu quả kinh doanh theo ngày.

---

## **Lưu ý về logic hệ thống**
- **Chỉ các invoice có Status = Completed mới được tính vào doanh thu.**
- **Các invoice bị hủy (Cancel = true) vẫn giữ Status = Completed, nhưng được tính vào Total Refund.**
- **Các invoice chưa thanh toán hoặc chưa hoàn thành (Status = Incomplete) không được tính vào bất kỳ chỉ số doanh thu nào.**
- **Khi một vé bị hủy, hệ thống sẽ không đổi Status mà chỉ set Cancel = true để đảm bảo minh bạch và dễ truy vết.**

---

## **Tóm tắt bảng chỉ số**
| Chỉ số         | Điều kiện lọc invoice                | Công thức tính toán                |
|----------------|--------------------------------------|------------------------------------|
| Gross Revenue  | Status = Completed                   | Tổng TotalMoney                    |
| Total Refund   | Status = Completed & Cancel = true   | Tổng TotalMoney                    |
| Net Revenue    | Gross Revenue - Total Refund         |                                    |
| Revenue Today  | Status = Completed & BookingDate=today | Tổng TotalMoney                  |

---

## **Ý nghĩa thực tiễn**
- **Gross Revenue** giúp bạn biết tổng doanh thu đã ghi nhận.
- **Total Refund** giúp bạn kiểm soát các khoản hoàn tiền/hủy vé.
- **Net Revenue** là con số thực tế để đánh giá hiệu quả kinh doanh.
- **Revenue Today** giúp theo dõi doanh thu theo ngày, hỗ trợ quản lý vận hành.

---

Nếu có thay đổi logic hoặc bổ sung chỉ số mới, hãy cập nhật file này để đảm bảo mọi người cùng hiểu rõ cách tính toán và ý nghĩa các chỉ số doanh thu! 
namespace MovieTheater.ViewModels
{
    public class ConfirmTicketAdminViewModel
    {
        // Booking details similar to ConfirmBookingViewModel
        public ConfirmBookingViewModel BookingDetails { get; set; }

        // Member details for admin confirmation
        public string MemberIdInput { get; set; } // For input by admin
        public string MemberId { get; set; } // Displayed if member found
        public string MemberFullName { get; set; }
        public string MemberIdentityCard { get; set; }
        public string MemberPhoneNumber { get; set; }
        public int MemberScore { get; set; }
        public string MemberEmail { get; set; }
        public string MemberPhone { get; set; }

        // Ticket conversion
        public int TicketsToConvert { get; set; }
        public decimal DiscountFromScore { get; set; }
        public string MemberCheckMessage { get; set; } // For displaying messages like "No member has found!" or "Member score is not enough!"
        public string ReturnUrl { get; set; } // For return button to seat selection
    }
}
// Xử lý chọn slot giờ và tự động tính tổng tiền đặt sân
let pricePerHour = 0;
let selectedSlots = [];

function initBookingPage(courtPrice) {
    pricePerHour = courtPrice;

    // Đăng ký sự kiện click chọn slot giờ
    $(document).on("click", ".time-slot-btn.available", function () {
        const slot = $(this).data("time");

        if ($(this).hasClass("selected")) {
            $(this).removeClass("selected");
            selectedSlots = selectedSlots.filter(s => s !== slot);
        } else {
            $(this).addClass("selected");
            selectedSlots.push(slot);
        }

        selectedSlots.sort();
        calculateTotal();
    });
}

// Tính toán tổng tiền dựa trên số slot đã chọn
function calculateTotal() {
    const totalHours = selectedSlots.length;
    const totalAmount = totalHours * pricePerHour;

    $("#selectedSlotCount").text(totalHours);
    $("#totalPriceDisplay").text(totalAmount.toLocaleString('vi-VN') + " VNĐ");

    // Gán dữ liệu vào input hidden của form để Submit
    $("#inputTotalPrice").val(totalAmount);
    $("#inputSelectedSlots").val(selectedSlots.join(","));
}

// Gọi API Fetch kiểm tra danh sách slot đã đặt theo Ngày + ID Sân
async function fetchBookedSlots(courtId, dateStr) {
    try {
        const response = await fetch(`/Bookings/GetBookedSlots?courtId=${courtId}&date=${dateStr}`);
        const bookedTimeList = await response.json();

        // Reset trạng thái các nút slot trên giao diện
        $(".time-slot-btn").removeClass("booked selected").addClass("available");
        selectedSlots = [];
        calculateTotal();

        // Đánh dấu các slot đã bị trùng giờ
        bookedTimeList.forEach(slotTime => {
            $(`.time-slot-btn[data-time='${slotTime}']`)
                .removeClass("available")
                .addClass("booked");
        });
    } catch (error) {
        console.error("Lỗi khi tải dữ liệu khung giờ đã đặt:", error);
    }
}
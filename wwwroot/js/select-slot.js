document.addEventListener("DOMContentLoaded", function () {
    var dateInput = document.getElementById("bookingDate");
    var notice = document.getElementById("notice");
    var nextButton = document.getElementById("nextButton");
    var confirmButton = document.getElementById("confirmButton");
    var cancelConfirmButton = document.getElementById("cancelConfirmButton");
    var backdrop = document.getElementById("confirmBackdrop");
    var floatingSummary = document.getElementById("floatingSummary");
    var customerPhone = document.getElementById("customerPhone");
    var phoneError = document.getElementById("phoneError");
    var confirmError = document.getElementById("confirmError");
    var clearButton = document.getElementById("clearButton");
    var closeSummaryButton = document.getElementById("closeSummaryButton");
    var todayButton = document.getElementById("todayButton");

    var selectedSlots = [];
    var submitting = false;

    function money(value) {
        return new Intl.NumberFormat("vi-VN").format(value) + "đ";
    }

    function showNotice(message, type) {
        if (!notice) return;
        notice.textContent = message;
        notice.className = "notice show " + type;
    }

    function hideNotice() {
        if (!notice) return;
        notice.textContent = "";
        notice.className = "notice";
    }

    function orderedSlots() {
        return selectedSlots.slice().sort(function (a, b) { return a.index - b.index; });
    }

    function totalPrice() {
        return selectedSlots.reduce(function (sum, slot) { return sum + slot.price; }, 0);
    }

    function duration() {
        return selectedSlots.length * 0.5;
    }

    function startTime() {
        var slots = orderedSlots();
        return slots.length ? slots[0].start : "--:--";
    }

    function endTime() {
        var slots = orderedSlots();
        return slots.length ? slots[slots.length - 1].end : "--:--";
    }

    function formatDate(value) {
        if (!value) return "--";
        var parts = value.split("-");
        if (parts.length !== 3) return value;
        return parts[2] + "/" + parts[1] + "/" + parts[0];
    }

    // ĐÃ CẬP NHẬT: Lấy giá tiền trực tiếp từ thuộc tính data-price của Database
    function getSelectedServices() {
        var services = [];
        var rows = document.querySelectorAll(".service-row");

        for (var i = 0; i < rows.length; i++) {
            var row = rows[i];
            var checkbox = row.querySelector(".service-check");
            var quantityInput = row.querySelector(".service-quantity");
            var nameEl = row.querySelector(".service-name");

            if (!checkbox || !checkbox.checked) continue;
            if (!quantityInput) continue;
            if (!nameEl) continue;

            var serviceId = row.getAttribute("data-service-id") || "";
            var priceAttr = row.getAttribute("data-price") || "0";

            if (!serviceId) continue;

            var unitPrice = parseFloat(priceAttr) || 0;
            var maxStock = parseInt(quantityInput.getAttribute("max"), 10) || 99;
            var quantity = Math.max(1, Math.min(maxStock, parseInt(quantityInput.value, 10) || 1));

            services.push({
                id: serviceId,
                name: nameEl.textContent ? nameEl.textContent.trim() : "",
                quantity: quantity,
                unitPrice: unitPrice,
                subtotal: quantity * unitPrice
            });
        }

        return services;
    }

    function serviceTotal() {
        var services = getSelectedServices();
        var sum = 0;
        for (var i = 0; i < services.length; i++) {
            sum += services[i].subtotal;
        }
        return sum;
    }

    function updateFinalTotal() {
        var courtPrice = totalPrice();
        var servicesPrice = serviceTotal();

        var serviceTotalEl = document.getElementById("serviceTotal");
        var finalCourtPriceEl = document.getElementById("finalCourtPrice");
        var finalServicePriceEl = document.getElementById("finalServicePrice");
        var finalTotalEl = document.getElementById("finalTotal");

        if (serviceTotalEl) serviceTotalEl.textContent = money(servicesPrice);
        if (finalCourtPriceEl) finalCourtPriceEl.textContent = money(courtPrice);
        if (finalServicePriceEl) finalServicePriceEl.textContent = money(servicesPrice);
        if (finalTotalEl) finalTotalEl.textContent = money(courtPrice + servicesPrice);
    }

    function updateSummary() {
        var slots = orderedSlots();
        var hasSelection = slots.length > 0;

        var summaryCourt = document.getElementById("summaryCourt");
        var summaryDate = document.getElementById("summaryDate");
        var summaryStart = document.getElementById("summaryStart");
        var summaryEnd = document.getElementById("summaryEnd");
        var summaryDuration = document.getElementById("summaryDuration");
        var summaryPrice = document.getElementById("summaryPrice");

        if (summaryCourt) summaryCourt.textContent = hasSelection ? slots[0].courtName : "Chưa chọn";
        if (summaryDate && dateInput) summaryDate.textContent = formatDate(dateInput.value);
        if (summaryStart) summaryStart.textContent = hasSelection ? startTime() : "--:--";
        if (summaryEnd) summaryEnd.textContent = hasSelection ? endTime() : "--:--";
        if (summaryDuration) summaryDuration.textContent = duration().toLocaleString("vi-VN") + " giờ";
        if (summaryPrice) summaryPrice.textContent = money(totalPrice());

        if (floatingSummary) {
            if (hasSelection) {
                floatingSummary.classList.add("show");
            } else {
                floatingSummary.classList.remove("show");
            }
        }

        if (nextButton) {
            nextButton.disabled = !hasSelection || submitting;
        }
    }

    function clearSelection() {
        for (var i = 0; i < selectedSlots.length; i++) {
            if (selectedSlots[i].button) {
                selectedSlots[i].button.classList.remove("selected");
                selectedSlots[i].button.classList.add("empty");
            }
        }
        selectedSlots = [];
        updateSummary();
    }

    function selectSlot(button) {
        if (!button || submitting) return;
        if (button.getAttribute("data-status") !== "empty") return;

        hideNotice();

        var courtId = Number(button.getAttribute("data-court-id"));
        var courtName = button.getAttribute("data-court-name") || "";
        var index = Number(button.getAttribute("data-index"));
        var start = button.getAttribute("data-start") || "";
        var end = button.getAttribute("data-end") || "";
        var price = Number(button.getAttribute("data-price-slot"));

        var slot = {
            button: button,
            courtId: courtId,
            courtName: courtName,
            index: index,
            start: start,
            end: end,
            price: price
        };

        var existingIndex = -1;
        for (var i = 0; i < selectedSlots.length; i++) {
            if (selectedSlots[i].courtId === slot.courtId && selectedSlots[i].index === slot.index) {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex >= 0) {
            clearSelection();
            return;
        }

        if (selectedSlots.length > 0) {
            var first = selectedSlots[0];

            if (slot.courtId !== first.courtId) {
                showNotice("Bạn chỉ có thể chọn slot trên cùng một sân.", "error");
                return;
            }

            var minIndex = selectedSlots[0].index;
            var maxIndex = selectedSlots[0].index;
            for (var j = 1; j < selectedSlots.length; j++) {
                if (selectedSlots[j].index < minIndex) minIndex = selectedSlots[j].index;
                if (selectedSlots[j].index > maxIndex) maxIndex = selectedSlots[j].index;
            }

            if (slot.index !== minIndex - 1 && slot.index !== maxIndex + 1) {
                showNotice("Hãy chọn slot liền kề với khung giờ đang chọn.", "error");
                return;
            }
        }

        selectedSlots.push(slot);

        button.classList.remove("empty");
        button.classList.add("selected");

        updateSummary();
    }

    var emptySlots = document.querySelectorAll(".slot.empty");
    for (var k = 0; k < emptySlots.length; k++) {
        (function (btn) {
            btn.addEventListener("click", function () {
                selectSlot(btn);
            });
        })(emptySlots[k]);
    }

    if (clearButton) {
        clearButton.addEventListener("click", function () {
            clearSelection();
            hideNotice();
        });
    }

    if (closeSummaryButton && floatingSummary) {
        closeSummaryButton.addEventListener("click", function () {
            floatingSummary.classList.remove("show");
        });
    }

    if (dateInput) {
        dateInput.addEventListener("change", function () {
            if (!dateInput.value) return;
            var url = new URL(window.location.href);
            url.searchParams.set("date", dateInput.value);
            window.location.href = url.toString();
        });
    }

    if (todayButton && dateInput) {
        todayButton.addEventListener("click", function () {
            var today = new Date();
            dateInput.value =
                today.getFullYear() + "-" +
                String(today.getMonth() + 1).padStart(2, "0") + "-" +
                String(today.getDate()).padStart(2, "0");

            dateInput.dispatchEvent(new Event("change"));
        });
    }

    if (nextButton) {
        nextButton.addEventListener("click", function () {
            var slots = orderedSlots();
            if (!slots.length) return;

            hideNotice();
            if (phoneError) {
                phoneError.textContent = "";
                phoneError.classList.remove("show");
            }
            if (confirmError) {
                confirmError.textContent = "";
                confirmError.classList.remove("show");
            }

            var confirmCourt = document.getElementById("confirmCourt");
            var confirmDate = document.getElementById("confirmDate");
            var confirmStart = document.getElementById("confirmStart");
            var confirmEnd = document.getElementById("confirmEnd");
            var confirmDuration = document.getElementById("confirmDuration");
            var confirmPrice = document.getElementById("confirmPrice");

            if (confirmCourt) confirmCourt.textContent = slots[0].courtName;
            if (confirmDate && dateInput) confirmDate.textContent = formatDate(dateInput.value);
            if (confirmStart) confirmStart.textContent = startTime();
            if (confirmEnd) confirmEnd.textContent = endTime();
            if (confirmDuration) confirmDuration.textContent = duration().toLocaleString("vi-VN") + " giờ";
            if (confirmPrice) confirmPrice.textContent = money(totalPrice());

            updateFinalTotal();
            if (backdrop) backdrop.classList.add("show");
        });
    }

    var serviceRows = document.querySelectorAll(".service-row");
    for (var m = 0; m < serviceRows.length; m++) {
        (function (row) {
            var checkbox = row.querySelector(".service-check");
            var quantityInput = row.querySelector(".service-quantity");
            var minusButton = row.querySelector(".quantity-minus");
            var plusButton = row.querySelector(".quantity-plus");

            if (!quantityInput || !checkbox) return;

            var maxStock = parseInt(quantityInput.getAttribute("max"), 10) || 99;

            function normalizeQuantity() {
                var currentVal = parseInt(quantityInput.value, 10) || 1;
                if (currentVal > maxStock) {
                    alert("Số lượng trong kho chỉ còn tối đa " + maxStock + "!");
                }
                var quantity = Math.max(1, Math.min(maxStock, currentVal));
                quantityInput.value = String(quantity);
                updateFinalTotal();
            }

            if (minusButton) {
                minusButton.addEventListener("click", function () {
                    quantityInput.value = String(Math.max(1, (parseInt(quantityInput.value, 10) || 1) - 1));
                    checkbox.checked = true;
                    updateFinalTotal();
                });
            }

            if (plusButton) {
                plusButton.addEventListener("click", function () {
                    var currentVal = parseInt(quantityInput.value, 10) || 1;
                    if (currentVal >= maxStock) {
                        alert("Số lượng trong kho chỉ còn tối đa " + maxStock + "!");
                        return;
                    }
                    quantityInput.value = String(Math.min(maxStock, currentVal + 1));
                    checkbox.checked = true;
                    updateFinalTotal();
                });
            }

            quantityInput.addEventListener("change", normalizeQuantity);
            checkbox.addEventListener("change", updateFinalTotal);
        })(serviceRows[m]);
    }

    if (cancelConfirmButton) {
        cancelConfirmButton.addEventListener("click", function () {
            if (submitting) return;
            if (backdrop) backdrop.classList.remove("show");
        });
    }

    if (backdrop) {
        backdrop.addEventListener("click", function (event) {
            if (event.target === backdrop && !submitting) {
                backdrop.classList.remove("show");
            }
        });
    }

    function validatePhone(value) {
        var phone = value.replace(/[\s-]/g, "");
        return /^0\d{9}$/.test(phone);
    }

    if (confirmButton) {
        confirmButton.addEventListener("click", function () {
            var slots = orderedSlots();
            if (!slots.length || submitting) return;

            var phone = customerPhone ? customerPhone.value.trim() : "";

            if (!validatePhone(phone)) {
                if (phoneError) {
                    phoneError.textContent = "Vui lòng nhập số điện thoại hợp lệ gồm 10 chữ số, bắt đầu bằng số 0.";
                    phoneError.classList.add("show");
                }
                if (customerPhone) customerPhone.focus();
                return;
            }

            if (phoneError) {
                phoneError.textContent = "";
                phoneError.classList.remove("show");
            }
            if (confirmError) {
                confirmError.textContent = "";
                confirmError.classList.remove("show");
            }

            submitting = true;
            confirmButton.disabled = true;
            confirmButton.textContent = "Đang xử lý...";
            if (cancelConfirmButton) cancelConfirmButton.disabled = true;

            updateSummary();

            var tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
            var token = tokenElement ? tokenElement.value : '';

            var selectedServices = getSelectedServices();
            var selectedServiceTotal = serviceTotal();

            fetch('/Bookings/CreateBooking', {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                body: JSON.stringify({
                    courtId: slots[0].courtId,
                    date: dateInput ? dateInput.value : "",
                    startTimes: slots.map(function (s) { return s.start; }),
                    customerPhone: phone,
                    services: selectedServices
                })
            })
                .then(function (response) {
                    return response.json().then(function (result) {
                        return { ok: response.ok, result: result };
                    });
                })
                .then(function (data) {
                    if (!data.ok || !data.result.success) {
                        if (confirmError) {
                            confirmError.textContent = data.result.message || "Đặt sân thất bại. Vui lòng thử lại.";
                            confirmError.classList.add("show");
                        }
                        setTimeout(function () {
                            window.location.reload();
                        }, 2000);

                        return;
                    }

                    if (backdrop) backdrop.classList.remove("show");

                    var courtAmount = Number(data.result.totalPrice || totalPrice());
                    var grandTotal = courtAmount + selectedServiceTotal;
                    var bookingCode = data.result.bookingCode;

                    // Ẩn bảng xác nhận cũ nếu có
                    if (backdrop) backdrop.classList.remove("show");

                    // ĐIỀN THÔNG TIN VÀO MODAL QR
                    var qrModal = document.getElementById("qrModal");
                    var qrCodeEl = document.getElementById("qrBookingCode");
                    var qrImgEl = document.getElementById("vietQrImage");

                    if (qrCodeEl) qrCodeEl.textContent = bookingCode;

                    // Cấu hình thông tin tài khoản nhận tiền của sân bạn tại đây:
                    // Cú pháp VietQR: https://img.vietqr.io/image/[MãNgânHàng]-[SốTàiKhoản]-compact2.png?amount=[Tiền]&addInfo=[NộiDung]
                    var bankId = "MB"; // Ví dụ ngân hàng MBBank
                    var accountNo = "0901234567999"; // Số tài khoản nhận tiền của sân
                    var accountName = "NGUYEN VAN A"; // Tên chủ tài khoản

                    var qrUrl = "https://img.vietqr.io/image/" + bankId + "-" + accountNo + "-compact2.png?amount=" + grandTotal + "&addInfo=" + encodeURIComponent(bookingCode) + "&accountName=" + encodeURIComponent(accountName);

                    if (qrImgEl) qrImgEl.src = qrUrl;
                    if (qrModal) qrModal.style.display = "flex";

                    // Sự kiện khi khách bấm nút hoàn tất sau khi quét QR
                    var finishBtn = document.getElementById("finishPaymentBtn");
                    if (finishBtn) {
                        finishBtn.onclick = function () {
                            window.location.reload();
                        };
                    }
                })
                .catch(function (error) {
                    if (confirmError) {
                        confirmError.textContent = "Không thể kết nối đến máy chủ. Vui lòng thử lại.";
                        confirmError.classList.add("show");
                    }
                })
                .finally(function () {
                    submitting = false;
                    confirmButton.disabled = false;
                    confirmButton.textContent = "Xác nhận đặt";
                    if (cancelConfirmButton) cancelConfirmButton.disabled = false;
                    updateSummary();
                });
        });
    }

    updateSummary();
    updateFinalTotal();
});
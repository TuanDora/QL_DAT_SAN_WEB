using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCourtBooking.Models.Services;

namespace MyCourtBooking.Controllers
{
    [AllowAnonymous]
    public class CourtsController : Controller
    {
        private readonly ICourtService _courtService;

        public CourtsController(ICourtService courtService)
        {
            _courtService = courtService;
        }

        // GET: /Courts hoặc /Courts/Index
        [HttpGet]
        public async Task<IActionResult> Index(
            string? keyword,
            string? district,
            int? courtTypeId,
            double? minRating,
            decimal? maxPrice,
            int page = 1)
        {
            int pageSize = 9;

            var (courts, totalCount) = await _courtService.GetPagedCourtsAsync(
                keyword, district, courtTypeId, minRating, maxPrice, page, pageSize);

            ViewBag.Keyword = keyword;
            ViewBag.SelectedDistrict = district;
            ViewBag.SelectedCourtTypeId = courtTypeId;
            ViewBag.MinRating = minRating;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            ViewBag.Districts = await _courtService.GetDistrictsAsync();
            ViewBag.CourtTypes = await _courtService.GetCourtTypesAsync();

            return View("~/Views/Courts/Index.cshtml", courts);
        }
        
        // GET: /Courts/Detail/5
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var courtDetail = await _courtService.GetCourtDetailByIdAsync(id);
            if (courtDetail == null)
            {
                return NotFound();
            }

            ViewBag.RelatedCourts = await _courtService.GetRelatedCourtsAsync(id, courtDetail.District, 3);
            return View("~/Views/Courts/Detail.cshtml", courtDetail);
        }

        // GET: /Courts/GetAvailableSlotsAjax
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlotsAjax(int courtId, DateTime date)
        {
            if (courtId <= 0 || date < DateTime.Today)
            {
                return Json(new { success = false, message = "Ngày chọn không hợp lệ!" });
            }

            var slots = await _courtService.GetAvailableSlotsAsync(courtId, date);
            return Json(new { success = true, data = slots });
        }

        // POST: /Courts/AddReviewAjax
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReviewAjax([FromBody] AddReviewRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu đánh giá không hợp lệ!" });
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện đánh giá!" });
            }

            var (success, message) = await _courtService.AddReviewAsync(userId, model.CourtId, model.Rating, model.Comment);
            return Json(new { success = success, message = message });
        }
    }

    public class AddReviewRequestModel
    {
        public int CourtId { get; set; }
        public double Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
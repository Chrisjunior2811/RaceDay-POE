using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// ---------- Account ----------

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, ErrorMessage = "Your name must be 100 characters or fewer.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Your email must be 150 characters or fewer.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
    [Display(Name = "I am a")]
    public string Role { get; set; } = "Participant";

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20, ErrorMessage = "The phone number must be 20 characters or fewer.")]
    [Display(Name = "Phone number (optional)")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Organiser registration code")]
    public string? OrganiserCode { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, ErrorMessage = "Your name must be 100 characters or fewer.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20, ErrorMessage = "The phone number must be 20 characters or fewer.")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool HasPicture { get; set; }
    public DateTime? MemberSince { get; set; }
}

// ---------- Participant ----------

public class HomeIndexViewModel
{
    public string? Search { get; set; }
    public string? Type { get; set; }

    [Display(Name = "Min distance (km)")]
    public decimal? MinDistanceKm { get; set; }

    [Display(Name = "Max distance (km)")]
    public decimal? MaxDistanceKm { get; set; }

    public List<EventDto> Events { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public EnrolmentDto? ExistingEnrolment { get; set; }
}

// ---------- Organiser ----------

public class DashboardViewModel
{
    public List<OrganiserEventDto> Events { get; set; } = new();
    public int TotalEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public OrganiserEventDto? NextEvent { get; set; }
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Please enter an event name.")]
    [StringLength(150, ErrorMessage = "The event name must be 150 characters or fewer.")]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Running";

    [Required(ErrorMessage = "Please enter a description.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a date and time.")]
    [Display(Name = "Date and time")]
    public DateTime EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(150, ErrorMessage = "The location must be 150 characters or fewer.")]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [StringLength(150, ErrorMessage = "The start point must be 150 characters or fewer.")]
    [Display(Name = "Start point")]
    public string? StartPoint { get; set; }

    [StringLength(150, ErrorMessage = "The end point must be 150 characters or fewer.")]
    [Display(Name = "End point")]
    public string? EndPoint { get; set; }

    [Display(Name = "Route notes")]
    public string? RouteDescription { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public bool HasBanner { get; set; }
}

public class CategoryFormViewModel
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Please enter a category name.")]
    [StringLength(100, ErrorMessage = "The category name must be 100 characters or fewer.")]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(0, 999999.99, ErrorMessage = "The entry fee must be 0 or more.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000, ErrorMessage = "The participant limit must be at least 1.")]
    [Display(Name = "Participant limit")]
    public int MaxParticipants { get; set; } = 100;
}

public class CategoriesPageViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

public class EnrolmentsPageViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultsPageViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public Dictionary<int, ResultDto> ResultsByEnrolment { get; set; } = new();
}

public class ResultEntryViewModel
{
    public int EnrolmentId { get; set; }

    [Display(Name = "Finish time")]
    public string? FinishTime { get; set; }

    [Range(1, 1000000, ErrorMessage = "The finishing position must be 1 or more.")]
    public int? FinishPosition { get; set; }

    [Range(1, 1000000, ErrorMessage = "The category position must be 1 or more.")]
    public int? CategoryPosition { get; set; }

    public string Status { get; set; } = "Finished";
}

// ---------- Errors ----------

public class ErrorViewModel
{
    public int Code { get; set; }
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "Please try again.";
}

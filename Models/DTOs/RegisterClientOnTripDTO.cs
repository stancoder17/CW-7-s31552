using System.ComponentModel.DataAnnotations;

namespace Travel_agencies_application.Models;

public class RegisterClientOnTripDTO
{
    [Required]
    public int IdClient { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Telephone { get; set; }
    public string Pesel { get; set; }
    [Required]
    public int IdTrip { get; set; }
    public string TripName { get; set; }
    public string TripDescription { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}
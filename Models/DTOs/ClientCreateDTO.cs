using System.ComponentModel.DataAnnotations;

namespace Travel_agencies_application.Models;

public class ClientCreateDto
{
    [Required, Length(1, 120)]
    public string FirstName { get; set; }
    
    [Required, Length(1, 120)]
    public string LastName { get; set; }
    
    [Required, Length(1, 120)]
    public string Email { get; set; }
    
    [Required, Length(1, 120)]
    public string Telephone { get; set; }
    
    [Required, Length(11, 11)]
    public string Pesel { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace SredstvaData.Models;

public class Firma
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Naziv { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Mesto { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string MaticniBroj { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string PIB { get; set; } = string.Empty;
    
    public ICollection<Sredstvo> Sredstva { get; set; } = new List<Sredstvo>();
}

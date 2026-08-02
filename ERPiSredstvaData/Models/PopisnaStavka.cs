using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiSredstvaData.Models;

public class PopisnaStavka
{
    public int Id { get; set; }
    
    public int PopisId { get; set; }
    public Popis Popis { get; set; } = null!;
    
    public int SredstvoId { get; set; }
    public Sredstvo Sredstvo { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal KnjiznaKolicina { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal PopisanaKolicina { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal KnjiznaVrednost { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProcenjenaVrednost { get; set; }
    
    public string Napomena { get; set; } = string.Empty;

    [NotMapped]
    public decimal Razlika => PopisanaKolicina - KnjiznaKolicina;

    [NotMapped]
    public bool ImaRazliku => Razlika != 0;
}

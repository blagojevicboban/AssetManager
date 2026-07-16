using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SredstvaData.Models;

public class Sredstvo
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string InventarskiBroj { get; set; } = string.Empty;
    
    [NotMapped]
    public string InventarskiBrojSort => System.Text.RegularExpressions.Regex.Replace(InventarskiBroj ?? "", @"\d+", m => m.Value.PadLeft(20, '0'));
    
    [Required]
    [MaxLength(200)]
    public string Naziv { get; set; } = string.Empty;
    
    public DateTime DatumNabavke { get; set; }
    
    public DateTime DatumAktiviranja { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal NabavnaVrednost { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal IspravkaVrednosti { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal SadasnjaVrednost { get; set; }
    
    public string AmortizacionaGrupa { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Konto { get; set; } = string.Empty;
    
    public int ObracunskaJedinica { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal StopaAmortizacije { get; set; }
    
    public bool JeAktivno { get; set; } = true;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Kolicina { get; set; } = 1;

    /// <summary>Originalna SIFRA iz SREDSTVA.DBF — za veze sa Karticom i Prijavom</summary>
    public int LegacySifra { get; set; }


    // Navigation
    public ICollection<Kartica> Kartice { get; set; } = new List<Kartica>();
    public ICollection<Prijava> Prijave { get; set; } = new List<Prijava>();
    public ICollection<Rashod> Rashodi { get; set; } = new List<Rashod>();
}

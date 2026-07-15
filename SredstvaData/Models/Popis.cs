using System;
using System.Collections.Generic;

namespace SredstvaData.Models;

public enum StatusPopisa
{
    UToku = 0,
    Zavrsen = 1
}

public class Popis
{
    public int Id { get; set; }
    
    public DateTime DatumPopisa { get; set; }
    public int Godina { get; set; }
    
    public int KomisijaId { get; set; }
    public Komisija Komisija { get; set; } = null!;
    
    public StatusPopisa Status { get; set; } = StatusPopisa.UToku;
    
    public ICollection<PopisnaStavka> Stavke { get; set; } = new List<PopisnaStavka>();
}

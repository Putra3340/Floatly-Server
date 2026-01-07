using System;
using System.Collections.Generic;

namespace Floaty_Music.Models;

public partial class Transaction
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? OrderId { get; set; }

    public string? SnapToken { get; set; }

    public int? PaymentType { get; set; }

    public int? PaymentStatus { get; set; }

    public decimal? Amount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Users? User { get; set; }
}

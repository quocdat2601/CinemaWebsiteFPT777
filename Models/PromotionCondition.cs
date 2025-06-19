using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class PromotionCondition
{
    public int ConditionId { get; set; }

    public int? PromotionId { get; set; }

    public int? ConditionTypeId { get; set; }

    public string? TargetEntity { get; set; }

    public string? TargetField { get; set; }

    public string? Operator { get; set; }

    public string? TargetValue { get; set; }

    public virtual ConditionType? ConditionType { get; set; }

    public virtual Promotion? Promotion { get; set; }
}

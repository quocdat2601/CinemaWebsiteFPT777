using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class ConditionType
{
    public int ConditionTypeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<PromotionCondition> PromotionConditions { get; set; } = new List<PromotionCondition>();
}
